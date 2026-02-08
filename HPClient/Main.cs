using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooClient;
using GTA_GameRooShared;

namespace HPClient
{

    public enum Teams {
        Safe,
        It
    }

    public class HPBot {
        public int PedHandle;
        public int VehicleHandle;
        public int FakeServerId;
        public int Team;
    }

    public class Main : BaseGamemode
    {
        Vehicle Car;
        float potatoEndTime = 0;
        float passCooldown = 0;
        bool pendingPotatoStart = false;
        int itPlayerServerId = -1;
        Random rand;

        // Bot potato tracking
        int botItIndex = -1;
        float botPotatoEndTime = 0;

        public static List<HPBot> Bots = new List<HPBot>();
        static int nextBotId = 9000;

        public Main() : base( "HP" ) {
            HUD = new HUD();
            rand = new Random( GetGameTimer() );
            EventHandlers["salty::HPRoundResult"] += new Action<string, string, string>( OnRoundResult );

            var gmInfo = GamemodeRegistry.Register( "hp", "Hot Potato",
                "One player holds the explosive potato and must crash into others to pass it before the timer runs out.", "#ff4444" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 16;
            gmInfo.Tags = new string[] { "Vehicle", "Elimination" };
            gmInfo.Teams = new string[] { "Safe", "It" };
            gmInfo.Features = new string[] { "Vehicles", "Timer", "Last Man Standing" };
            gmInfo.Guide = new GuideSection {
                Overview = "Vehicular hot potato. Everyone drives muscle cars. One player holds an explosive potato and must crash into others to pass it before the 30-second timer detonates.",
                HowToWin = "Last player standing wins. If you hold the potato when the timer hits zero, you explode.",
                Rules = new string[] {
                    "Crash into another player to pass the potato.",
                    "2-second cooldown between passes.",
                    "Eliminated players become spectators."
                },
                TeamRoles = new GuideTeamRole[] {
                    new GuideTeamRole {
                        Name = "It", Color = "#e94560",
                        Goal = "You have the potato! Ram into a Safe player within 30 seconds or you explode.",
                        Tips = new string[] { "Green markers show targets.", "Aim for corners where players can't dodge.", "Use boost to close the gap." }
                    },
                    new GuideTeamRole {
                        Name = "Safe", Color = "#4ade80",
                        Goal = "Avoid the potato holder!",
                        Tips = new string[] { "Red marker shows who has it.", "Stay mobile and dodge.", "Don't drive in straight lines." }
                    }
                },
                Tips = new string[] {
                    "Don't drive in straight lines.",
                    "When you're It, aim for corners where players can't dodge.",
                    "Watch the timer \u2014 30 seconds goes fast."
                }
            };

            RegisterCommand( "spawnbot", new Action<int, List<object>, string>( async ( source, args, raw ) => {
                await SpawnBot();
            } ), false );

            RegisterCommand( "clearbots", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ClearBots();
            } ), false );

            DebugRegistry.RegisterEntityProvider( "hp", () => BuildEntityListJson() );

            DebugRegistry.Register( "hp", "hp_set_team_safe", "Set Team: Safe", "Teams", () => {
                SetTeam( (int)Teams.Safe );
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", (int)Teams.Safe );
                PlayerSpawn();
                WriteChat( "Debug", "You are now Safe", 30, 200, 30 );
                SendDebugEntityUpdate();
            } );
            DebugRegistry.Register( "hp", "hp_set_team_it", "Set Team: It", "Teams", () => {
                SetTeam( (int)Teams.It );
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", (int)Teams.It );
                PlayerSpawn();
                WriteChat( "Debug", "You are now It", 30, 200, 30 );
                SendDebugEntityUpdate();
            } );
            DebugRegistry.Register( "hp", "hp_respawn", "Respawn", "Self", () => {
                PlayerSpawn();
                WriteChat( "Debug", "Respawned", 30, 200, 30 );
            } );
            DebugRegistry.Register( "hp", "hp_spawn_bot", "Spawn Bot", "Bots", async () => {
                await SpawnBot();
                SendDebugEntityUpdate();
            } );
            DebugRegistry.Register( "hp", "hp_clear_bots", "Clear All Bots", "Bots", () => {
                ClearBots();
                SendDebugEntityUpdate();
            } );
        }

