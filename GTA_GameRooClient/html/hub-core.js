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
let progression = { xp: 0, level: 1, tokens: 0, unlockedModels: [], selectedModel: '', unlockedItems: [], adminLevel: 0 };
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

// Tab visibility based on admin level
function updateTabVisibility() {
    var debugBtn = document.querySelector('.nav-btn[data-tab="debug"]');
    var mapsBtn = document.querySelector('.nav-btn[data-tab="maps"]');
    var adminBtn = document.querySelector('.nav-btn[data-tab="admin"]');
    if (debugBtn) { if (progression.adminLevel >= 1) debugBtn.classList.remove('nav-hidden'); else debugBtn.classList.add('nav-hidden'); }
    if (mapsBtn) { if (progression.adminLevel >= 2) mapsBtn.classList.remove('nav-hidden'); else mapsBtn.classList.add('nav-hidden'); }
    if (adminBtn) { if (progression.adminLevel >= 3) adminBtn.classList.remove('nav-hidden'); else adminBtn.classList.add('nav-hidden'); }
    // If current tab is now hidden, switch to home
    if ((currentTab === 'debug' && progression.adminLevel < 1) ||
        (currentTab === 'maps' && progression.adminLevel < 2) ||
        (currentTab === 'admin' && progression.adminLevel < 3)) {
        switchTab('home');
    }
}

// Vote tab management
function showVoteTab() {
    var voteBtn = document.querySelector('.nav-btn[data-tab="vote"]');
    if (voteBtn) voteBtn.classList.remove('nav-hidden');
    switchTab('vote');
}

function hideVoteTab() {
    var voteBtn = document.querySelector('.nav-btn[data-tab="vote"]');
    if (voteBtn) voteBtn.classList.add('nav-hidden');
    if (currentTab === 'vote') {
        switchTab('home');
    }
}

// Tab switching
function switchTab(name) {
    // Guard: prevent switching to restricted tabs
    if (name === 'debug' && progression.adminLevel < 1) return;
    if (name === 'maps' && progression.adminLevel < 2) return;
    if (name === 'admin' && progression.adminLevel < 3) return;

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
        fetch('https://gta_gameroo/mapsTabClosed', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });
    }

    // Request online players when entering admin tab
    if (name === 'admin') {
        fetch('https://gta_gameroo/getOnlinePlayers', {
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
        fetch('https://gta_gameroo/requestMaps', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });
    }
}

function closeHub() {
    stopListening();
    if (customizeActive) {
        exitCustomization(true);
    }
    document.getElementById('hub').classList.remove('visible');
    document.getElementById('hub').classList.remove('shop-preview-active');
    fetch('https://gta_gameroo/closeHub', {
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

document.getElementById('welcomeStartBtn').addEventListener('click', function() {
    document.getElementById('welcomeSplash').classList.remove('active');
});

document.getElementById('resetBtn').addEventListener('click', function() {
    fetch('https://gta_gameroo/resetDefaults', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
    });
});
