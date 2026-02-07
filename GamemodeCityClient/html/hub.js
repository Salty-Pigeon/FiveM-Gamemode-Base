// Map JS key identifiers to FiveM control IDs (F5 omitted — reserved for hub toggle)
const keyToControlId = {
    // Letters
    'a':          34,
    'b':          29,
    'c':          26,
    'd':          35,
    'e':          38,
    'f':          23,
    'g':          47,
    'h':          74,
    'i':          27,
    'j':          311,
    'k':          311,
    'l':          182,
    'm':          244,
    'n':          249,
    'o':          182,
    'p':          199,
    'q':          44,
    'r':          45,
    's':          33,
    't':          245,
    'u':          303,
    'v':          0,
    'w':          32,
    'x':          73,
    'y':          246,
    'z':          48,
    // Symbols
    '`':          243,
    '~':          243,
    '[':          39,
    ']':          40,
    // Number keys
    '1':          157,
    '2':          158,
    '3':          160,
    '4':          164,
    '5':          165,
    '6':          159,
    '7':          161,
    '8':          162,
    '9':          163,
    '0':          56,
    // Modifiers & common keys
    'Shift':      21,
    'Control':    36,
    'Alt':        19,
    'Tab':        37,
    'CapsLock':   171,
    ' ':          22,
    'Enter':      18,
    'Backspace':  194,
    // Navigation keys
    'Insert':     121,
    'Home':       212,
    'End':        213,
    'Delete':     214,
    'PageUp':     10,
    'PageDown':   11,
    // Arrow keys
    'ArrowUp':    172,
    'ArrowDown':  173,
    'ArrowLeft':  174,
    'ArrowRight': 175,
    // Function keys
    'F1':         170,
    'F2':         156,
    'F3':         168,
    'F6':         289,
    'F7':         168,
    'F8':         169,
    'F9':         56,
    'F10':        57
};

// Reverse map: control ID to display name
const controlIdToName = {
    // Letters
    34:  'A',
    29:  'B',
    26:  'C',
    35:  'D',
    38:  'E',
    23:  'F',
    47:  'G',
    74:  'H',
    27:  'I',
    311: 'J',
    182: 'L',
    244: 'M',
    249: 'N',
    199: 'P',
    44:  'Q',
    45:  'R',
    33:  'S',
    245: 'T',
    303: 'U',
    0:   'V',
    32:  'W',
    73:  'X',
    246: 'Y',
    48:  'Z',
    // Symbols
    243: '~ (Tilde)',
    39:  '[',
    40:  ']',
    // Number keys
    157: '1',
    158: '2',
    160: '3',
    164: '4',
    165: '5',
    159: '6',
    161: '7',
    162: '8',
    163: '9',
    56:  '0',
    // Modifiers & common keys
    21:  'Shift',
    36:  'Ctrl',
    19:  'Alt',
    37:  'Tab',
    171: 'Caps Lock',
    22:  'Space',
    18:  'Enter',
    194: 'Backspace',
    // Navigation keys
    121: 'Insert',
    212: 'Home',
    213: 'End',
    214: 'Delete',
    10:  'Page Up',
    11:  'Page Down',
    // Arrow keys
    172: 'Arrow Up',
    173: 'Arrow Down',
    174: 'Arrow Left',
    175: 'Arrow Right',
    // Function keys
    170: 'F1',
    156: 'F2',
    168: 'F3',
    289: 'F6',
    169: 'F8',
    57:  'F10'
};

let gamemodes = [];
let bindings = {};
let listeningAction = null;
let rows = {};
let selectedGamemodeId = null;
let debugActions = {};
let selectedDebugGamemodeId = null;
let currentTab = 'home';
let debugEntities = [];
let selectedEntityId = null;
let maps = [];
let selectedMapId = null;
let deleteConfirmActive = false;
let boundariesShown = {};
let mapsRequested = false;