        public override async void Start( float gameTime ) {
            base.Start( gameTime );

            await RunCountdown();

            if( pendingPotatoStart ) {
                potatoEndTime = GetGameTimer() + 30000;
                pendingPotatoStart = false;
            }
        }

        public override void Update() {
            CantExitVehichles();

            // Force player back into car if ejected
            if( !LocalPlayer.Character.IsInVehicle() && Car != null ) {
                Game.PlayerPed.SetIntoVehicle( Car, VehicleSeat.Driver );
            }

            // Start timer after countdown ends
            if( pendingPotatoStart && !CountdownActive ) {
                potatoEndTime = GetGameTimer() + 30000;
                pendingPotatoStart = false;
            }

            // Collision detection - only if we're "it" and cooldown expired
            if( Team == (int)Teams.It && passCooldown < GetGameTimer() && Car != null ) {
                if( Car.HasCollided ) {
                    string mat = Car.MaterialCollidingWith.ToString();
                    if( mat == "CarMetal" || mat == "Unk" ) {
                        bool passed = false;

                        // Check real players first
                        Player closest = null;
                        float closestDist = 8f;
                        foreach( var ply in new PlayerList() ) {
                            if( ply.ServerId == LocalPlayer.ServerId ) continue;
                            object teamObj = GetPlayerDetail( ply.ServerId, "team" );
                            if( teamObj == null || Convert.ToInt32( teamObj ) != (int)Teams.Safe ) continue;
                            float dist = World.GetDistance( Car.Position, ply.Character.Position );
                            if( dist < closestDist ) {
                                closestDist = dist;
                                closest = ply;
                            }
                        }
                        if( closest != null ) {
                            TriggerServerEvent( "salty:netHPPass", closest.ServerId );
                            passCooldown = GetGameTimer() + 2000;
                            passed = true;
                        }

                        // Check bots if no player was hit
                        if( !passed ) {
                            int closestBotIdx = -1;
                            float closestBotDist = 8f;
                            for( int i = 0; i < Bots.Count; i++ ) {
                                var bot = Bots[i];
                                if( bot.Team != (int)Teams.Safe ) continue;
                                if( !DoesEntityExist( bot.PedHandle ) ) continue;
                                Vector3 botPos = GetEntityCoords( bot.PedHandle, true );
                                float dist = World.GetDistance( Car.Position, botPos );
                                if( dist < closestBotDist ) {
                                    closestBotDist = dist;
                                    closestBotIdx = i;
                                }
                            }
                            if( closestBotIdx >= 0 ) {
                                PassPotatoToBot( closestBotIdx );
                                passCooldown = GetGameTimer() + 2000;
                            }
                        }
                    }
                }
            }

            // Timer check - if we're "it" and timer expired, explode
            if( Team == (int)Teams.It && potatoEndTime > 0 && GetGameTimer() > potatoEndTime ) {
                ExplodePotato();
            }

            // Bot potato timer - bot holding potato explodes when timer runs out
            if( botItIndex >= 0 && botPotatoEndTime > 0 && GetGameTimer() > botPotatoEndTime ) {
                ExplodeBotPotato();
            }

            DrawMarkers();
            DrawPotatoTimer();

            base.Update();
        }

        public override void SetTeam( int team ) {
            base.SetTeam( team );
            switch( team ) {
                case (int)Teams.It:
                    HubNUI.ShowRoleReveal( "HOT POTATO!", "#ff4444" );
                    HUD.SetGoal( "CRASH into someone!", 255, 50, 50, 255, 10 );
                    if( CountdownActive ) {
                        pendingPotatoStart = true;
                    } else {
                        potatoEndTime = GetGameTimer() + 30000;
                    }
                    break;
                case (int)Teams.Safe:
                    HubNUI.ShowRoleReveal( "Safe", "#22c55e" );
                    HUD.SetGoal( "Avoid the hot potato!", 34, 197, 94, 255, 10 );
                    potatoEndTime = 0;
                    break;
            }
        }

        public override void OnDetailUpdate( int ply, string key, object oldValue, object newValue ) {
            if( key == "team" && Convert.ToInt32( newValue ) == (int)Teams.It && ply != LocalPlayer.ServerId ) {
                itPlayerServerId = ply;
                potatoEndTime = GetGameTimer() + 30000;
            }
        }

