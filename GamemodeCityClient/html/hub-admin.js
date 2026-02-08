// ==================== Admin Panel ====================

var adminLevelNames = ['Player', 'Moderator', 'Admin', 'Owner'];

function renderAdminOnlinePlayers(players) {
    var list = document.getElementById('adminPlayerList');
    list.innerHTML = '';
    if (!players || players.length === 0) {
        list.innerHTML = '<div class="admin-empty">No players online</div>';
        return;
    }
    players.forEach(function(p) {
        var row = document.createElement('div');
        row.className = 'admin-player-row';

        var name = document.createElement('span');
        name.className = 'admin-player-name';
        name.textContent = p.name;
        row.appendChild(name);

        var lic = document.createElement('span');
        lic.className = 'admin-player-license';
        lic.textContent = p.license.length > 24 ? p.license.substring(0, 24) + '...' : p.license;
        lic.title = p.license;
        row.appendChild(lic);

        var badge = document.createElement('span');
        badge.className = 'admin-level-badge admin-level-' + p.adminLevel;
        badge.textContent = p.adminLevel + ' ' + (adminLevelNames[p.adminLevel] || '');
        row.appendChild(badge);

        if (p.adminLevel < 3) {
            var select = document.createElement('select');
            select.className = 'admin-select';
            for (var i = 0; i <= 2; i++) {
                var opt = document.createElement('option');
                opt.value = i;
                opt.textContent = i + ' - ' + adminLevelNames[i];
                if (i === p.adminLevel) opt.selected = true;
                select.appendChild(opt);
            }
            row.appendChild(select);

            var btn = document.createElement('button');
            btn.className = 'btn admin-action-btn';
            btn.textContent = 'Set';
            (function(license, sel) {
                btn.addEventListener('click', function() {
                    fetch('https://gamemodecity/setAdminLevel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ license: license, level: parseInt(sel.value) })
                    });
                });
            })(p.license, select);
            row.appendChild(btn);
        }

        list.appendChild(row);
    });
}

function showAdminStatus(message, isSuccess) {
    var el = document.getElementById('adminStatus');
    el.textContent = message;
    el.className = 'admin-status ' + (isSuccess ? 'success' : 'error');
    setTimeout(function() { el.textContent = ''; el.className = 'admin-status'; }, 4000);
}

// Admin panel buttons
document.getElementById('adminManualSet').addEventListener('click', function() {
    var license = document.getElementById('adminManualLicense').value.trim();
    var level = parseInt(document.getElementById('adminManualLevel').value);
    if (!license) return;
    fetch('https://gamemodecity/setAdminLevel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ license: license, level: level })
    });
});

document.getElementById('adminLookupBtn').addEventListener('click', function() {
    var license = document.getElementById('adminLookupLicense').value.trim();
    if (!license) return;
    fetch('https://gamemodecity/lookupPlayer', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ license: license })
    });
});
