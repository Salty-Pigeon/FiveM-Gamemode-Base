fx_version 'cerulean'
game 'gta5'

name 'gta_gameroo'
description 'GameRoo - Multi-gamemode FiveM resource'

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
    'GTA_GameRooClient.net.dll',
    'GTA_GameRooShared.net.dll',
    'TTTClient.net.dll',
    'TDMClient.net.dll',
    'ICMClient.net.dll',
    'MVBClient.net.dll',
    'HPClient.net.dll',
}

server_scripts {
    'GTA_GameRooServer.net.dll',
    'GTA_GameRooShared.net.dll',
    'TTTServer.net.dll',
    'TDMServer.net.dll',
    'ICMServer.net.dll',
    'MVBServer.net.dll',
    'HPServer.net.dll',
}