        private void DrawMarkers() {
            if( Team == SPECTATOR ) return;

            foreach( var ply in new PlayerList() ) {
                if( ply.ServerId == LocalPlayer.ServerId ) continue;
                object teamObj = GetPlayerDetail( ply.ServerId, "team" );
                if( teamObj == null ) continue;
                int plyTeam = Convert.ToInt32( teamObj );

                if( Team == (int)Teams.It && plyTeam == (int)Teams.Safe ) {
                    // I'm "it" - draw green chevrons above safe players (targets)
                    DrawMarker( 2, ply.Character.Position.X, ply.Character.Position.Y, ply.Character.Position.Z + 2.5f,
                        0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 2.0f, 2.0f, 2.0f,
                        50, 200, 50, 120, false, true, 2, false, null, null, false );
                }
                else if( Team == (int)Teams.Safe && plyTeam == (int)Teams.It ) {
                    // I'm safe - draw red chevron above the "it" player (danger)
                    DrawMarker( 2, ply.Character.Position.X, ply.Character.Position.Y, ply.Character.Position.Z + 2.5f,
                        0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 2.0f, 2.0f, 2.0f,
                        255, 50, 50, 150, false, true, 2, false, null, null, false );
                }
            }

            // Draw markers above bots
            foreach( var bot in Bots ) {
                if( !DoesEntityExist( bot.PedHandle ) ) continue;
                Vector3 botPos = GetEntityCoords( bot.PedHandle, true );

                if( Team == (int)Teams.It && bot.Team == (int)Teams.Safe ) {
                    DrawMarker( 2, botPos.X, botPos.Y, botPos.Z + 2.5f,
                        0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 2.0f, 2.0f, 2.0f,
                        50, 200, 50, 120, false, true, 2, false, null, null, false );
                }
                else if( Team == (int)Teams.Safe && bot.Team == (int)Teams.It ) {
                    DrawMarker( 2, botPos.X, botPos.Y, botPos.Z + 2.5f,
                        0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 2.0f, 2.0f, 2.0f,
                        255, 50, 50, 150, false, true, 2, false, null, null, false );
                }
            }
        }

        private void DrawPotatoTimer() {
            // Show bot potato timer when a bot is "it"
            float timerEnd = potatoEndTime;
            if( botItIndex >= 0 && botPotatoEndTime > 0 ) {
                timerEnd = botPotatoEndTime;
            }
            if( timerEnd <= 0 ) return;

            float remaining = ( timerEnd - GetGameTimer() ) / 1000f;
            if( remaining < 0 ) remaining = 0;

            if( Team == (int)Teams.It ) {
                // Large pulsing countdown for "it" player
                int r = 255, g = 255, b = 255;
                float scale = 1.0f;
                if( remaining < 5f ) {
                    r = 255; g = 50; b = 50;
                    // Pulse effect
                    scale = 1.0f + (float)Math.Sin( GetGameTimer() / 100.0 ) * 0.15f;
                }
                SetTextFont( 7 );
                SetTextScale( 0.0f, scale );
                SetTextColour( r, g, b, 255 );
                SetTextCentre( true );
                SetTextOutline();
                SetTextEntry( "STRING" );
                AddTextComponentString( Math.Ceiling( remaining ).ToString() );
                DrawText( 0.5f, 0.05f );
            }
            else if( Team == (int)Teams.Safe ) {
                // Smaller timer for safe players
                SetTextFont( 4 );
                SetTextScale( 0.0f, 0.5f );
                SetTextColour( 255, 200, 50, 255 );
                SetTextCentre( true );
                SetTextOutline();
                SetTextEntry( "STRING" );
                AddTextComponentString( "POTATO: " + Math.Ceiling( remaining ) + "s" );
                DrawText( 0.5f, 0.05f );
            }
        }

        private void ExplodePotato() {
            potatoEndTime = 0;
            if( Car != null ) {
                Car.IsInvincible = false;
                Car.IsExplosionProof = false;
                Car.ExplodeNetworked();
            }
            LocalPlayer.Character.Kill();
        }

