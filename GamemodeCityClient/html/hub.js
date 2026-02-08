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
let guideOpen = false;

// Progression state
let progression = { xp: 0, level: 1, tokens: 0, unlockedModels: [], selectedModel: '', unlockedItems: [] };
let pedModels = [];
let shopCategory = 'All';

// Customization state
let customizeSettings = null;
let currentAppearance = null;
let currentSection = 'model';
let customizeActive = false;

// Vote state
let voteGamemodes = [];
let voteSelectedId = null;
let voteVoters = {};
let voteDuration = 30;
let voteTimeLeft = 30;
let voteTimerInterval = null;
let voteIsOpen = false;

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

    // Reset guide state when leaving home tab
    if (name !== 'home') {
        hideGuide();
    }

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

    // Customization management
    if (name === 'shop') {
        document.getElementById('hub').classList.add('shop-preview-active');
        if (!customizeActive) {
            enterCustomization();
        }
    } else {
        if (customizeActive) {
            exitCustomization(true);
        }
        document.getElementById('hub').classList.remove('shop-preview-active');
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

// ==================== Character Customization ====================

// Hair colors (GTA V hair color palette - approximate hex values)
var hairColors = [
    '#0a0a0a','#1c1c1c','#2e1e0e','#3b2213','#4a2a14','#593216','#6b3a18','#7a4420',
    '#8b5e34','#9a7040','#a0824c','#b09060','#c0a070','#d0b888','#e0c898','#f0dca8',
    '#8b0000','#a02020','#b03030','#c04040','#d05050','#e06060','#f07070','#ff8888',
    '#4a1a4a','#5a2a5a','#6a3a6a','#7a4a7a','#8a5a8a','#2a4a2a','#1a3a5a','#4a4a1a',
    '#222','#333','#444','#555','#666','#777','#888','#999','#aaa','#bbb','#ccc',
    '#ddd','#eee','#2a1506','#3a2010','#4a2a1a','#5a3520','#6a4030','#7a5040','#8a6050',
    '#9a7060','#aa8070','#ba9080','#caa090','#dab0a0','#eac0b0','#fad0c0','#ffe0d0'
];

// Eye color names matching AppearanceConstants
var eyeColorNames = [
    'Green','Emerald','Light Blue','Ocean Blue','Light Brown','Dark Brown','Hazel',
    'Dark Gray','Light Gray','Pink','Yellow','Purple','Blackout','Shades of Gray',
    'Tequila Sunrise','Neon','Red Inferno','Alien Blue','Gold Dust','Amber',
    'Sea Green','Mist','Retro','Mint','Tiger Eye','Mocha','Sterling','Midnight',
    'Blaze','Frost','Vivid'
];

// FiveM cb(string) double-JSON-encodes: r.json() returns a string, not an object.
// This helper parses the inner JSON if needed.
function nuiResp(r) {
    return r.json().then(function(d) {
        if (typeof d === 'string') { try { return JSON.parse(d); } catch(e) { return d; } }
        return d;
    });
}

var componentNames = ['Face','Mask','Hair','Torso','Legs','Bag','Shoes','Accessory','Undershirt','Armor','Decals','Tops'];
var propNames = { 0:'Hats', 1:'Glasses', 2:'Ears', 6:'Watch', 7:'Bracelet' };
var propIndices = [0, 1, 2, 6, 7];
var faceFeatureNames = [
    'Nose Width','Nose Peak Height','Nose Peak Length','Nose Bone Height','Nose Peak Lowering',
    'Nose Bone Twist','Eyebrow Height','Eyebrow Depth','Cheekbone Height','Cheekbone Width',
    'Cheek Width','Eye Opening','Lip Thickness','Jaw Bone Width','Jaw Bone Shape',
    'Chin Height','Chin Length','Chin Width','Chin Hole Size','Neck Thickness'
];
var overlayNames = [
    'Blemishes','Facial Hair','Eyebrows','Ageing','Makeup','Blush',
    'Complexion','Sun Damage','Lipstick','Moles/Freckles','Chest Hair','Body Blemishes'
];

var settingsVersion = 0; // Track which settings response is latest

function changePreviewModel(hash) {
    settingsVersion++;
    console.log('[shop] changePreviewModel:', hash);
    fetch('https://gamemodecity/changeModel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ modelHash: hash })
    }).then(nuiResp).then(function(data) {
        console.log('[shop] changeModel response:', JSON.stringify(data));
        if (data.status === 'ok') {
            customizeSettings = data.settings;
            currentAppearance = data.appearance;
            console.log('[shop] isFreemode:', customizeSettings && customizeSettings.isFreemode);
            updateSubTabStates();
            renderCurrentSection();
        }
    }).catch(function(err) {
        console.log('[shop] changeModel ERROR:', err);
    });
}