// Tab switching
function switchTab(name) {
    currentTab = name;

    document.querySelectorAll('.tab').forEach(function(el) {
        el.classList.add('hidden');
    });
    document.querySelectorAll('.nav-btn[data-tab]').forEach(function(el) {
        el.classList.remove('active');
    });
    var tab = document.getElementById('tab-' + name);
    if (tab) tab.classList.remove('hidden');
    var btn = document.querySelector('.nav-btn[data-tab="' + name + '"]');
    if (btn) btn.classList.add('active');

    // Reset controls state when leaving controls tab
    if (name !== 'controls') {
        stopListening();
        selectedGamemodeId = null;
        document.getElementById('controlsPanel').classList.add('hidden');
        // Deactivate gamemode buttons
        document.querySelectorAll('.controls-gm-btn').forEach(function(el) {
            el.classList.remove('active');
        });
    }

    // Reset debug state when leaving debug tab
    if (name !== 'debug') {
        selectedDebugGamemodeId = null;
        debugEntities = [];
        selectedEntityId = null;
        document.getElementById('debugPanel').classList.add('hidden');
        document.querySelectorAll('.debug-gm-btn').forEach(function(el) {
            el.classList.remove('active');
        });
    }

    // Reset maps state when leaving maps tab
    if (name !== 'maps') {
        deleteConfirmActive = false;
        // Notify C# that we left the maps tab so it can stop drawing boundaries
        fetch('https://gamemodecity/mapsTabClosed', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });
    }

    // Request maps from server when entering maps tab (only once per hub session)
    if (name === 'maps' && !mapsRequested) {
        mapsRequested = true;
        fetch('https://gamemodecity/requestMaps', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });
    }
}

// Render gamemode cards on Home tab
function renderGamemodeCards() {
    var grid = document.getElementById('gamemodeGrid');
    grid.innerHTML = '';
    gamemodes.forEach(function(gm) {
        var card = document.createElement('div');
        card.className = 'gamemode-card';
        card.style.borderLeftColor = gm.color;

        // Header row: title + player count
        var header = document.createElement('div');
        header.className = 'gamemode-card-header';

        var h3 = document.createElement('h3');
        h3.textContent = gm.name;
        header.appendChild(h3);

        if (gm.minPlayers > 0 && gm.maxPlayers > 0) {
            var badge = document.createElement('span');
            badge.className = 'gamemode-player-count';
            badge.textContent = gm.minPlayers + '-' + gm.maxPlayers;
            header.appendChild(badge);
        }

        card.appendChild(header);

        // Tag pills
        if (gm.tags && gm.tags.length > 0) {
            var tagsRow = document.createElement('div');
            tagsRow.className = 'gamemode-tags';
            gm.tags.forEach(function(tag) {
                var pill = document.createElement('span');
                pill.className = 'gamemode-tag';
                pill.textContent = tag;
                pill.style.background = gm.color + '22';
                pill.style.color = gm.color;
                tagsRow.appendChild(pill);
            });
            card.appendChild(tagsRow);
        }

        // Description
        var p = document.createElement('p');
        p.textContent = gm.description;
        card.appendChild(p);

        // Teams row
        if (gm.teams && gm.teams.length > 0) {
            var teamsRow = document.createElement('div');
            teamsRow.className = 'gamemode-info-row';
            var teamsLabel = document.createElement('span');
            teamsLabel.className = 'gamemode-info-label';
            teamsLabel.textContent = 'Teams: ';
            var teamsValues = document.createElement('span');
            teamsValues.className = 'gamemode-info-values';
            teamsValues.textContent = gm.teams.join(' \u00B7 ');
            teamsRow.appendChild(teamsLabel);
            teamsRow.appendChild(teamsValues);
            card.appendChild(teamsRow);
        }

        // Features row
        if (gm.features && gm.features.length > 0) {
            var featRow = document.createElement('div');
            featRow.className = 'gamemode-info-row';
            var featLabel = document.createElement('span');
            featLabel.className = 'gamemode-info-label';
            featLabel.textContent = '\u26A1 ';
            var featValues = document.createElement('span');
            featValues.className = 'gamemode-info-values';
            featValues.textContent = gm.features.join(' \u00B7 ');
            featRow.appendChild(featLabel);
            featRow.appendChild(featValues);
            card.appendChild(featRow);
        }

        grid.appendChild(card);
    });
}

