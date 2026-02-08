fx_version 'cerulean'
game 'gta5'

name 'gamemodecity'
description 'GamemodeCity - Multi-gamemode FiveM resource'

ui_page 'html/hub.html'

files {
    'html/hub.html',
    'html/hub-base.css', 'html/hub-home.css', 'html/hub-controls.css',
    'html/hub-debug.css', 'html/hub-shop.css', 'html/hub-maps.css',
    'html/hub-admin.css', 'html/hub-overlays.css',
    'html/hub-helpers.js', 'html/hub-core.js', 'html/hub-home.js',
    'html/hub-controls.js', 'html/hub-debug.js', 'html/hub-shop.js',
    'html/hub-maps.js', 'html/hub-admin.js', 'html/hub-overlays.js',
    'html/hub-messages.js',
    'MenuAPI.dll',
}

client_scripts {
    'GamemodeCityClient.net.dll',
    'GamemodeCityShared.net.dll',
    'TTTClient.net.dll',
    'TDMClient.net.dll',
    'ICMClient.net.dll',
    'MVBClient.net.dll',
    'HPClient.net.dll',
}

server_scripts {
    'GamemodeCityServer.net.dll',
    'GamemodeCityShared.net.dll',
    'TTTServer.net.dll',
    'TDMServer.net.dll',
    'ICMServer.net.dll',
    'MVBServer.net.dll',
    'HPServer.net.dll',
}
