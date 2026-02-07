// Map JS key identifiers to FiveM control IDs (F5 omitted — reserved for hub toggle)
const keyToControlId = {
    'm':          244,
    '`':          243,
    '~':          243,
    'e':          38,
    'f':          23,
    'g':          47,
    'h':          74,
    'q':          44,
    'z':          48,
    'x':          73,
    'Control':    36,
    'Tab':        37,
    'CapsLock':   171,
    'Insert':     121,
    'Home':       212,
    'End':        213,
    'Delete':     214,
    'F2':         156,
    'F6':         289,
    'F1':         170,
    'b':          29,
    'n':          249,
    'v':          0
};

// Reverse map: control ID to display name
const controlIdToName = {
    244: 'M',
    243: '~ (Tilde)',
    38:  'E',
    23:  'F',
    47:  'G',
    74:  'H',
    44:  'Q',
    48:  'Z',
    73:  'X',
    36:  'Ctrl',
    37:  'Tab',
    171: 'Caps Lock',
    121: 'Insert',
    212: 'Home',
    213: 'End',
    214: 'Delete',
    156: 'F2',
    289: 'F6',
    170: 'F1',
    29:  'B',
    249: 'N',
    0:   'V'
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
}

// Render gamemode cards on Home tab
function renderGamemodeCards() {
    var grid = document.getElementById('gamemodeGrid');
    grid.innerHTML = '';
    gamemodes.forEach(function(gm) {
        var card = document.createElement('div');
        card.className = 'gamemode-card';
        card.style.borderLeftColor = gm.color;

        var h3 = document.createElement('h3');
        h3.textContent = gm.name;

        var p = document.createElement('p');
        p.textContent = gm.description;

        card.appendChild(h3);
        card.appendChild(p);
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
        renderGamemodeCards();
        renderControlsGamemodeList();
        renderDebugGamemodeList();
        listeningAction = null;
        selectedGamemodeId = null;
        selectedDebugGamemodeId = null;
        debugEntities = [];
        selectedEntityId = null;
        document.getElementById('controlsPanel').classList.add('hidden');
        document.getElementById('debugPanel').classList.add('hidden');
        document.querySelectorAll('.controls-gm-btn').forEach(function(el) {
            el.classList.remove('active');
        });
        document.querySelectorAll('.debug-gm-btn').forEach(function(el) {
            el.classList.remove('active');
        });
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