function enterCustomization() {
    previewingModelHash = null;
    purchaseConfirmHash = null;

    // Immediately show the model section (it only needs pedModels + progression)
    customizeActive = true;
    currentSection = 'model';
    renderCurrentSection();
    updateSubTabStates(); // Disable non-model tabs until ped ready

    // Spawn preview ped in background — update settings when ready
    settingsVersion++;
    var myVersion = settingsVersion;
    console.log('[shop] enterCustomization: firing customizeStart');
    fetch('https://gamemodecity/customizeStart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    }).then(nuiResp).then(function(data) {
        console.log('[shop] customizeStart response:', JSON.stringify(data), 'myVersion:', myVersion, 'settingsVersion:', settingsVersion);
        if (data.status !== 'ok') return;
        // Only apply if user hasn't changed model since we started
        if (settingsVersion !== myVersion) return;
        customizeSettings = data.settings;
        currentAppearance = data.appearance;
        console.log('[shop] customizeStart: isFreemode:', customizeSettings && customizeSettings.isFreemode);
        updateSubTabStates();
        renderCurrentSection();
    }).catch(function(err) {
        console.log('[shop] customizeStart ERROR:', err);
    });
}

function exitCustomization(cancelled) {
    if (!customizeActive) return;
    customizeActive = false;
    previewingModelHash = null;
    purchaseConfirmHash = null;
    var endpoint = cancelled ? 'customizeCancel' : 'customizeSave';
    fetch('https://gamemodecity/' + endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
    customizeSettings = null;
    currentAppearance = null;
}

function updateSubTabStates() {
    var isFreemode = customizeSettings && customizeSettings.isFreemode;
    var pedReady = customizeSettings != null;
    document.querySelectorAll('.shop-sub-tab').forEach(function(btn) {
        var section = btn.getAttribute('data-section');
        if (section === 'model') {
            btn.classList.remove('disabled');
        } else if (!pedReady) {
            // Disable all non-model tabs until preview ped is ready
            btn.classList.add('disabled');
        } else if ((section === 'face' || section === 'overlays' || section === 'eyes') && !isFreemode) {
            btn.classList.add('disabled');
        } else {
            btn.classList.remove('disabled');
        }
    });
}

function setCamera(preset) {
    fetch('https://gamemodecity/setCameraPreset', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ preset: preset })
    });
    document.querySelectorAll('.cam-btn').forEach(function(b) { b.classList.remove('active'); });
    var active = document.querySelector('.cam-btn[data-preset="' + preset + '"]');
    if (active) active.classList.add('active');
}

function autoCameraForSection(section) {
    if (section === 'face' || section === 'hair' || section === 'eyes' || section === 'overlays') {
        setCamera('head');
    } else if (section === 'clothing' || section === 'accessories') {
        setCamera('body');
    } else {
        setCamera('default');
    }
}

function renderCurrentSection() {
    var container = document.getElementById('shopSectionContent');
    container.innerHTML = '';
    switch (currentSection) {
        case 'model': renderModelSection(container); break;
        case 'face': renderFaceSection(container); break;
        case 'hair': renderHairSection(container); break;
        case 'overlays': renderOverlaysSection(container); break;
        case 'clothing': renderClothingSection(container); break;
        case 'accessories': renderAccessoriesSection(container); break;
        case 'eyes': renderEyesSection(container); break;
    }
}

// ---- Model Section ----
var previewingModelHash = null;
var purchaseConfirmHash = null;

