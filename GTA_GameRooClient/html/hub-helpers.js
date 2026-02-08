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

// FiveM cb(string) double-JSON-encodes: r.json() returns a string, not an object.
// This helper parses the inner JSON if needed.
function nuiResp(r) {
    return r.json().then(function(d) {
        if (typeof d === 'string') { try { return JSON.parse(d); } catch(e) { return d; } }
        return d;
    });
}

function round2(v) {
    return Math.round((v || 0) * 100) / 100;
}

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
