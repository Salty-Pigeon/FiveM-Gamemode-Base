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
                fetch('https://gta_gameroo/selectDebugGamemode', {
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
                    fetch('https://gta_gameroo/debugAction', {
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