function renderModelSection(container) {
    // Category filters
    var filters = document.createElement('div');
    filters.className = 'shop-filters';
    ['All','Freemode','Male','Female','Special'].forEach(function(cat) {
        var btn = document.createElement('button');
        btn.className = 'shop-filter-btn';
        if (shopCategory === cat) btn.classList.add('active');
        btn.textContent = cat;
        btn.addEventListener('click', function() {
            shopCategory = cat;
            purchaseConfirmHash = null;
            renderCurrentSection();
        });
        filters.appendChild(btn);
    });
    container.appendChild(filters);

    // Grid
    var grid = document.createElement('div');
    grid.className = 'shop-grid';

    pedModels.forEach(function(model) {
        if (shopCategory !== 'All' && model.category !== shopCategory) return;

        var isOwned = progression.unlockedModels.indexOf(model.hash) !== -1;
        var isSelected = progression.selectedModel === model.hash;
        var isPreviewing = previewingModelHash === model.hash;
        var canAfford = progression.tokens >= 500;

        var card = document.createElement('div');
        card.className = 'shop-card';
        if (isPreviewing) card.classList.add('previewing');
        if (isSelected) card.classList.add('selected');
        else if (isOwned) card.classList.add('owned');
        else card.classList.add('locked');

        var icon = document.createElement('div');
        icon.className = 'shop-card-icon';
        icon.textContent = model.category === 'Male' ? 'M' : model.category === 'Female' ? 'F' : 'S';
        card.appendChild(icon);

        var name = document.createElement('div');
        name.className = 'shop-card-name';
        name.textContent = model.name;
        card.appendChild(name);

        var cat = document.createElement('div');
        cat.className = 'shop-card-category';
        cat.textContent = model.category;
        card.appendChild(cat);

        // Click card to preview ANY model (owned or not)
        (function(hash) {
            card.addEventListener('click', function() {
                previewingModelHash = hash;
                purchaseConfirmHash = null;
                changePreviewModel(hash);
            });
        })(model.hash);

        // Action button
        var btn = document.createElement('button');
        btn.className = 'shop-card-btn';
        if (isSelected) {
            btn.classList.add('selected-btn');
            btn.textContent = 'Selected';
        } else if (isOwned) {
            btn.classList.add('select-btn');
            btn.textContent = 'Select';
            (function(hash) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    fetch('https://gamemodecity/selectModel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ modelHash: hash })
                    });
                    // Also update preview ped to this model
                    previewingModelHash = hash;
                    changePreviewModel(hash);
                });
            })(model.hash);
        } else if (purchaseConfirmHash === model.hash) {
            // Confirm purchase state
            btn.classList.add('confirm-btn');
            btn.textContent = 'Confirm - 500T';
            (function(hash) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    btn.textContent = 'Buying...';
                    btn.disabled = true;
                    purchaseConfirmHash = null;
                    fetch('https://gamemodecity/purchaseModel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ modelHash: hash })
                    });
                });
            })(model.hash);
        } else if (canAfford) {
            btn.classList.add('buy-btn');
            btn.textContent = '500T';
            (function(hash) {
                btn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    purchaseConfirmHash = hash;
                    renderCurrentSection();
                });
            })(model.hash);
        } else {
            btn.classList.add('cant-afford');
            btn.textContent = '500T';
        }
        card.appendChild(btn);
        grid.appendChild(card);
    });
    container.appendChild(grid);
}