        private void PassPotatoToBot( int botIdx ) {
            // Player passes potato to bot - player becomes safe, bot becomes "it"
            Bots[botIdx].Team = (int)Teams.It;
            botItIndex = botIdx;
            botPotatoEndTime = GetGameTimer() + 30000;

            // Make player safe client-side
            potatoEndTime = 0;
            SetTeam( (int)Teams.Safe );
            TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", (int)Teams.Safe );

            WriteChat( "Hot Potato", "You passed the potato to a bot!", 255, 200, 50 );
        }

        private void ExplodeBotPotato() {
            if( botItIndex < 0 || botItIndex >= Bots.Count ) return;
            var bot = Bots[botItIndex];

            // Explode the bot's vehicle
            if( DoesEntityExist( bot.VehicleHandle ) ) {
                SetEntityAsNoLongerNeeded( ref bot.VehicleHandle );
                AddExplosion( GetEntityCoords( bot.VehicleHandle, true ).X, GetEntityCoords( bot.VehicleHandle, true ).Y, GetEntityCoords( bot.VehicleHandle, true ).Z, 7, 10f, true, false, 1f );
                int veh = bot.VehicleHandle;
                DeleteVehicle( ref veh );
            }
            if( DoesEntityExist( bot.PedHandle ) ) {
                int ped = bot.PedHandle;
                SetEntityAsMissionEntity( ped, false, true );
                DeletePed( ref ped );
            }

            WriteChat( "Hot Potato", "Bot exploded! Potato is back!", 255, 68, 68 );

            // Remove the dead bot
            Bots.RemoveAt( botItIndex );
            botItIndex = -1;
            botPotatoEndTime = 0;

            // Pick new target: if bots remain, player gets potato back
            SetTeam( (int)Teams.It );
            TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", (int)Teams.It );
            potatoEndTime = GetGameTimer() + 30000;
        }

        public async Task SpawnCar() {
            if( Car != null )
                Car.Delete();
            Game.PlayerPed.Position = ClientGlobals.LastSpawn;
            Car = await World.CreateVehicle( "dominator", Game.PlayerPed.Position, ClientGlobals.LastSpawnHeading );
            if( Car != null ) {
                Car.CanBeVisiblyDamaged = false;
                Car.CanEngineDegrade = false;
                Car.CanTiresBurst = false;
                Car.CanWheelsBreak = false;
                Car.IsInvincible = true;
                Car.IsFireProof = true;
                Game.PlayerPed.SetIntoVehicle( Car, VehicleSeat.Driver );
            }
            SetGameplayCamRelativeHeading( 0 );
        }

        public override void PlayerSpawn() {
            base.PlayerSpawn();
            SpawnCar();
        }

        public override void End() {
            ClearBots();
            if( Car != null )
                Car.Delete();
            base.End();
        }

        public void OnRoundResult( string winner, string color, string reason ) {
            HubNUI.ShowRoundEnd( winner, color, reason );
        }

        private static string EscapeJsonString( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
        }

        public string BuildEntityListJson() {
            var entries = new List<string>();

            string playerTeam = Team == (int)Teams.It ? "It" : "Safe";
            entries.Add( "{\"id\":" + LocalPlayer.ServerId + ",\"name\":\"" + EscapeJsonString( LocalPlayer.Name ) + " (You)\",\"type\":\"player\",\"team\":\"" + EscapeJsonString( playerTeam ) + "\"}" );

            foreach( var player in Players ) {
                if( player.ServerId == LocalPlayer.ServerId ) continue;
                entries.Add( "{\"id\":" + player.ServerId + ",\"name\":\"" + EscapeJsonString( player.Name ) + "\",\"type\":\"player\",\"team\":\"\"}" );
            }

            foreach( var bot in Bots ) {
                string botTeam = bot.Team == (int)Teams.It ? "It" : "Safe";
                entries.Add( "{\"id\":" + bot.FakeServerId + ",\"name\":\"Bot_" + bot.FakeServerId + "\",\"type\":\"bot\",\"team\":\"" + EscapeJsonString( botTeam ) + "\"}" );
            }

            return "[" + string.Join( ",", entries ) + "]";
        }

        public void SendDebugEntityUpdate() {
            HubNUI.SendDebugEntityUpdate( "hp" );
        }