// Render gamemode selector buttons on Controls tab
function renderControlsGamemodeList() {
    var list = document.getElementById('controlsGamemodeList');
    list.innerHTML = '';
    gamemodes.forEach(function(gm) {
        var btn = document.createElement('button');
        btn.className = 'controls-gm-btn';
        btn.textContent = gm.name;

        if (!gm.hasControls) {
            btn.classList.add('no-controls');
        } else {
            btn.addEventListener('click', function() {
                // Deactivate all
                document.querySelectorAll('.controls-gm-btn').forEach(function(el) {
                    el.classList.remove('active');
                });
                btn.classList.add('active');
                selectedGamemodeId = gm.id;

                // Request controls from C#
                fetch('https://gamemodecity/selectControlsGamemode', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ gamemodeId: gm.id })
                });
            });
        }

        list.appendChild(btn);
    });
}

// Render binding rows
function renderBindings() {
    var container = document.getElementById('bindingsContainer');
    container.innerHTML = '';
    rows = {};

    for (var action in bindings) {
        var info = bindings[action];
        var row = document.createElement('div');
        row.className = 'row';

        var name = document.createElement('span');
        name.className = 'action-name';
        name.textContent = info.actionName;

        var btn = document.createElement('button');
        btn.className = 'key-btn';
        btn.textContent = controlIdToName[info.controlId] || ('Control ' + info.controlId);

        (function(a) {
            btn.addEventListener('click', function() {
                startListening(a);
            });
        })(action);

        row.appendChild(name);
        row.appendChild(btn);
        container.appendChild(row);
        rows[action] = { btn: btn };
    }

    document.getElementById('controlsPanel').classList.remove('hidden');
}

// Render gamemode selector buttons on Debug tab
function renderDebugGamemodeList() {
    var list = document.getElementById('debugGamemodeList');
    list.innerHTML = '';
    gamemodes.forEach(function(gm) {
        var btn = document.createElement('button');
        btn.className = 'debug-gm-btn';
        btn.textContent = gm.name;

        var hasDebug = debugActions[gm.id] && Object.keys(debugActions[gm.id]).length > 0;
        if (!hasDebug) {
            btn.classList.add('no-debug');
        } else {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.debug-gm-btn').forEach(function(el) {
                    el.classList.remove('active');
                });
                btn.classList.add('active');
                selectedDebugGamemodeId = gm.id;
                debugEntities = [];
                selectedEntityId = null;
                renderDebugActions(gm.id);

                // Request entity list from C#
                fetch('https://gamemodecity/selectDebugGamemode', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ gamemodeId: gm.id })
                });
            });
        }

        list.appendChild(btn);
    });
}

// Render debug entities list
function renderDebugEntities() {
    var container = document.getElementById('debugEntityList');
    if (!container) return;
    container.innerHTML = '';

    debugEntities.forEach(function(entity) {
        var row = document.createElement('div');
        row.className = 'debug-entity-row';
        if (selectedEntityId === entity.id) {
            row.classList.add('selected');
        }

        var nameSpan = document.createElement('span');
        nameSpan.className = 'entity-name';
        nameSpan.textContent = entity.name;

        var badge = document.createElement('span');
        badge.className = 'entity-badge';
        if (entity.type === 'player') {
            badge.classList.add('entity-badge-player');
            badge.textContent = 'PLAYER';
        } else {
            badge.classList.add('entity-badge-bot');
            badge.textContent = 'BOT';
        }

        var team = document.createElement('span');
        team.className = 'entity-team';
        team.textContent = entity.team || '';

        row.appendChild(nameSpan);
        row.appendChild(badge);
        row.appendChild(team);

        (function(id) {
            row.addEventListener('click', function() {
                selectedEntityId = id;
                renderDebugEntities();
                updateTargetActionStates();
            });
        })(entity.id);

        container.appendChild(row);
    });
}