// ---- Face Section ----
function renderFaceSection(container) {
    if (!customizeSettings || !customizeSettings.isFreemode) {
        container.innerHTML = '<div style="color:#5a5a7a;text-align:center;padding:40px">Face customization is only available for Freemode models</div>';
        return;
    }

    // Head Blend
    var title = document.createElement('div');
    title.className = 'cust-section-title';
    title.textContent = 'Genetics';
    container.appendChild(title);

    var blendRow = document.createElement('div');
    blendRow.className = 'cust-head-blend-row';

    var hb = (currentAppearance && currentAppearance.headBlend) || {};
    var fields = [
        { label: 'Mother', key: 'shapeFirst', max: 45 },
        { label: 'Father', key: 'shapeSecond', max: 45 },
        { label: 'Shape Mix', key: 'shapeMix', isFloat: true },
        { label: 'Skin Mother', key: 'skinFirst', max: 45 },
        { label: 'Skin Father', key: 'skinSecond', max: 45 },
        { label: 'Skin Mix', key: 'skinMix', isFloat: true }
    ];

    fields.forEach(function(f) {
        var col = document.createElement('div');
        col.className = 'cust-head-blend-col';
        var lbl = document.createElement('label');
        lbl.textContent = f.label;
        col.appendChild(lbl);

        if (f.isFloat) {
            var inp = document.createElement('input');
            inp.type = 'range';
            inp.className = 'cust-slider';
            inp.min = '0'; inp.max = '1'; inp.step = '0.05';
            inp.value = hb[f.key] || 0.5;
            inp.addEventListener('input', function() {
                hb[f.key] = parseFloat(inp.value);
                sendHeadBlend(hb);
            });
            col.appendChild(inp);
        } else {
            var inp = document.createElement('input');
            inp.type = 'number';
            inp.min = '0'; inp.max = String(f.max);
            inp.value = hb[f.key] || 0;
            inp.addEventListener('change', function() {
                hb[f.key] = parseInt(inp.value) || 0;
                sendHeadBlend(hb);
            });
            col.appendChild(inp);
        }
        blendRow.appendChild(col);
    });
    container.appendChild(blendRow);

    // Face Features (sliders)
    var title2 = document.createElement('div');
    title2.className = 'cust-section-title';
    title2.textContent = 'Face Features';
    container.appendChild(title2);

    var ff2 = (currentAppearance && currentAppearance.faceFeatures) || [];
    for (var i = 0; i < 20; i++) {
        (function(idx) {
            var row = document.createElement('div');
            row.className = 'cust-row';

            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = faceFeatureNames[idx] || ('Feature ' + idx);
            row.appendChild(lbl);

            var slider = document.createElement('input');
            slider.type = 'range';
            slider.className = 'cust-slider';
            slider.min = '-1'; slider.max = '1'; slider.step = '0.05';
            slider.value = ff2[idx] || 0;

            var valSpan = document.createElement('span');
            valSpan.className = 'cust-slider-value';
            valSpan.textContent = parseFloat(slider.value).toFixed(2);

            slider.addEventListener('input', function() {
                valSpan.textContent = parseFloat(slider.value).toFixed(2);
                if (!ff2[idx] && ff2.length <= idx) {
                    while (ff2.length <= idx) ff2.push(0);
                }
                ff2[idx] = parseFloat(slider.value);
                fetch('https://gamemodecity/changeFaceFeature', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ index: idx, value: parseFloat(slider.value) })
                });
            });

            row.appendChild(slider);
            row.appendChild(valSpan);
            container.appendChild(row);
        })(i);
    }
}

function sendHeadBlend(hb) {
    fetch('https://gamemodecity/changeHeadBlend', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            shapeFirst: hb.shapeFirst || 0,
            shapeSecond: hb.shapeSecond || 0,
            shapeMix: hb.shapeMix || 0.5,
            skinFirst: hb.skinFirst || 0,
            skinSecond: hb.skinSecond || 0,
            skinMix: hb.skinMix || 0.5
        })
    });
}

// ---- Hair Section ----
function renderHairSection(container) {
    var hair = (currentAppearance && currentAppearance.hair) || { style: 0, color: 0, highlight: 0 };
    var maxHair = customizeSettings ? customizeSettings.components[2].maxDrawable : 1;

    var title = document.createElement('div');
    title.className = 'cust-section-title';
    title.textContent = 'Hair Style';
    container.appendChild(title);

    // Style prev/next
    var row = document.createElement('div');
    row.className = 'cust-row';

    var lbl = document.createElement('span');
    lbl.className = 'cust-row-label';
    lbl.textContent = 'Style';
    row.appendChild(lbl);

    var pn = createPrevNext(hair.style, maxHair, function(val) {
        hair.style = val;
        sendHair(hair);
        checkHairUnlock(val);
    });
    row.appendChild(pn.el);

    // Unlock badge
    var badge = document.createElement('span');
    badge.className = 'cust-buy-badge';
    badge.id = 'hairUnlockBadge';
    updateHairBadge(badge, hair.style);
    badge.addEventListener('click', function() {
        var key = 'hair_' + hair.style;
        if (hair.style === 0 || progression.unlockedItems.indexOf(key) !== -1) return;
        fetch('https://gamemodecity/purchaseItem', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ itemKey: key })
        });
    });
    row.appendChild(badge);
    container.appendChild(row);

    // Color
    var title2 = document.createElement('div');
    title2.className = 'cust-section-title';
    title2.textContent = 'Hair Color';
    container.appendChild(title2);

    var colorGrid = document.createElement('div');
    colorGrid.className = 'cust-color-grid';
    for (var c = 0; c < 64; c++) {
        (function(idx) {
            var swatch = document.createElement('div');
            swatch.className = 'cust-color-swatch';
            swatch.style.background = hairColors[idx] || '#555';
            if (idx === hair.color) swatch.classList.add('active');
            swatch.addEventListener('click', function() {
                hair.color = idx;
                sendHair(hair);
                colorGrid.querySelectorAll('.cust-color-swatch').forEach(function(s) { s.classList.remove('active'); });
                swatch.classList.add('active');
            });
            colorGrid.appendChild(swatch);
        })(c);
    }
    container.appendChild(colorGrid);

    // Highlight
    var title3 = document.createElement('div');
    title3.className = 'cust-section-title';
    title3.textContent = 'Highlight Color';
    container.appendChild(title3);

    var hlGrid = document.createElement('div');
    hlGrid.className = 'cust-color-grid';
    for (var h = 0; h < 64; h++) {
        (function(idx) {
            var swatch = document.createElement('div');
            swatch.className = 'cust-color-swatch';
            swatch.style.background = hairColors[idx] || '#555';
            if (idx === hair.highlight) swatch.classList.add('active');
            swatch.addEventListener('click', function() {
                hair.highlight = idx;
                sendHair(hair);
                hlGrid.querySelectorAll('.cust-color-swatch').forEach(function(s) { s.classList.remove('active'); });
                swatch.classList.add('active');
            });
            hlGrid.appendChild(swatch);
        })(h);
    }
    container.appendChild(hlGrid);
}

