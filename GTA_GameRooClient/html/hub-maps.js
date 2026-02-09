// ==================== Map Editor ====================

var spawnTypeNames = ['PLAYER', 'WEAPON', 'OBJECT', 'WIN_BARRIER'];

function renderMapList() {
    var container = document.getElementById('mapListItems');
    container.innerHTML = '';

    maps.forEach(function(map) {
        var item = document.createElement('div');
        item.className = 'map-list-item';
        if (selectedMapId === map.id) item.classList.add('selected');

        var name = document.createElement('div');
        name.className = 'map-list-item-name';
        if (!map.enabled) name.classList.add('disabled');
        name.textContent = map.name + (map.enabled ? '' : ' (Disabled)');
        item.appendChild(name);

        var meta = document.createElement('div');
        meta.className = 'map-list-item-meta';
        if (map.gamemodes) {
            map.gamemodes.forEach(function(gm) {
                var badge = document.createElement('span');
                badge.className = 'map-list-badge gm-' + gm.toLowerCase();
                badge.textContent = gm.toUpperCase();
                meta.appendChild(badge);
            });
        }
        var spawnBadge = document.createElement('span');
        spawnBadge.className = 'map-list-badge';
        spawnBadge.textContent = (map.spawns ? map.spawns.length : 0) + ' spawns';
        meta.appendChild(spawnBadge);
        item.appendChild(meta);

        (function(id) {
            item.addEventListener('click', function() {
                selectMap(id);
            });
        })(map.id);

        container.appendChild(item);
    });
}

function selectMap(id) {
    selectedMapId = id;
    deleteConfirmActive = false;
    renderMapList();

    var map = null;
    for (var i = 0; i < maps.length; i++) {
        if (maps[i].id === id) { map = maps[i]; break; }
    }
    if (!map) {
        document.getElementById('mapForm').classList.add('hidden');
        document.getElementById('mapNoSelection').classList.remove('hidden');
        return;
    }

    document.getElementById('mapNoSelection').classList.add('hidden');
    document.getElementById('mapForm').classList.remove('hidden');

    document.getElementById('mapName').value = map.name || '';
    document.getElementById('mapAuthor').value = map.author || '';
    document.getElementById('mapDescription').value = map.description || '';
    document.getElementById('mapEnabled').checked = map.enabled !== false;
    document.getElementById('mapMinPlayers').value = map.minPlayers || 2;
    document.getElementById('mapMaxPlayers').value = map.maxPlayers || 32;
    document.getElementById('mapPosX').value = round2(map.posX);
    document.getElementById('mapPosY').value = round2(map.posY);
    document.getElementById('mapPosZ').value = round2(map.posZ);
    document.getElementById('mapSizeX').value = round2(map.sizeX);
    document.getElementById('mapSizeY').value = round2(map.sizeY);
    document.getElementById('mapSizeZ').value = round2(map.sizeZ);
    document.getElementById('mapRotation').value = round2(map.rotation || 0);

    // Gamemodes checkboxes
    var gmCheckboxes = document.querySelectorAll('#mapGamemodes input[type="checkbox"]');
    gmCheckboxes.forEach(function(cb) {
        cb.checked = map.gamemodes && map.gamemodes.indexOf(cb.value) !== -1;
    });

    // Update boundaries button text
    document.getElementById('mapBoundaries').textContent = boundariesShown[id] ? 'Hide Boundaries' : 'Show Boundaries';

    // Initialize vertices array if missing
    map.vertices = map.vertices || [];

    renderVertexList();
    renderSpawnList();

    // Notify C# which map is selected for boundary drawing
    fetch('https://gta_gameroo/selectMapForEdit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mapId: id })
    });
}

function getSelectedMap() {
    if (selectedMapId === null) return null;
    for (var i = 0; i < maps.length; i++) {
        if (maps[i].id === selectedMapId) return maps[i];
    }
    return null;
}

function readFormIntoMap(map) {
    map.name = document.getElementById('mapName').value;
    map.author = document.getElementById('mapAuthor').value;
    map.description = document.getElementById('mapDescription').value;
    map.enabled = document.getElementById('mapEnabled').checked;
    map.minPlayers = parseInt(document.getElementById('mapMinPlayers').value) || 2;
    map.maxPlayers = parseInt(document.getElementById('mapMaxPlayers').value) || 32;
    map.posX = parseFloat(document.getElementById('mapPosX').value) || 0;
    map.posY = parseFloat(document.getElementById('mapPosY').value) || 0;
    map.posZ = parseFloat(document.getElementById('mapPosZ').value) || 0;
    map.sizeX = parseFloat(document.getElementById('mapSizeX').value) || 0;
    map.sizeY = parseFloat(document.getElementById('mapSizeY').value) || 0;
    map.sizeZ = parseFloat(document.getElementById('mapSizeZ').value) || 0;
    map.rotation = parseFloat(document.getElementById('mapRotation').value) || 0;

    var gms = [];
    document.querySelectorAll('#mapGamemodes input[type="checkbox"]').forEach(function(cb) {
        if (cb.checked) gms.push(cb.value);
    });
    map.gamemodes = gms;

    // Vertices are managed directly via the list, just ensure the array exists
    map.vertices = map.vertices || [];
}

