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
        if (guideOpen) {
            hideGuide();
            return;
        }
        if (voteIsOpen) {
            // Release NUI focus but keep overlay visible
            fetch('https://gamemodecity/closeVote', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({})
            });
            return;
        }
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

// Listen for messages from C#
window.addEventListener('message', function(event) {
    var data = event.data;

    if (data.type === 'openHub') {
        gamemodes = data.gamemodes || [];
        debugActions = data.debugActions || {};
        if (data.progression) {
            progression = data.progression;
        }
        if (data.pedModels) {
            pedModels = data.pedModels;
        }
        if (data.maps) {
            maps = data.maps;
            // If maps came with data, no need to request from server
            mapsRequested = maps.length > 0;
        } else {
            mapsRequested = false;
        }
        hideGuide();
        renderGamemodeCards();
        renderControlsGamemodeList();
        renderDebugGamemodeList();
        updateStatsBar();
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

        updateTabVisibility();
        switchTab(data.tab || currentTab || 'home');
        document.getElementById('hub').classList.add('visible');
    }
    else if (data.type === 'closeHub') {
        stopListening();
        document.getElementById('hub').classList.remove('visible');
        document.getElementById('hub').classList.remove('shop-preview-active');
    }
    else if (data.type === 'updateProgression') {
        progression.xp = data.xp;
        progression.level = data.level;
        progression.tokens = data.tokens;
        progression.unlockedModels = data.unlockedModels || [];
        progression.selectedModel = data.selectedModel || '';
        progression.unlockedItems = data.unlockedItems || [];
        progression.adminLevel = data.adminLevel || 0;
        updateStatsBar();
        updateTabVisibility();
        if (customizeActive && currentSection) {
            renderCurrentSection();
        }
        if (data.leveledUp) {
            showLevelUpToast(data.level);
        }
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
                    heading: round2(data.heading || 0),
                    spawnType: 0, entity: 'player', team: 0,
                    sizeX: 0, sizeY: 0
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
    // Game Timer
    else if (data.type === 'updateGameTimer') {
        var timerEl = document.getElementById('game-timer');
        var textEl = document.getElementById('gameTimerText');
        textEl.textContent = data.time;
        timerEl.classList.add('active');
        if (data.urgent) {
            timerEl.classList.add('urgent');
        } else {
            timerEl.classList.remove('urgent');
        }
    }
    else if (data.type === 'hideGameTimer') {
        var timerEl = document.getElementById('game-timer');
        timerEl.classList.remove('active');
        timerEl.classList.remove('urgent');
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
        var overlay = document.getElementById('ttt-countdown');
        var isGo = data.count === 0;

        numEl.textContent = isGo ? 'GO' : data.count;

        // Toggle go state for green styling + flash
        overlay.classList.remove('countdown-go');
        if (isGo) {
            void overlay.offsetWidth;
            overlay.classList.add('countdown-go');
        }

        // Remove and re-add active to retrigger all animations (number, pulse, vignette)
        overlay.classList.remove('active');
        overlay.classList.remove('fade-out');
        void overlay.offsetWidth; // Force reflow
        overlay.classList.add('active');
        clearTimeout(tttTimers['ttt-countdown']);
        tttTimers['ttt-countdown'] = setTimeout(function() {
            hideTTTOverlay('ttt-countdown');
            overlay.classList.remove('countdown-go');
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
    // Vote Overlay
    else if (data.type === 'openVote') {
        voteGamemodes = data.gamemodes || [];
        voteSelectedId = null;
        voteVoters = {};
        voteIsOpen = true;
        renderVoteCards();
        var overlay = document.getElementById('vote-overlay');
        overlay.classList.remove('fade-out');
        overlay.classList.add('active');
        startVoteTimer(data.duration || 30);
    }
    else if (data.type === 'updateVotes') {
        voteVoters = data.votes || {};
        updateVoteDisplay();
    }
    else if (data.type === 'voteWinner') {
        showVoteWinner(data.winnerId);
    }
    else if (data.type === 'closeVote') {
        closeVoteOverlay();
    }
    // Admin panel
    else if (data.type === 'onlinePlayers') {
        renderAdminOnlinePlayers(data.players || []);
    }
    else if (data.type === 'adminResult') {
        var r = data.data || {};
        showAdminStatus(r.message || '', r.success);
        if (r.success) {
            // Refresh player list
            fetch('https://gamemodecity/getOnlinePlayers', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({})
            });
        }
    }
    else if (data.type === 'lookupResult') {
        var lr = data.data || {};
        var el = document.getElementById('adminLookupResult');
        if (lr.found) {
            el.innerHTML = '<strong>' + lr.name + '</strong> — Level ' + lr.adminLevel + ' (' + (adminLevelNames[lr.adminLevel] || 'Unknown') + ')';
        } else {
            el.textContent = 'Player not found in database';
        }
    }
    else if (data.type === 'previewPedPosition') {
        var ex = document.getElementById('adminPedX');
        var ey = document.getElementById('adminPedY');
        var ez = document.getElementById('adminPedZ');
        var eh = document.getElementById('adminPedH');
        if (ex) ex.value = data.x.toFixed(4);
        if (ey) ey.value = data.y.toFixed(4);
        if (ez) ez.value = data.z.toFixed(4);
        if (eh && data.heading !== undefined) eh.value = data.heading.toFixed(1);
    }
});

// Hide restricted tabs on initial load
updateTabVisibility();