function sendHair(hair) {
    fetch('https://gamemodecity/changeHair', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ style: hair.style, color: hair.color, highlight: hair.highlight })
    });
}

function checkHairUnlock(style) {
    var badge = document.getElementById('hairUnlockBadge');
    if (badge) updateHairBadge(badge, style);
}

function updateHairBadge(badge, style) {
    var key = 'hair_' + style;
    if (style === 0 || progression.unlockedItems.indexOf(key) !== -1) {
        badge.textContent = 'Owned';
        badge.classList.add('owned');
    } else {
        badge.textContent = '50T';
        badge.classList.remove('owned');
    }
}

// ---- Overlays Section ----
function renderOverlaysSection(container) {
    if (!customizeSettings || !customizeSettings.isFreemode) {
        container.innerHTML = '<div style="color:#5a5a7a;text-align:center;padding:40px">Features are only available for Freemode models</div>';
        return;
    }

    var overlays = (currentAppearance && currentAppearance.headOverlays) || [];
    var maxOverlays = customizeSettings.overlays || [];

    for (var i = 0; i < 12; i++) {
        (function(idx) {
            var maxVal = maxOverlays[idx] || 1;
            var ov = overlays[idx] || { index: 255, opacity: 1, colorType: 0, firstColor: 0, secondColor: 0 };

            var title = document.createElement('div');
            title.className = 'cust-section-title';
            title.textContent = overlayNames[idx];
            container.appendChild(title);

            // Style prev/next
            var row = document.createElement('div');
            row.className = 'cust-row';
            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = 'Style';
            row.appendChild(lbl);

            // -1 = none (255), then 0 to maxVal-1
            var displayVal = ov.index === 255 ? -1 : ov.index;
            var pn = createPrevNext(displayVal, maxVal, function(val) {
                ov.index = val < 0 ? 255 : val;
                sendOverlay(idx, ov);
            }, -1);
            row.appendChild(pn.el);
            container.appendChild(row);

            // Opacity slider
            var opRow = document.createElement('div');
            opRow.className = 'cust-row';
            var opLbl = document.createElement('span');
            opLbl.className = 'cust-row-label';
            opLbl.textContent = 'Opacity';
            opRow.appendChild(opLbl);

            var opSlider = document.createElement('input');
            opSlider.type = 'range';
            opSlider.className = 'cust-slider';
            opSlider.min = '0'; opSlider.max = '1'; opSlider.step = '0.05';
            opSlider.value = ov.opacity;

            var opVal = document.createElement('span');
            opVal.className = 'cust-slider-value';
            opVal.textContent = parseFloat(opSlider.value).toFixed(2);

            opSlider.addEventListener('input', function() {
                opVal.textContent = parseFloat(opSlider.value).toFixed(2);
                ov.opacity = parseFloat(opSlider.value);
                sendOverlay(idx, ov);
            });

            opRow.appendChild(opSlider);
            opRow.appendChild(opVal);
            container.appendChild(opRow);

            // Color (for overlays that support it: 1=facial hair, 2=eyebrows, 8=lipstick, 5=blush, 10=chest hair)
            var colorOverlays = [1, 2, 5, 8, 10];
            if (colorOverlays.indexOf(idx) !== -1) {
                var colorRow = document.createElement('div');
                colorRow.className = 'cust-row';
                var colorLbl = document.createElement('span');
                colorLbl.className = 'cust-row-label';
                colorLbl.textContent = 'Color';
                colorRow.appendChild(colorLbl);

                var colorPn = createPrevNext(ov.firstColor, 64, function(val) {
                    ov.firstColor = val;
                    ov.colorType = (idx === 5 || idx === 8) ? 2 : 1;
                    sendOverlay(idx, ov);
                });
                colorRow.appendChild(colorPn.el);
                container.appendChild(colorRow);
            }

            var spacer = document.createElement('div');
            spacer.className = 'cust-spacer';
            container.appendChild(spacer);
        })(i);
    }
}

