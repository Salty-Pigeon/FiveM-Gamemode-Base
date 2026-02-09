// ==================== Vote Overlay ====================

function renderVoteCards() {
    var grid = document.getElementById('voteGrid');
    grid.innerHTML = '';

    voteGamemodes.forEach(function(gm) {
        var card = document.createElement('div');
        card.className = 'vote-card';
        card.setAttribute('data-gm-id', gm.id);

        if (voteSelectedId === gm.id) {
            card.classList.add('selected');
        }

        // Color banner
        var banner = document.createElement('div');
        banner.className = 'vote-card-banner';
        banner.style.background = gm.color;
        card.appendChild(banner);

        var body = document.createElement('div');
        body.className = 'vote-card-body';

        // Header: name + tally
        var header = document.createElement('div');
        header.className = 'vote-card-header';

        var name = document.createElement('div');
        name.className = 'vote-card-name';
        name.textContent = gm.name;
        header.appendChild(name);

        var tally = document.createElement('div');
        tally.className = 'vote-card-tally';
        tally.id = 'vote-tally-' + gm.id;
        var voters = voteVoters[gm.id] || [];
        tally.textContent = voters.length > 0 ? voters.length : '';
        header.appendChild(tally);

        body.appendChild(header);

        // Tags
        if (gm.tags && gm.tags.length > 0) {
            var tagsRow = document.createElement('div');
            tagsRow.className = 'vote-card-tags';
            gm.tags.forEach(function(tag) {
                var pill = document.createElement('span');
                pill.className = 'vote-card-tag';
                pill.textContent = tag;
                pill.style.background = gm.color + '22';
                pill.style.color = gm.color;
                tagsRow.appendChild(pill);
            });
            body.appendChild(tagsRow);
        }

        // Description
        var desc = document.createElement('div');
        desc.className = 'vote-card-desc';
        desc.textContent = gm.description;
        body.appendChild(desc);

        // Player count
        if (gm.minPlayers > 0 && gm.maxPlayers > 0) {
            var players = document.createElement('div');
            players.className = 'vote-card-players';
            players.textContent = gm.minPlayers + '-' + gm.maxPlayers + ' players';
            body.appendChild(players);
        }

        // Voter pills
        var votersDiv = document.createElement('div');
        votersDiv.className = 'vote-card-voters';
        votersDiv.id = 'vote-voters-' + gm.id;
        voters.forEach(function(voterName) {
            var pill = document.createElement('span');
            pill.className = 'vote-voter-pill';
            pill.textContent = voterName;
            votersDiv.appendChild(pill);
        });
        body.appendChild(votersDiv);

        card.appendChild(body);

        // Click handler
        (function(gmId) {
            card.addEventListener('click', function() {
                if (card.classList.contains('winner') || card.classList.contains('loser')) return;
                voteSelectedId = gmId;
                // Update selection visuals
                document.querySelectorAll('.vote-card').forEach(function(c) {
                    c.classList.remove('selected');
                });
                card.classList.add('selected');
                // Send vote to server
                fetch('https://gta_gameroo/castVote', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ gamemodeId: gmId })
                });
            });
        })(gm.id);

        grid.appendChild(card);
    });
}

function updateVoteDisplay() {
    var totalVotes = 0;
    for (var key in voteVoters) {
        totalVotes += voteVoters[key].length;
    }

    // Update vote count with bump
    var countEl = document.getElementById('voteCount');
    countEl.textContent = totalVotes + (totalVotes === 1 ? ' vote' : ' votes');
    countEl.classList.add('bump');
    setTimeout(function() { countEl.classList.remove('bump'); }, 200);

    // Update each card's tally and voter pills
    voteGamemodes.forEach(function(gm) {
        var voters = voteVoters[gm.id] || [];
        var tallyEl = document.getElementById('vote-tally-' + gm.id);
        if (tallyEl) {
            tallyEl.textContent = voters.length > 0 ? voters.length : '';
            tallyEl.classList.add('bump');
            setTimeout(function() { tallyEl.classList.remove('bump'); }, 200);
        }

        var votersDiv = document.getElementById('vote-voters-' + gm.id);
        if (votersDiv) {
            votersDiv.innerHTML = '';
            voters.forEach(function(voterName) {
                var pill = document.createElement('span');
                pill.className = 'vote-voter-pill';
                pill.textContent = voterName;
                votersDiv.appendChild(pill);
            });
        }
    });
}