// Enable/disable target action buttons based on selection
function updateTargetActionStates() {
    document.querySelectorAll('.debug-action-btn[data-needs-target="true"]').forEach(function(btn) {
        if (selectedEntityId !== null) {
            btn.classList.remove('disabled');
        } else {
            btn.classList.add('disabled');
        }
    });
}

// Render debug actions for a gamemode grouped by category
function renderDebugActions(gamemodeId) {
    var container = document.getElementById('debugActionsContainer');
    container.innerHTML = '';

    var gmActions = debugActions[gamemodeId];
    if (!gmActions) return;

    // Insert entity list section at top
    var entitySection = document.createElement('div');
    entitySection.className = 'debug-category';

    var entityTitle = document.createElement('div');
    entityTitle.className = 'debug-category-title';
    entityTitle.textContent = 'Select Target';
    entitySection.appendChild(entityTitle);

    var entityList = document.createElement('div');
    entityList.className = 'debug-entity-list';
    entityList.id = 'debugEntityList';
    entitySection.appendChild(entityList);

    container.appendChild(entitySection);

    for (var category in gmActions) {
        var section = document.createElement('div');
        section.className = 'debug-category';

        var title = document.createElement('div');
        title.className = 'debug-category-title';
        title.textContent = category;
        section.appendChild(title);

        var grid = document.createElement('div');
        grid.className = 'debug-actions-grid';

        gmActions[category].forEach(function(action) {
            var btn = document.createElement('button');
            btn.className = 'debug-action-btn';
            btn.textContent = action.label;

            if (action.needsTarget) {
                btn.setAttribute('data-needs-target', 'true');
                if (selectedEntityId === null) {
                    btn.classList.add('disabled');
                }
            }

            (function(gId, aId, needs) {
                btn.addEventListener('click', function() {
                    if (needs && selectedEntityId === null) return;
                    var payload = { gamemodeId: gId, actionId: aId };
                    if (needs) {
                        payload.targetId = selectedEntityId;
                    }
                    fetch('https://gamemodecity/debugAction', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    });
                });
            })(gamemodeId, action.id, action.needsTarget);
            grid.appendChild(btn);
        });

        section.appendChild(grid);
        container.appendChild(section);
    }

    document.getElementById('debugPanel').classList.remove('hidden');

    // Render entities if we already have them
    renderDebugEntities();
}

function startListening(action) {
    if (listeningAction && rows[listeningAction]) {
        var prev = rows[listeningAction].btn;
        prev.classList.remove('listening');
        prev.textContent = controlIdToName[bindings[listeningAction].controlId] || ('Control ' + bindings[listeningAction].controlId);
    }
    listeningAction = action;
    rows[action].btn.classList.add('listening');
    rows[action].btn.textContent = 'Press any key...';
}

function stopListening() {
    if (listeningAction && rows[listeningAction]) {
        var btn = rows[listeningAction].btn;
        btn.classList.remove('listening');
        btn.textContent = controlIdToName[bindings[listeningAction].controlId] || ('Control ' + bindings[listeningAction].controlId);
    }
    listeningAction = null;
}

// ==================== Map Editor ====================

var spawnTypeNames = ['PLAYER', 'WEAPON', 'OBJECT'];

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

    // Gamemodes checkboxes
    var gmCheckboxes = document.querySelectorAll('#mapGamemodes input[type="checkbox"]');
    gmCheckboxes.forEach(function(cb) {
        cb.checked = map.gamemodes && map.gamemodes.indexOf(cb.value) !== -1;
    });

    // Update boundaries button text
    document.getElementById('mapBoundaries').textContent = boundariesShown[id] ? 'Hide Boundaries' : 'Show Boundaries';

    renderSpawnList();

    // Notify C# which map is selected for boundary drawing
    fetch('https://gamemodecity/selectMapForEdit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mapId: id })
    });
}