function sendOverlay(overlayId, ov) {
    fetch('https://gamemodecity/changeHeadOverlay', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            overlayId: overlayId,
            index: ov.index,
            opacity: ov.opacity,
            colorType: ov.colorType || 0,
            firstColor: ov.firstColor || 0,
            secondColor: ov.secondColor || 0
        })
    });
}

// ---- Clothing Section ----
function renderClothingSection(container) {
    if (!customizeSettings) return;
    var comps = customizeSettings.components;
    var curComps = (currentAppearance && currentAppearance.components) || [];
    var isFreemode = customizeSettings.isFreemode;

    // Skip face(0) and hair(2) for freemode, show all for non-freemode
    for (var i = 0; i < 12; i++) {
        if (isFreemode && (i === 0 || i === 2)) continue;
        if (comps[i].maxDrawable <= 1) continue; // Skip slots with only 1 option

        (function(compId) {
            var maxDraw = comps[compId].maxDrawable;
            var cur = curComps[compId] || { drawable: 0, texture: 0 };
            var maxTex = comps[compId].maxTexture;

            var title = document.createElement('div');
            title.className = 'cust-section-title';
            title.textContent = componentNames[compId];
            container.appendChild(title);

            // Drawable prev/next
            var row = document.createElement('div');
            row.className = 'cust-row';
            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = 'Style';
            row.appendChild(lbl);

            var texRow, texPn;
            var pn = createPrevNext(cur.drawable, maxDraw, function(val) {
                cur.drawable = val;
                cur.texture = 0;
                fetch('https://gamemodecity/changeComponent', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ componentId: compId, drawable: val, texture: 0 })
                }).then(nuiResp).then(function(data) {
                    if (data.maxTexture !== undefined) {
                        maxTex = data.maxTexture;
                        if (texPn && texPn.updateMax) texPn.updateMax(maxTex);
                    }
                });
                updateCompBadge(compId, val);
            });
            row.appendChild(pn.el);

            // Unlock badge
            var badge = document.createElement('span');
            badge.className = 'cust-buy-badge';
            badge.id = 'compBadge_' + compId;
            updateCompBadgeEl(badge, compId, cur.drawable);
            badge.addEventListener('click', function() {
                var key = 'comp_' + compId + '_' + cur.drawable;
                if (cur.drawable === 0 || progression.unlockedItems.indexOf(key) !== -1) return;
                fetch('https://gamemodecity/purchaseItem', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ itemKey: key })
                });
            });
            row.appendChild(badge);
            container.appendChild(row);

            // Texture prev/next
            if (maxTex > 1) {
                texRow = document.createElement('div');
                texRow.className = 'cust-row';
                var texLbl = document.createElement('span');
                texLbl.className = 'cust-row-label';
                texLbl.textContent = 'Texture';
                texRow.appendChild(texLbl);

                texPn = createPrevNext(cur.texture, maxTex, function(val) {
                    cur.texture = val;
                    fetch('https://gamemodecity/changeComponent', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ componentId: compId, drawable: cur.drawable, texture: val })
                    });
                });
                texRow.appendChild(texPn.el);
                container.appendChild(texRow);
            }

            var spacer = document.createElement('div');
            spacer.className = 'cust-spacer';
            container.appendChild(spacer);
        })(i);
    }
}

function updateCompBadge(compId, drawable) {
    var badge = document.getElementById('compBadge_' + compId);
    if (badge) updateCompBadgeEl(badge, compId, drawable);
}

function updateCompBadgeEl(badge, compId, drawable) {
    var key = 'comp_' + compId + '_' + drawable;
    if (drawable === 0 || progression.unlockedItems.indexOf(key) !== -1) {
        badge.textContent = 'Owned';
        badge.classList.add('owned');
    } else {
        badge.textContent = '100T';
        badge.classList.remove('owned');
    }
}

