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
