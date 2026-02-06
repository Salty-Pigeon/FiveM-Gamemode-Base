@echo off
setlocal enabledelayedexpansion

set "PROJECT=C:\Users\jason\Documents\Projects\FiveM-Gamemode-Base"
set "SERVER=C:\FiveM\txData\FiveMBasicServerCFXDefault_857960.base\resources"

echo ============================================
echo  GamemodeCity Deploy Script
echo ============================================
echo.

:: ---- gamemodecity (base) ----
echo [1/5] Deploying gamemodecity...
set "DEST=%SERVER%\[gamemodes]\gamemodecity"
xcopy /Y /Q "%PROJECT%\GamemodeCityClient\bin\Debug\GamemodeCityClient.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityServer\bin\Debug\GamemodeCityServer.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityShared\bin\Debug\GamemodeCityShared.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MenuAPI.dll"                                          "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MySql.Data.dll"                                       "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\BouncyCastle.Crypto.dll"                              "%DEST%\"

:: ---- TDM ----
echo [2/5] Deploying TDM...
set "DEST=%SERVER%\[gamemodes]\tdm"
xcopy /Y /Q "%PROJECT%\TDMClient\bin\Debug\TDMClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\TDMServer\bin\Debug\TDMServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityClient\bin\Debug\GamemodeCityClient.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityServer\bin\Debug\GamemodeCityServer.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityShared\bin\Debug\GamemodeCityShared.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MenuAPI.dll"                                          "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MySql.Data.dll"                                       "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\BouncyCastle.Crypto.dll"                              "%DEST%\"

:: ---- TTT ----
echo [3/5] Deploying TTT...
set "DEST=%SERVER%\[gamemodes]\ttt"
xcopy /Y /Q "%PROJECT%\TTTClient\bin\Debug\TTTClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\TTTServer\bin\Debug\TTTServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityClient\bin\Debug\GamemodeCityClient.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityServer\bin\Debug\GamemodeCityServer.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityShared\bin\Debug\GamemodeCityShared.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MenuAPI.dll"                                          "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MySql.Data.dll"                                       "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\BouncyCastle.Crypto.dll"                              "%DEST%\"

:: ---- ICM ----
echo [4/5] Deploying ICM...
set "DEST=%SERVER%\[gamemodes]\icm"
xcopy /Y /Q "%PROJECT%\ICMClient\bin\Debug\ICMClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\ICMServer\bin\Debug\ICMServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityClient\bin\Debug\GamemodeCityClient.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityServer\bin\Debug\GamemodeCityServer.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityShared\bin\Debug\GamemodeCityShared.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MenuAPI.dll"                                          "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MySql.Data.dll"                                       "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\BouncyCastle.Crypto.dll"                              "%DEST%\"

:: ---- MVB ----
echo [5/5] Deploying MVB...
set "DEST=%SERVER%\[gamemodes]\mvb"
xcopy /Y /Q "%PROJECT%\MVBClient\bin\Debug\MVBClient.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\MVBServer\bin\Debug\MVBServer.net.dll"                   "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityClient\bin\Debug\GamemodeCityClient.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityServer\bin\Debug\GamemodeCityServer.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\GamemodeCityShared\bin\Debug\GamemodeCityShared.net.dll"  "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MenuAPI.dll"                                          "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\MySql.Data.dll"                                       "%DEST%\"
xcopy /Y /Q "%PROJECT%\lib\BouncyCastle.Crypto.dll"                              "%DEST%\"

echo.
echo ============================================
echo  Deploy complete! Restart server to reload.
echo ============================================
echo.
pause
