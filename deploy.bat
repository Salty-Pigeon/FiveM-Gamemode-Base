@echo off
setlocal enabledelayedexpansion

set "PROJECT=C:\Users\jason\Documents\Projects\FiveM-Gamemode-Base"
set "SERVER=C:\FiveM\txData\FiveMBasicServerCFXDefault_857960.base\resources"

echo ============================================
echo  GameRoo Deploy Script
echo ============================================
echo.

:: ---- gta_gameroo (all gamemodes consolidated) ----
echo [1/1] Deploying gta_gameroo (all gamemodes)...
set "DEST=%SERVER%\[gamemodes]\gta_gameroo"
xcopy /Y /Q "%PROJECT%\GTA_GameRooClient\bin\Debug\GTA_GameRooClient.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GTA_GameRooServer\bin\Debug\GTA_GameRooServer.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GTA_GameRooShared\bin\Debug\GTA_GameRooShared.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MenuAPI.dll"                                          "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MySql.Data.dll"                                       "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\BouncyCastle.Crypto.dll"                              "%DEST%\"
xcopy /Y /Q "%PROJECT%\TDMClient\bin\Debug\TDMClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\TDMServer\bin\Debug\TDMServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\TTTClient\bin\Debug\TTTClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\TTTServer\bin\Debug\TTTServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\ICMClient\bin\Debug\ICMClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\ICMServer\bin\Debug\ICMServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\MVBClient\bin\Debug\MVBClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\MVBServer\bin\Debug\MVBServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\HPClient\bin\Debug\HPClient.net.dll"                    "%DEST%\"
xcopy /Y /Q "%PROJECT%\HPServer\bin\Debug\HPServer.net.dll"                    "%DEST%\"
xcopy /Y /Q "%PROJECT%\GGClient\bin\Debug\GGClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\GGServer\bin\Debug\GGServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\fxmanifest.lua"                                          "%DEST%\"
if not exist "%DEST%\html" mkdir "%DEST%\html"
xcopy /Y /Q "%PROJECT%\GTA_GameRooClient\html\*.*"              "%DEST%\html\"

echo.
echo ============================================
echo  Deploy complete! Restart server to reload.
echo ============================================
echo.
pause