function round2(v) {
    return Math.round((v || 0) * 100) / 100;
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

    var gms = [];
    document.querySelectorAll('#mapGamemodes input[type="checkbox"]').forEach(function(cb) {
        if (cb.checked) gms.push(cb.value);
    });
    map.gamemodes = gms;
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
            });
        })(index);
        typeDiv.appendChild(sel);
        row.appendChild(typeDiv);

        // Team input
        var teamDiv = document.createElement('div');
        teamDiv.className = 'map-spawn-team';
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
                fetch('https://gamemodecity/teleportToSpawn', {
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
                fetch('https://gamemodecity/getPlayerPosition', {
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

// Map action button handlers
document.getElementById('mapCreateBtn').addEventListener('click', function() {
    fetch('https://gamemodecity/createMap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
    // Map will appear via 'mapCreated' NUI message from C#
});

document.getElementById('mapUseMyPos').addEventListener('click', function() {
    fetch('https://gamemodecity/getPlayerPosition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ context: 'useMyPos' })
    });
});

document.getElementById('mapAddSpawn').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    if (!map.spawns) map.spawns = [];

    fetch('https://gamemodecity/getPlayerPosition', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ context: 'addSpawn' })
    });
});

function minimizeHub() {
    document.getElementById('hub').classList.remove('visible');
    fetch('https://gamemodecity/minimizeHub', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
}

document.getElementById('mapTeleport').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    readFormIntoMap(map);
    fetch('https://gamemodecity/teleportToMap', {
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
    fetch('https://gamemodecity/toggleBoundaries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mapId: map.id, show: true, posX: map.posX, posY: map.posY, posZ: map.posZ, sizeX: map.sizeX, sizeY: map.sizeY, sizeZ: map.sizeZ })
    });
    minimizeHub();
});