function renderSpawnList() {
    var map = getSelectedMap();
    var container = document.getElementById('mapSpawnList');
    container.innerHTML = '';
    if (!map || !map.spawns) {
        document.getElementById('mapSpawnCount').textContent = '(0)';
        return;
    }
    document.getElementById('mapSpawnCount').textContent = '(' + map.spawns.length + ')';

    map.spawns.forEach(function(spawn, index) {
        var row = document.createElement('div');
        row.className = 'map-spawn-row';

        // Type select
        var typeDiv = document.createElement('div');
        typeDiv.className = 'map-spawn-type';
        var sel = document.createElement('select');
        spawnTypeNames.forEach(function(name, i) {
            var opt = document.createElement('option');
            opt.value = i;
            opt.textContent = name;
            if (i === spawn.spawnType) opt.selected = true;
            sel.appendChild(opt);
        });
        (function(idx) {
            sel.addEventListener('change', function() {
                map.spawns[idx].spawnType = parseInt(sel.value);
                if (parseInt(sel.value) === 3) {
                    if (!map.spawns[idx].sizeX) map.spawns[idx].sizeX = 10;
                    if (!map.spawns[idx].sizeY) map.spawns[idx].sizeY = 10;
                }
                renderSpawnList();
            });
        })(index);
        typeDiv.appendChild(sel);
        row.appendChild(typeDiv);

        var isBarrier = spawn.spawnType === 3;

        // Team input (hidden for WIN_BARRIER)
        var teamDiv = document.createElement('div');
        teamDiv.className = 'map-spawn-team';
        if (isBarrier) teamDiv.style.display = 'none';
        var teamInput = document.createElement('input');
        teamInput.type = 'number';
        teamInput.min = 0;
        teamInput.max = 3;
        teamInput.value = spawn.team;
        teamInput.title = 'Team';
        (function(idx) {
            teamInput.addEventListener('change', function() {
                map.spawns[idx].team = parseInt(teamInput.value) || 0;
            });
        })(index);
        teamDiv.appendChild(teamInput);
        row.appendChild(teamDiv);

        // Coords
        var coordDiv = document.createElement('div');
        coordDiv.className = 'map-spawn-coords';
        ['posX', 'posY', 'posZ'].forEach(function(field) {
            var inp = document.createElement('input');
            inp.type = 'number';
            inp.step = '0.1';
            inp.value = round2(spawn[field]);
            inp.title = field.replace('pos', '');
            (function(idx, f) {
                inp.addEventListener('change', function() {
                    map.spawns[idx][f] = parseFloat(inp.value) || 0;
                });
            })(index, field);
            coordDiv.appendChild(inp);
        });
        row.appendChild(coordDiv);

        // Heading / Rotation
        var headDiv = document.createElement('div');
        headDiv.className = 'map-spawn-heading';
        var headInput = document.createElement('input');
        headInput.type = 'number';
        headInput.min = 0;
        headInput.max = 360;
        headInput.step = 5;
        headInput.value = round2(spawn.heading || 0);
        headInput.title = isBarrier ? 'Rotation (0-360)' : 'Heading (0-360)';
        (function(idx) {
            headInput.addEventListener('change', function() {
                map.spawns[idx].heading = parseFloat(headInput.value) || 0;
            });
        })(index);
        headDiv.appendChild(headInput);
        row.appendChild(headDiv);

        // Width & Length (WIN_BARRIER only)
        if (isBarrier) {
            var sizeDiv = document.createElement('div');
            sizeDiv.className = 'map-spawn-barrier-size';
            var wInput = document.createElement('input');
            wInput.type = 'number';
            wInput.min = 1;
            wInput.max = 500;
            wInput.step = 1;
            wInput.value = round2(spawn.sizeX || 10);
            wInput.title = 'Width';
            wInput.placeholder = 'W';
            (function(idx) {
                wInput.addEventListener('input', function() {
                    map.spawns[idx].sizeX = parseFloat(wInput.value) || 10;
                });
            })(index);
            sizeDiv.appendChild(wInput);
            var lInput = document.createElement('input');
            lInput.type = 'number';
            lInput.min = 1;
            lInput.max = 500;
            lInput.step = 1;
            lInput.value = round2(spawn.sizeY || 10);
            lInput.title = 'Length';
            lInput.placeholder = 'L';
            (function(idx) {
                lInput.addEventListener('input', function() {
                    map.spawns[idx].sizeY = parseFloat(lInput.value) || 10;
                });
            })(index);
            sizeDiv.appendChild(lInput);
            row.appendChild(sizeDiv);
        }

        // Actions
        var actDiv = document.createElement('div');
        actDiv.className = 'map-spawn-actions';

        // Teleport button
        var tpBtn = document.createElement('button');
        tpBtn.className = 'map-spawn-btn';
        tpBtn.textContent = 'TP';
        tpBtn.title = 'Teleport to spawn';
        (function(idx) {
            tpBtn.addEventListener('click', function() {
                fetch('https://gta_gameroo/teleportToSpawn', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ posX: map.spawns[idx].posX, posY: map.spawns[idx].posY, posZ: map.spawns[idx].posZ })
                });
                minimizeHub();
            });
        })(index);
        actDiv.appendChild(tpBtn);

        // Move to player button
        var moveBtn = document.createElement('button');
        moveBtn.className = 'map-spawn-btn';
        moveBtn.textContent = 'Here';
        moveBtn.title = 'Move spawn to your position';
        (function(idx) {
            moveBtn.addEventListener('click', function() {
                fetch('https://gta_gameroo/getPlayerPosition', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ context: 'moveSpawn', spawnIndex: idx })
                });
            });
        })(index);
        actDiv.appendChild(moveBtn);

        // Delete button
        var delBtn = document.createElement('button');
        delBtn.className = 'map-spawn-btn delete';
        delBtn.textContent = 'X';
        delBtn.title = 'Delete spawn';
        (function(idx) {
            delBtn.addEventListener('click', function() {
                map.spawns.splice(idx, 1);
                renderSpawnList();
            });
        })(index);
        actDiv.appendChild(delBtn);

        row.appendChild(actDiv);
        container.appendChild(row);
    });
}