        public async Task SpawnBot() {
            Vector3 spawnPos = ClientGlobals.LastSpawn;

            foreach( var kvp in ClientGlobals.Maps ) {
                var mapSpawns = kvp.Value.Spawns.Where( s => s.SpawnType == SpawnType.PLAYER ).ToList();
                if( mapSpawns.Count > 0 ) {
                    spawnPos = mapSpawns[rand.Next( mapSpawns.Count )].Position;
                    break;
                }
            }

            // Load collision at spawn point
            RequestCollisionAtCoord( spawnPos.X, spawnPos.Y, spawnPos.Z );
            NewLoadSceneStart( spawnPos.X, spawnPos.Y, spawnPos.Z, spawnPos.X, spawnPos.Y, spawnPos.Z, 50f, 0 );

            string[] pedModels = { "a_m_y_hipster_01", "a_f_y_hipster_02", "a_m_y_skater_01", "a_f_y_skater_01", "a_m_y_runner_01" };
            string pedModelName = pedModels[rand.Next( pedModels.Length )];

            uint pedHash = (uint)GetHashKey( pedModelName );
            uint vehHash = (uint)GetHashKey( "dominator" );
            RequestModel( pedHash );
            RequestModel( vehHash );

            int timeout = 0;
            while( (!HasModelLoaded( pedHash ) || !HasModelLoaded( vehHash )) && timeout < 100 ) {
                await Delay( 50 );
                timeout++;
            }
            NewLoadSceneStop();

            if( !HasModelLoaded( pedHash ) || !HasModelLoaded( vehHash ) ) {
                WriteChat( "HP", "Failed to load bot models (ped:" + HasModelLoaded( pedHash ) + " veh:" + HasModelLoaded( vehHash ) + ")", 200, 30, 30 );
                return;
            }

            float groundZ = spawnPos.Z;
            float outZ = 0f;
            if( GetGroundZFor_3dCoord( spawnPos.X, spawnPos.Y, spawnPos.Z + 5f, ref outZ, false ) ) {
                groundZ = outZ + 0.5f;
            }

            int vehicle = CreateVehicle( vehHash, spawnPos.X, spawnPos.Y, groundZ, 0f, false, false );
            if( vehicle == 0 ) {
                WriteChat( "HP", "Failed to create bot vehicle", 200, 30, 30 );
                return;
            }
            SetEntityAsMissionEntity( vehicle, true, true );
            SetVehicleOnGroundProperly( vehicle );

            await Delay( 100 );

            int ped = CreatePed( 4, pedHash, spawnPos.X, spawnPos.Y, groundZ, 0f, false, false );
            if( ped == 0 ) {
                WriteChat( "HP", "Failed to create bot ped", 200, 30, 30 );
                DeleteVehicle( ref vehicle );
                return;
            }
            SetEntityAsMissionEntity( ped, true, true );
            SetPedIntoVehicle( ped, vehicle, -1 );
            SetBlockingOfNonTemporaryEvents( ped, true );
            SetPedFleeAttributes( ped, 0, false );

            SetModelAsNoLongerNeeded( pedHash );
            SetModelAsNoLongerNeeded( vehHash );

            TaskVehicleDriveWander( ped, vehicle, 30f, 786468 );

            int botId = nextBotId++;
            Bots.Add( new HPBot { PedHandle = ped, VehicleHandle = vehicle, FakeServerId = botId, Team = (int)Teams.Safe } );
            WriteChat( "HP", "Spawned bot at " + spawnPos.X.ToString("F1") + ", " + spawnPos.Y.ToString("F1") + ", " + groundZ.ToString("F1"), 30, 200, 30 );
        }

        public void ClearBots() {
            foreach( var bot in Bots ) {
                if( DoesEntityExist( bot.PedHandle ) ) {
                    int ped = bot.PedHandle;
                    SetEntityAsMissionEntity( ped, false, true );
                    DeletePed( ref ped );
                }
                if( DoesEntityExist( bot.VehicleHandle ) ) {
                    int veh = bot.VehicleHandle;
                    SetEntityAsMissionEntity( veh, false, true );
                    DeleteVehicle( ref veh );
                }
            }
            Bots.Clear();
            WriteChat( "HP", "All bots cleared.", 200, 200, 30 );
        }
    }
}
