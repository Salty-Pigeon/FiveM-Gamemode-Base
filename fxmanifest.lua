fx_version 'cerulean'
game 'gta5'

name 'gamemodecity'
description 'GamemodeCity - Multi-gamemode FiveM resource'

ui_page 'html/hub.html'

files {
    'html/hub.html',
    'html/hub.css',
    'html/hub.js',
    'MenuAPI.dll',
}

client_scripts {
    'GamemodeCityClient.net.dll',
    'GamemodeCityShared.net.dll',
    'TTTClient.net.dll',
    'TDMClient.net.dll',
    'ICMClient.net.dll',
    'MVBClient.net.dll',
}

server_scripts {
    'GamemodeCityServer.net.dll',
    'GamemodeCityShared.net.dll',
    'TTTServer.net.dll',
    'TDMServer.net.dll',
    'ICMServer.net.dll',
    'MVBServer.net.dll',
}