function renderVertexList() {
    var map = getSelectedMap();
    var container = document.getElementById('mapVertexList');
    container.innerHTML = '';
    if (!map || !map.vertices) {
        document.getElementById('mapVertexCount').textContent = '(0)';
        return;
    }
    document.getElementById('mapVertexCount').textContent = '(' + map.vertices.length + ')';

    map.vertices.forEach(function(vertex, index) {
        var row = document.createElement('div');
        row.className = 'map-spawn-row';

        // Index label
        var idxDiv = document.createElement('div');
        idxDiv.className = 'map-spawn-type';
        idxDiv.style.minWidth = '30px';
        idxDiv.style.fontWeight = 'bold';
        idxDiv.textContent = '#' + (index + 1);
        row.appendChild(idxDiv);

        // X input
        var coordDiv = document.createElement('div');
        coordDiv.className = 'map-spawn-coords';
        var xInp = document.createElement('input');
        xInp.type = 'number';
        xInp.step = '0.1';
        xInp.value = round2(vertex.x);
        xInp.title = 'X';
        (function(idx) {
            xInp.addEventListener('change', function() {
                map.vertices[idx].x = parseFloat(xInp.value) || 0;
            });
        })(index);
        coordDiv.appendChild(xInp);

        var yInp = document.createElement('input');
        yInp.type = 'number';
        yInp.step = '0.1';
        yInp.value = round2(vertex.y);
        yInp.title = 'Y';
        (function(idx) {
            yInp.addEventListener('change', function() {
                map.vertices[idx].y = parseFloat(yInp.value) || 0;
            });
        })(index);
        coordDiv.appendChild(yInp);
        row.appendChild(coordDiv);

        // Actions
        var actDiv = document.createElement('div');
        actDiv.className = 'map-spawn-actions';

        // TP button
        var tpBtn = document.createElement('button');
        tpBtn.className = 'map-spawn-btn';
        tpBtn.textContent = 'TP';
        tpBtn.title = 'Teleport to vertex';
        (function(idx) {
            tpBtn.addEventListener('click', function() {
                var posZ = parseFloat(document.getElementById('mapPosZ').value) || 0;
                fetch('https://gta_gameroo/teleportToSpawn', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ posX: map.vertices[idx].x, posY: map.vertices[idx].y, posZ: posZ })
                });
                minimizeHub();
            });
        })(index);
        actDiv.appendChild(tpBtn);

        // Here button
        var hereBtn = document.createElement('button');
        hereBtn.className = 'map-spawn-btn';
        hereBtn.textContent = 'Here';
        hereBtn.title = 'Move vertex to your position';
        (function(idx) {
            hereBtn.addEventListener('click', function() {
                fetch('https://gta_gameroo/getPlayerPosition', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ context: 'moveVertex', spawnIndex: idx })
                });
            });
        })(index);
        actDiv.appendChild(hereBtn);

        // Up arrow
        if (index > 0) {
            var upBtn = document.createElement('button');
            upBtn.className = 'map-spawn-btn';
            upBtn.textContent = '\u25B2';
            upBtn.title = 'Move up';
            (function(idx) {
                upBtn.addEventListener('click', function() {
                    var tmp = map.vertices[idx];
                    map.vertices[idx] = map.vertices[idx - 1];
                    map.vertices[idx - 1] = tmp;
                    renderVertexList();
                });
            })(index);
            actDiv.appendChild(upBtn);
        }

        // Down arrow
        if (index < map.vertices.length - 1) {
            var downBtn = document.createElement('button');
            downBtn.className = 'map-spawn-btn';
            downBtn.textContent = '\u25BC';
            downBtn.title = 'Move down';
            (function(idx) {
                downBtn.addEventListener('click', function() {
                    var tmp = map.vertices[idx];
                    map.vertices[idx] = map.vertices[idx + 1];
                    map.vertices[idx + 1] = tmp;
                    renderVertexList();
                });
            })(index);
            actDiv.appendChild(downBtn);
        }

        // Delete button
        var delBtn = document.createElement('button');
        delBtn.className = 'map-spawn-btn delete';
        delBtn.textContent = 'X';
        delBtn.title = 'Delete vertex';
        (function(idx) {
            delBtn.addEventListener('click', function() {
                map.vertices.splice(idx, 1);
                renderVertexList();
            });
        })(index);
        actDiv.appendChild(delBtn);

        row.appendChild(actDiv);
        container.appendChild(row);
    });
}