// ---- Accessories Section ----
function renderAccessoriesSection(container) {
    if (!customizeSettings) return;
    var propsSettings = customizeSettings.props;
    var curProps = (currentAppearance && currentAppearance.props) || [];

    propsSettings.forEach(function(ps, idx) {
        var propId = ps.id;
        var maxDraw = ps.maxDrawable;
        if (maxDraw <= 0) return;

        // Find current prop value
        var curProp = null;
        for (var j = 0; j < curProps.length; j++) {
            if (curProps[j].id === propId) { curProp = curProps[j]; break; }
        }
        if (!curProp) curProp = { id: propId, drawable: -1, texture: 0 };

        (function(pId, cur, maxD) {
            var title = document.createElement('div');
            title.className = 'cust-section-title';
            title.textContent = propNames[pId] || ('Prop ' + pId);
            container.appendChild(title);

            var maxTex = ps.maxTexture;

            // Drawable prev/next (-1 = none)
            var row = document.createElement('div');
            row.className = 'cust-row';
            var lbl = document.createElement('span');
            lbl.className = 'cust-row-label';
            lbl.textContent = 'Style';
            row.appendChild(lbl);

            var texPn;
            var pn = createPrevNext(cur.drawable, maxD, function(val) {
                cur.drawable = val;
                cur.texture = 0;
                fetch('https://gamemodecity/changeProp', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ propId: pId, drawable: val, texture: 0 })
                }).then(nuiResp).then(function(data) {
                    if (data.maxTexture !== undefined) {
                        maxTex = data.maxTexture;
                        if (texPn && texPn.updateMax) texPn.updateMax(maxTex);
                    }
                });
                updatePropBadge(pId, val);
            }, -1);
            row.appendChild(pn.el);

            // Unlock badge
            var badge = document.createElement('span');
            badge.className = 'cust-buy-badge';
            badge.id = 'propBadge_' + pId;
            updatePropBadgeEl(badge, pId, cur.drawable);
            badge.addEventListener('click', function() {
                var key = 'prop_' + pId + '_' + cur.drawable;
                if (cur.drawable <= 0 || progression.unlockedItems.indexOf(key) !== -1) return;
                fetch('https://gamemodecity/purchaseItem', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ itemKey: key })
                });
            });
            row.appendChild(badge);
            container.appendChild(row);

            // Texture prev/next
            var texRow = document.createElement('div');
            texRow.className = 'cust-row';
            var texLbl = document.createElement('span');
            texLbl.className = 'cust-row-label';
            texLbl.textContent = 'Texture';
            texRow.appendChild(texLbl);

            texPn = createPrevNext(cur.texture, Math.max(maxTex, 1), function(val) {
                cur.texture = val;
                fetch('https://gamemodecity/changeProp', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ propId: pId, drawable: cur.drawable, texture: val })
                });
            });
            texRow.appendChild(texPn.el);
            container.appendChild(texRow);

            var spacer = document.createElement('div');
            spacer.className = 'cust-spacer';
            container.appendChild(spacer);
        })(propId, curProp, maxDraw);
    });
}

function updatePropBadge(propId, drawable) {
    var badge = document.getElementById('propBadge_' + propId);
    if (badge) updatePropBadgeEl(badge, propId, drawable);
}

function updatePropBadgeEl(badge, propId, drawable) {
    var key = 'prop_' + propId + '_' + drawable;
    if (drawable <= 0 || progression.unlockedItems.indexOf(key) !== -1) {
        badge.textContent = 'Owned';
        badge.classList.add('owned');
    } else {
        badge.textContent = '75T';
        badge.classList.remove('owned');
    }
}