function startVoteTimer(duration) {
    voteDuration = duration;
    voteTimeLeft = duration;
    clearInterval(voteTimerInterval);

    updateTimerDisplay();

    voteTimerInterval = setInterval(function() {
        voteTimeLeft--;
        if (voteTimeLeft < 0) voteTimeLeft = 0;
        updateTimerDisplay();
        if (voteTimeLeft <= 0) {
            clearInterval(voteTimerInterval);
        }
    }, 1000);
}

function updateTimerDisplay() {
    var fill = document.getElementById('voteTimerFill');
    var text = document.getElementById('voteTimerText');
    var pct = (voteTimeLeft / voteDuration) * 100;
    fill.style.width = pct + '%';
    text.textContent = voteTimeLeft + 's';

    if (voteTimeLeft <= 10) {
        fill.classList.add('urgent');
    } else {
        fill.classList.remove('urgent');
    }
}

function showVoteWinner(winnerId) {
    clearInterval(voteTimerInterval);

    document.querySelectorAll('.vote-card').forEach(function(card) {
        var gmId = card.getAttribute('data-gm-id');
        card.style.cursor = 'default';
        if (gmId === winnerId) {
            card.classList.remove('selected');
            card.classList.add('winner');
            // Insert winner banner after color banner
            var banner = card.querySelector('.vote-card-banner');
            if (banner) {
                var winBanner = document.createElement('div');
                winBanner.className = 'vote-card-winner-banner';
                winBanner.textContent = 'WINNER';
                banner.insertAdjacentElement('afterend', winBanner);
            }
        } else {
            card.classList.remove('selected');
            card.classList.add('loser');
        }
    });

    // Auto-hide vote tab after 4s
    setTimeout(function() {
        endVoteSession();
    }, 4000);
}

function endVoteSession() {
    clearInterval(voteTimerInterval);
    // Reset state
    voteGamemodes = [];
    voteSelectedId = null;
    voteVoters = {};
    voteIsOpen = false;
    hideVoteTab();
}

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

// ==================== Gun Game Overlays ====================

function showGGLevelHud() {
    var hud = document.getElementById('gg-level-hud');
    var lb = document.getElementById('gg-leaderboard');
    if (hud) hud.style.display = 'block';
    if (lb) lb.style.display = 'block';
}

function hideGGLevelHud() {
    var hud = document.getElementById('gg-level-hud');
    var lb = document.getElementById('gg-leaderboard');
    if (hud) hud.style.display = 'none';
    if (lb) lb.style.display = 'none';
}

function updateGGLevel(level, weaponName, maxLevel) {
    var numEl = document.getElementById('ggLevelNumber');
    var wepEl = document.getElementById('ggLevelWeapon');
    var fillEl = document.getElementById('ggProgressFill');
    if (numEl) numEl.textContent = level;
    if (wepEl) wepEl.textContent = weaponName;
    if (fillEl) {
        var pct = maxLevel > 0 ? ((level / maxLevel) * 100) : 0;
        fillEl.style.width = pct + '%';
    }
}

function updateGGLeaderboard(players) {
    var list = document.getElementById('ggLbList');
    if (!list) return;

    // Sort by level descending
    players.sort(function(a, b) { return b.level - a.level; });

    var highestLevel = players.length > 0 ? players[0].level : 0;

    list.innerHTML = '';
    players.forEach(function(p) {
        var entry = document.createElement('div');
        entry.className = 'gg-lb-entry';
        if (p.level === highestLevel && highestLevel > 0) {
            entry.classList.add('leader');
        }
        if (p.isLocal) {
            entry.classList.add('local');
        }

        var name = document.createElement('span');
        name.className = 'gg-lb-entry-name';
        name.textContent = p.name;
        entry.appendChild(name);

        var lvl = document.createElement('span');
        lvl.className = 'gg-lb-entry-level';
        lvl.textContent = 'Lv.' + p.level;
        entry.appendChild(lvl);

        list.appendChild(entry);
    });
}