// Map action button handlers
document.getElementById('mapAddVertex').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    if (!map.vertices) map.vertices = [];

    fetch('https://gta_gameroo/getPlayerPosition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ context: 'addVertex' })
    });
});

document.getElementById('mapCreateBtn').addEventListener('click', function() {
    fetch('https://gta_gameroo/createMap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
    // Map will appear via 'mapCreated' NUI message from C#
});

document.getElementById('mapUseMyPos').addEventListener('click', function() {
    fetch('https://gta_gameroo/getPlayerPosition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ context: 'useMyPos' })
    });
});

document.getElementById('mapAddSpawn').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    if (!map.spawns) map.spawns = [];

    fetch('https://gta_gameroo/getPlayerPosition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ context: 'addSpawn' })
    });
});

function minimizeHub() {
    document.getElementById('hub').classList.remove('visible');
    fetch('https://gta_gameroo/minimizeHub', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
}

document.getElementById('mapTeleport').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    readFormIntoMap(map);
    fetch('https://gta_gameroo/teleportToMap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ posX: map.posX, posY: map.posY, posZ: map.posZ })
    });
    minimizeHub();
});

document.getElementById('mapBoundaries').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    readFormIntoMap(map);
    fetch('https://gta_gameroo/toggleBoundaries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mapId: map.id, show: true, posX: map.posX, posY: map.posY, posZ: map.posZ, sizeX: map.sizeX, sizeY: map.sizeY, sizeZ: map.sizeZ, rotation: map.rotation || 0, vertices: map.vertices || [] })
    });
    minimizeHub();
});

document.getElementById('mapSave').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    readFormIntoMap(map);
    fetch('https://gta_gameroo/saveMap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ map: map })
    });
    // Map update will come via 'mapSaved' NUI message from C#
});

document.getElementById('mapDelete').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    if (!deleteConfirmActive) {
        deleteConfirmActive = true;
        var btn = document.getElementById('mapDelete');
        btn.textContent = 'Confirm Delete?';
        btn.classList.add('confirming');
        setTimeout(function() {
            if (deleteConfirmActive) {
                deleteConfirmActive = false;
                btn.textContent = 'Delete Map';
                btn.classList.remove('confirming');
            }
        }, 3000);
        return;
    }
    deleteConfirmActive = false;
    var deleteId = map.id;
    fetch('https://gta_gameroo/deleteMap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mapId: deleteId })
    });
    // Remove locally immediately for responsiveness
    for (var i = 0; i < maps.length; i++) {
        if (maps[i].id === deleteId) {
            maps.splice(i, 1);
            break;
        }
    }
    selectedMapId = null;
    document.getElementById('mapForm').classList.add('hidden');
    document.getElementById('mapNoSelection').classList.remove('hidden');
    document.getElementById('mapDelete').textContent = 'Delete Map';
    document.getElementById('mapDelete').classList.remove('confirming');
    renderMapList();
    // C# will also send updateMaps which will sync the full list
});