// ---- Eyes Section ----
function renderEyesSection(container) {
    if (!customizeSettings || !customizeSettings.isFreemode) {
        container.innerHTML = '<div style="color:#5a5a7a;text-align:center;padding:40px">Eye color is only available for Freemode models</div>';
        return;
    }

    var curEye = (currentAppearance && currentAppearance.eyeColor) || 0;

    var title = document.createElement('div');
    title.className = 'cust-section-title';
    title.textContent = 'Eye Color';
    container.appendChild(title);

    var grid = document.createElement('div');
    grid.className = 'cust-eye-grid';

    for (var i = 0; i < eyeColorNames.length; i++) {
        (function(idx) {
            var btn = document.createElement('button');
            btn.className = 'cust-eye-btn';
            if (idx === curEye) btn.classList.add('active');
            btn.textContent = eyeColorNames[idx];
            btn.addEventListener('click', function() {
                curEye = idx;
                if (currentAppearance) currentAppearance.eyeColor = idx;
                grid.querySelectorAll('.cust-eye-btn').forEach(function(b) { b.classList.remove('active'); });
                btn.classList.add('active');
                fetch('https://gamemodecity/changeEyeColor', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ color: idx })
                });
            });
            grid.appendChild(btn);
        })(i);
    }
    container.appendChild(grid);
}

// ---- Prev/Next Helper ----
function createPrevNext(initial, max, onChange, minVal) {
    var val = initial;
    var mn = minVal !== undefined ? minVal : 0;
    var mx = max;

    var wrap = document.createElement('div');
    wrap.className = 'cust-prevnext';

    var prevBtn = document.createElement('button');
    prevBtn.className = 'cust-prevnext-btn';
    prevBtn.innerHTML = '&#9664;';

    var valSpan = document.createElement('span');
    valSpan.className = 'cust-prevnext-value';
    valSpan.textContent = val < mn ? 'None' : val + '/' + (mx - (mn < 0 ? 0 : 1));

    var nextBtn = document.createElement('button');
    nextBtn.className = 'cust-prevnext-btn';
    nextBtn.innerHTML = '&#9654;';

    function update() {
        valSpan.textContent = val < 0 ? 'None' : val + '/' + (mx - 1);
        if (onChange) onChange(val);
    }

    prevBtn.addEventListener('click', function() {
        val--;
        if (val < mn) val = mx - 1;
        update();
    });

    nextBtn.addEventListener('click', function() {
        val++;
        if (val >= mx) val = mn;
        update();
    });

    wrap.appendChild(prevBtn);
    wrap.appendChild(valSpan);
    wrap.appendChild(nextBtn);

    return {
        el: wrap,
        updateMax: function(newMax) {
            mx = newMax;
            if (val >= mx) { val = 0; update(); }
            valSpan.textContent = val < 0 ? 'None' : val + '/' + (mx - 1);
        }
    };
}

// Sub-tab clicks
document.getElementById('shopSubTabs').addEventListener('click', function(e) {
    var btn = e.target.closest('.shop-sub-tab');
    if (!btn || btn.classList.contains('disabled')) return;
    var section = btn.getAttribute('data-section');
    currentSection = section;
    document.querySelectorAll('.shop-sub-tab').forEach(function(b) { b.classList.remove('active'); });
    btn.classList.add('active');
    autoCameraForSection(section);
    renderCurrentSection();
});

// Camera buttons
document.getElementById('shopCameraBtns').addEventListener('click', function(e) {
    var btn = e.target.closest('.cam-btn');
    if (!btn) return;
    setCamera(btn.getAttribute('data-preset'));
});

// Save/Cancel buttons
document.getElementById('shopSaveBtn').addEventListener('click', function() {
    exitCustomization(false);
    switchTab('home');
});

document.getElementById('shopCancelBtn').addEventListener('click', function() {
    exitCustomization(true);
    switchTab('home');
});

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
    map.rotation = parseFloat(document.getElementById('mapRotation').value) || 0;

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
        body: JSON.stringify({ mapId: map.id, show: true, posX: map.posX, posY: map.posY, posZ: map.posZ, sizeX: map.sizeX, sizeY: map.sizeY, sizeZ: map.sizeZ, rotation: map.rotation || 0 })
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
    if (customizeActive) {
        exitCustomization(true);
    }
    document.getElementById('hub').classList.remove('visible');
    document.getElementById('hub').classList.remove('shop-preview-active');
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
                fetch('https://gamemodecity/castVote', {
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

    // Auto-close after 4s
    setTimeout(function() {
        closeVoteOverlay();
    }, 4000);
}

function closeVoteOverlay() {
    var overlay = document.getElementById('vote-overlay');
    overlay.classList.add('fade-out');
    clearInterval(voteTimerInterval);
    setTimeout(function() {
        overlay.classList.remove('active');
        overlay.classList.remove('fade-out');
        // Reset state
        voteGamemodes = [];
        voteSelectedId = null;
        voteVoters = {};
        voteIsOpen = false;
    }, 400);
}

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
        updateStatsBar();
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
});