document.getElementById('mapSave').addEventListener('click', function() {
    var map = getSelectedMap();
    if (!map) return;
    readFormIntoMap(map);
    fetch('https://gamemodecity/saveMap', {
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
    fetch('https://gamemodecity/deleteMap', {
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

function closeHub() {
    stopListening();
    document.getElementById('hub').classList.remove('visible');
    fetch('https://gamemodecity/closeHub', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
}

// Nav button clicks
document.querySelectorAll('.nav-btn[data-tab]').forEach(function(btn) {
    btn.addEventListener('click', function() {
        switchTab(btn.getAttribute('data-tab'));
    });
});

document.getElementById('closeBtn').addEventListener('click', closeHub);

document.getElementById('resetBtn').addEventListener('click', function() {
    fetch('https://gamemodecity/resetDefaults', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
});

// Keydown handler
document.addEventListener('keydown', function(e) {
    // Allow typing in input/select fields (for map editor etc.)
    var tag = e.target.tagName.toLowerCase();
    var isInput = (tag === 'input' || tag === 'textarea' || tag === 'select');

    if (isInput) {
        // Only intercept Escape in inputs
        if (e.key === 'Escape') {
            e.target.blur();
            e.preventDefault();
        }
        return;
    }

    e.preventDefault();
    e.stopPropagation();

    if (e.key === 'Escape') {
        if (listeningAction) {
            stopListening();
        } else {
            closeHub();
        }
        return;
    }

    if (!listeningAction) return;

    var key = e.key;
    if (key.length === 1) key = key.toLowerCase();

    var controlId = keyToControlId[key];
    if (controlId === undefined) return;

    bindings[listeningAction].controlId = controlId;

    rows[listeningAction].btn.classList.remove('listening');
    rows[listeningAction].btn.textContent = controlIdToName[controlId] || ('Control ' + controlId);

    var action = listeningAction;
    listeningAction = null;

    fetch('https://gamemodecity/setBind', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action: action, controlId: controlId })
    });
});

// TTT Overlay utilities
var tttTimers = {};

function showTTTOverlay(id, duration) {
    var el = document.getElementById(id);
    if (!el) return;
    clearTimeout(tttTimers[id]);
    el.classList.remove('fade-out');
    el.classList.add('active');
    tttTimers[id] = setTimeout(function() {
        hideTTTOverlay(id);
    }, duration);
}

function hideTTTOverlay(id) {
    var el = document.getElementById(id);
    if (!el) return;
    clearTimeout(tttTimers[id]);
    el.classList.add('fade-out');
    setTimeout(function() {
        el.classList.remove('active');
        el.classList.remove('fade-out');
    }, 500);
}

// Listen for messages from C#
window.addEventListener('message', function(event) {
    var data = event.data;

    if (data.type === 'openHub') {
        gamemodes = data.gamemodes || [];
        debugActions = data.debugActions || {};
        if (data.maps) {
            maps = data.maps;
            // If maps came with data, no need to request from server
            mapsRequested = maps.length > 0;
        } else {
            mapsRequested = false;
        }
        renderGamemodeCards();
        renderControlsGamemodeList();
        renderDebugGamemodeList();
        listeningAction = null;
        selectedGamemodeId = null;
        selectedDebugGamemodeId = null;
        debugEntities = [];
        selectedEntityId = null;
        deleteConfirmActive = false;
        document.getElementById('controlsPanel').classList.add('hidden');
        document.getElementById('debugPanel').classList.add('hidden');
        document.querySelectorAll('.controls-gm-btn').forEach(function(el) {
            el.classList.remove('active');
        });
        document.querySelectorAll('.debug-gm-btn').forEach(function(el) {
            el.classList.remove('active');
        });

        // Render maps if we have data
        if (data.maps) {
            renderMapList();
            // If a map was previously selected, re-select it
            if (selectedMapId !== null) {
                var stillExists = false;
                for (var i = 0; i < maps.length; i++) {
                    if (maps[i].id === selectedMapId) { stillExists = true; break; }
                }
                if (stillExists) {
                    selectMap(selectedMapId);
                } else {
                    selectedMapId = null;
                    document.getElementById('mapForm').classList.add('hidden');
                    document.getElementById('mapNoSelection').classList.remove('hidden');
                }
            }
        }

        switchTab(data.tab || currentTab || 'home');
        document.getElementById('hub').classList.add('visible');
    }
    else if (data.type === 'closeHub') {
        stopListening();
        document.getElementById('hub').classList.remove('visible');
    }
    else if (data.type === 'showControls') {
        bindings = data.bindings;
        renderBindings();
    }
    else if (data.type === 'updateControls') {
        bindings = data.bindings;
        renderBindings();
    }
    else if (data.type === 'showDebugEntities') {
        debugEntities = data.entities || [];
        selectedEntityId = null;
        renderDebugEntities();
        updateTargetActionStates();
    }
    else if (data.type === 'updateDebugEntities') {
        var entities = data.entities || [];
        // Preserve selection if entity still exists
        var oldSelection = selectedEntityId;
        debugEntities = entities;
        var stillExists = false;
        for (var i = 0; i < entities.length; i++) {
            if (entities[i].id === oldSelection) {
                stillExists = true;
                break;
            }
        }
        if (!stillExists) {
            selectedEntityId = null;
        }
        renderDebugEntities();
        updateTargetActionStates();
    }
    // Maps
    else if (data.type === 'updateMaps') {
        // Preserve any unsaved local map (id: -1) that the user just created
        var unsavedMap = null;
        for (var i = 0; i < maps.length; i++) {
            if (maps[i].id === -1) { unsavedMap = maps[i]; break; }
        }
        maps = data.maps || [];
        if (unsavedMap) {
            // Only re-add if server didn't already include it
            var serverHasIt = false;
            for (var j = 0; j < maps.length; j++) {
                if (maps[j].id === -1) { serverHasIt = true; break; }
            }
            if (!serverHasIt) maps.unshift(unsavedMap);
        }
        renderMapList();
        if (selectedMapId !== null) {
            var exists = false;
            for (var k = 0; k < maps.length; k++) {
                if (maps[k].id === selectedMapId) { exists = true; break; }
            }
            if (exists) {
                selectMap(selectedMapId);
            } else {
                selectedMapId = null;
                document.getElementById('mapForm').classList.add('hidden');
                document.getElementById('mapNoSelection').classList.remove('hidden');
            }
        }
    }
    else if (data.type === 'playerPosition') {
        var px = round2(data.x);
        var py = round2(data.y);
        var pz = round2(data.z);
        if (data.context === 'useMyPos') {
            document.getElementById('mapPosX').value = px;
            document.getElementById('mapPosY').value = py;
            document.getElementById('mapPosZ').value = pz;
        } else if (data.context === 'addSpawn') {
            var m = getSelectedMap();
            if (m) {
                if (!m.spawns) m.spawns = [];
                m.spawns.push({
                    id: -1,
                    posX: px, posY: py, posZ: pz,
                    spawnType: 0, entity: 'player', team: 0
                });
                renderSpawnList();
            }
        } else if (data.context === 'moveSpawn') {
            var m2 = getSelectedMap();
            if (m2 && m2.spawns && data.spawnIndex >= 0 && data.spawnIndex < m2.spawns.length) {
                m2.spawns[data.spawnIndex].posX = px;
                m2.spawns[data.spawnIndex].posY = py;
                m2.spawns[data.spawnIndex].posZ = pz;
                renderSpawnList();
            }
        }
    }
    else if (data.type === 'mapCreated') {
        // A new map was created in C# — add it to the local list and select it
        if (data.map) {
            maps.push(data.map);
            renderMapList();
            selectMap(data.map.id);
        }
    }
    else if (data.type === 'mapSaved') {
        // A map was saved in C# — update the local copy (handles id change from -1 to real id)
        if (data.map) {
            var oldId = data.oldId;
            var found = false;
            for (var i = 0; i < maps.length; i++) {
                if (maps[i].id === oldId) {
                    maps[i] = data.map;
                    found = true;
                    break;
                }
            }
            if (!found) {
                maps.push(data.map);
            }
            selectedMapId = data.map.id;
            renderMapList();
            selectMap(data.map.id);
        }
    }
    // TTT Overlays
    else if (data.type === 'tttRoleReveal') {
        document.getElementById('tttRoleName').textContent = data.team;
        document.getElementById('tttRoleName').style.color = data.color;
        document.getElementById('tttRoleName').style.textShadow = '0 0 40px ' + data.color + ', 0 0 80px ' + data.color;
        document.getElementById('tttRoleLine').style.background = data.color;
        showTTTOverlay('ttt-role-reveal', 3000);
    }
    else if (data.type === 'tttCountdown') {
        var numEl = document.getElementById('tttCountdownNumber');
        numEl.textContent = data.count === 0 ? 'GO' : data.count;
        // Remove and re-add active to retrigger animation
        var overlay = document.getElementById('ttt-countdown');
        overlay.classList.remove('active');
        overlay.classList.remove('fade-out');
        void overlay.offsetWidth; // Force reflow
        overlay.classList.add('active');
        clearTimeout(tttTimers['ttt-countdown']);
        tttTimers['ttt-countdown'] = setTimeout(function() {
            hideTTTOverlay('ttt-countdown');
        }, 900);
    }
    else if (data.type === 'tttRoundEnd') {
        var winnerEl = document.getElementById('tttRoundEndWinner');
        winnerEl.textContent = data.winner;
        winnerEl.style.color = data.color;
        winnerEl.style.textShadow = '0 0 40px ' + data.color + ', 0 0 80px ' + data.color;
        document.getElementById('tttRoundEndReason').textContent = data.reason;
        showTTTOverlay('ttt-round-end', 8000);
    }
    else if (data.type === 'tttBodyInspect') {
        document.getElementById('tttBodyName').textContent = data.name;
        var teamEl = document.getElementById('tttBodyTeam');
        teamEl.textContent = data.team;
        teamEl.style.color = data.teamColor;
        var unknownText = 'Unknown - requires Detective';
        document.getElementById('tttBodyWeapon').textContent = data.weapon || unknownText;
        document.getElementById('tttBodyDeathTime').textContent = data.deathTime || unknownText;
        showTTTOverlay('ttt-body-inspect', 5000);
    }
});
