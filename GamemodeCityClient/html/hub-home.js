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

        (function(gmData) {
            card.addEventListener('click', function() {
                showGuide(gmData);
            });
        })(gm);

        grid.appendChild(card);
    });
}

// Guide detail view
function showGuide(gm) {
    guideOpen = true;
    document.getElementById('gamemodeGrid').classList.add('hidden');
    var subtitle = document.querySelector('#tab-home .tab-subtitle');
    var title = document.querySelector('#tab-home h1');
    if (subtitle) subtitle.classList.add('hidden');
    if (title) title.classList.add('hidden');
    var detail = document.getElementById('guideDetail');
    detail.classList.remove('hidden');

    // Render hero
    var hero = document.getElementById('guideHero');
    hero.innerHTML = '';
    var accent = document.createElement('div');
    accent.className = 'guide-hero-accent';
    accent.style.background = gm.color;
    hero.appendChild(accent);

    var body = document.createElement('div');
    body.className = 'guide-hero-body';

    var header = document.createElement('div');
    header.className = 'guide-hero-header';
    var name = document.createElement('div');
    name.className = 'guide-hero-name';
    name.textContent = gm.name;
    header.appendChild(name);
    if (gm.minPlayers > 0 && gm.maxPlayers > 0) {
        var players = document.createElement('span');
        players.className = 'guide-hero-players';
        players.textContent = gm.minPlayers + '-' + gm.maxPlayers + ' players';
        header.appendChild(players);
    }
    body.appendChild(header);

    var desc = document.createElement('div');
    desc.className = 'guide-hero-desc';
    desc.textContent = gm.description;
    body.appendChild(desc);

    if (gm.tags && gm.tags.length > 0) {
        var tags = document.createElement('div');
        tags.className = 'guide-hero-tags';
        gm.tags.forEach(function(tag) {
            var pill = document.createElement('span');
            pill.className = 'guide-hero-tag';
            pill.textContent = tag;
            pill.style.background = gm.color + '22';
            pill.style.color = gm.color;
            tags.appendChild(pill);
        });
        body.appendChild(tags);
    }

    hero.appendChild(body);

    // Render guide content
    var content = document.getElementById('guideContent');
    content.innerHTML = '';

    if (!gm.guide) {
        var soon = document.createElement('div');
        soon.className = 'guide-coming-soon';
        soon.textContent = 'Guide coming soon';
        content.appendChild(soon);
        return;
    }

    var sections = document.createElement('div');
    sections.className = 'guide-sections';

    // Overview
    if (gm.guide.overview) {
        var sec = document.createElement('div');
        sec.className = 'guide-section';
        var t = document.createElement('div');
        t.className = 'guide-section-title';
        t.textContent = 'Overview';
        sec.appendChild(t);
        var txt = document.createElement('div');
        txt.className = 'guide-section-text';
        txt.textContent = gm.guide.overview;
        sec.appendChild(txt);
        sections.appendChild(sec);
    }

    // How to Win
    if (gm.guide.howToWin) {
        var sec = document.createElement('div');
        sec.className = 'guide-section';
        var t = document.createElement('div');
        t.className = 'guide-section-title';
        t.textContent = 'How to Win';
        sec.appendChild(t);
        var txt = document.createElement('div');
        txt.className = 'guide-section-text';
        txt.textContent = gm.guide.howToWin;
        sec.appendChild(txt);
        sections.appendChild(sec);
    }

    // Rules
    if (gm.guide.rules && gm.guide.rules.length > 0) {
        var sec = document.createElement('div');
        sec.className = 'guide-section';
        var t = document.createElement('div');
        t.className = 'guide-section-title';
        t.textContent = 'Rules';
        sec.appendChild(t);
        var ul = document.createElement('ul');
        ul.className = 'guide-rule-list';
        gm.guide.rules.forEach(function(rule) {
            var li = document.createElement('li');
            li.className = 'guide-rule';
            li.textContent = rule;
            ul.appendChild(li);
        });
        sec.appendChild(ul);
        sections.appendChild(sec);
    }

    // Team Roles
    if (gm.guide.teamRoles && gm.guide.teamRoles.length > 0) {
        var sec = document.createElement('div');
        sec.className = 'guide-section';
        var t = document.createElement('div');
        t.className = 'guide-section-title';
        t.textContent = 'Team Roles';
        sec.appendChild(t);
        var grid = document.createElement('div');
        grid.className = 'guide-roles-grid';
        gm.guide.teamRoles.forEach(function(role) {
            var card = document.createElement('div');
            card.className = 'guide-role-card';
            card.style.borderLeftColor = role.color;

            var rName = document.createElement('div');
            rName.className = 'guide-role-name';
            rName.textContent = role.name;
            rName.style.color = role.color;
            card.appendChild(rName);

            var rGoal = document.createElement('div');
            rGoal.className = 'guide-role-goal';
            rGoal.textContent = role.goal;
            card.appendChild(rGoal);

            if (role.tips && role.tips.length > 0) {
                var tips = document.createElement('ul');
                tips.className = 'guide-role-tips';
                role.tips.forEach(function(tip) {
                    var li = document.createElement('li');
                    li.className = 'guide-role-tip';
                    li.textContent = tip;
                    tips.appendChild(li);
                });
                card.appendChild(tips);
            }

            grid.appendChild(card);
        });
        sec.appendChild(grid);
        sections.appendChild(sec);
    }

    // Tips
    if (gm.guide.tips && gm.guide.tips.length > 0) {
        var sec = document.createElement('div');
        sec.className = 'guide-section';
        var t = document.createElement('div');
        t.className = 'guide-section-title';
        t.textContent = 'Tips';
        sec.appendChild(t);
        var ul = document.createElement('ul');
        ul.className = 'guide-tip-list';
        gm.guide.tips.forEach(function(tip) {
            var li = document.createElement('li');
            li.className = 'guide-tip';
            li.textContent = tip;
            ul.appendChild(li);
        });
        sec.appendChild(ul);
        sections.appendChild(sec);
    }

    content.appendChild(sections);
}

function hideGuide() {
    if (!guideOpen) return;
    guideOpen = false;
    document.getElementById('guideDetail').classList.add('hidden');
    document.getElementById('gamemodeGrid').classList.remove('hidden');
    var subtitle = document.querySelector('#tab-home .tab-subtitle');
    var title = document.querySelector('#tab-home h1');
    if (subtitle) subtitle.classList.remove('hidden');
    if (title) title.classList.remove('hidden');
    document.getElementById('guideHero').innerHTML = '';
    document.getElementById('guideContent').innerHTML = '';
}

document.getElementById('guideBackBtn').addEventListener('click', hideGuide);

// ==================== Progression / Stats ====================

function updateStatsBar() {
    var xpNeeded = 200;
    var pct = xpNeeded > 0 ? Math.min((progression.xp / xpNeeded) * 100, 100) : 0;
    document.getElementById('statsLevelBadge').textContent = 'LVL ' + progression.level;
    document.getElementById('statsXpFill').style.width = pct + '%';
    document.getElementById('statsXpText').textContent = progression.xp + ' / ' + xpNeeded + ' XP';
    document.getElementById('statsTokens').textContent = progression.tokens + ' Tokens';
    document.getElementById('shopTokenBalance').textContent = progression.tokens + ' Tokens';
}

function showLevelUpToast(level) {
    var toast = document.getElementById('levelupToast');
    document.getElementById('levelupToastSub').textContent = 'Level ' + level + ' - +100 Tokens';
    toast.classList.add('show');
    setTimeout(function() {
        toast.classList.remove('show');
    }, 3000);
}
