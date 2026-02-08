using GamemodeCityClient;
using GamemodeCityShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMClient
{
    public class ICMBot {
        public int PedHandle;
        public int VehicleHandle;
        public int FakeServerId;
        public int Team;
    }

    public class Main : BaseGamemode
    {

        Vehicle Truck;
        Vehicle Bike;
        Random rand;

        public static List<ICMBot> Bots = new List<ICMBot>();
        static int nextBotId = 9000;

        bool CanKill = false;

        List<string> Bikes = new List<string>() {
            "BMX",
            "Cruiser",
            "Fixter",
            "Scorcher",
            "TriBike"
        };

        public Main(  ) : base ( "ICM" ) {
            HUD = new HUD();
            rand = new Random( GetGameTimer() );
            EventHandlers["salty:icmDriverKillable"] += new Action( Killable );
            EventHandlers["salty::ICMRoundResult"] += new Action<string, string, string>( OnRoundResult );

            var gmInfo = GamemodeRegistry.Register( "icm", "Ice Cream Man",
                "One player drives the ice cream truck while others try to survive the chaos.", "#50c878" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 16;
            gmInfo.Tags = new string[] { "Asymmetric", "Vehicle" };
            gmInfo.Teams = new string[] { "Driver", "Survivors" };
            gmInfo.Features = new string[] { "Ice Cream Truck", "Bike Chase", "Trigger Zones" };
            gmInfo.Guide = new GuideSection {
                Overview = "Asymmetric vehicle chase. One player drives a massive ice cream truck trying to splatter the rest, who ride bicycles toward the finish line.",
                HowToWin = "Kids win by reaching the finish line. The Ice Cream Man wins by splattering all kids before time runs out.",
                Rules = new string[] {
                    "The truck is indestructible until the boost zone.",
                    "Kids can't exit their bikes.",
                    "Going out of bounds resets your position."
                },
                TeamRoles = new GuideTeamRole[] {
                    new GuideTeamRole {
                        Name = "Ice Cream Man", Color = "#50c878",
                        Goal = "Drive the truck and chase down the kids.",
                        Tips = new string[] { "Your truck is heavy and fast.", "Auto-respawns if you leave the map.", "Cut off escape routes rather than chasing directly." }
                    },
                    new GuideTeamRole {
                        Name = "Kids", Color = "#60a5fa",
                        Goal = "Ride your bike toward the finish line.",
                        Tips = new string[] { "After the boost zone you get a weapon to fight back.", "Use narrow paths the truck can't fit through.", "Stay unpredictable to avoid being caught." }
                    }
                },
                Tips = new string[] {
                    "Kids \u2014 use narrow paths the truck can't fit through.",
                    "Driver \u2014 cut off escape routes rather than chasing directly.",
                    "After the boost zone, bikes get a speed boost and a weapon."
                }
            };

            RegisterCommand( "spawnbot", new Action<int, List<object>, string>( async ( source, args, raw ) => {
                if( Team == 0 ) {
                    await SpawnKidBot();
                } else {
                    await SpawnICMBot();
                }
            } ), false );

            RegisterCommand( "clearbots", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ClearBots();
            } ), false );

            // Debug menu
            DebugRegistry.RegisterEntityProvider( "icm", () => BuildEntityListJson() );

            DebugRegistry.Register( "icm", "icm_set_team_icm", "Set Team: Ice Cream Man", "Teams", () => {
                SetTeam( 0 );
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", 0 );
                PlayerSpawn();
                WriteChat( "Debug", "You are now the Ice Cream Man", 30, 200, 30 );
                SendDebugEntityUpdate();
            } );
            DebugRegistry.Register( "icm", "icm_set_team_kid", "Set Team: Kid", "Teams", () => {
                SetTeam( 1 );
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", 1 );
                PlayerSpawn();
                WriteChat( "Debug", "You are now a Kid", 30, 200, 30 );
                SendDebugEntityUpdate();
            } );
            DebugRegistry.Register( "icm", "icm_respawn", "Respawn", "Self", () => {
                PlayerSpawn();
                WriteChat( "Debug", "Respawned", 30, 200, 30 );
            } );
            DebugRegistry.Register( "icm", "icm_spawn_bot", "Spawn Bot", "Bots", async () => {
                if( Team == 0 ) { await SpawnKidBot(); } else { await SpawnICMBot(); }
                SendDebugEntityUpdate();
            } );
            DebugRegistry.Register( "icm", "icm_clear_bots", "Clear All Bots", "Bots", () => {
                ClearBots();
                SendDebugEntityUpdate();
            } );
        }

        private void Killable() {
            if( Truck != null ) {
                Truck.IsInvincible = false;
                Truck.IsExplosionProof = false;
            }
        }

        public override async void Start( float gameTime ) {
            base.Start( gameTime );

            await RunCountdown();
        }

        public override void End() {
            ClearBots();
            SetWeaponDamageModifier( (uint)GetHashKey( "WEAPON_MICROSMG" ), 1 );

            if( Truck != null )
                Truck.Delete();
            if( Bike != null )
                Bike.Delete();
            base.End();

        }

        public override void PlayerSpawn() {           
            base.PlayerSpawn();

            if( Team == 0 ) {
                SpawnTruck();
            } else if ( Team == 1 ) {
                SpawnBike();
            } 

        }

        public override void Update() {
            CantExitVehichles();

            if( Team == 0 ) {
                if( !Game.PlayerPed.IsInVehicle() && Truck != null ) {
                    Game.PlayerPed.SetIntoVehicle( Truck, VehicleSeat.Driver );
                }

                uint streetName = 0;
                uint crossingName = 0;
                GetStreetNameAtCoord( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref streetName, ref crossingName );
                if( streetName == 629262578 || crossingName == 629262578 ) {
                    var velocity = Truck.Velocity;
                    var speed = Truck.Speed;
                    Truck.Position = ClientGlobals.LastSpawn;
                    Truck.Heading = ClientGlobals.LastSpawnHeading;
                    Truck.Velocity = velocity * 2;
                    Truck.Speed = speed * 1.5f;
                    SetGameplayCamRelativeHeading( 0 );
                    //AddScore( 1 );
                }

                // Reset truck to spawn when hitting map boundary
                if( Map != null && Truck != null && !Map.IsInZone( Game.PlayerPed.Position ) ) {
                    var velocity = Truck.Velocity;
                    var speed = Truck.Speed;
                    Truck.Position = ClientGlobals.LastSpawn;
                    Truck.Heading = ClientGlobals.LastSpawnHeading;
                    Truck.Velocity = velocity;
                    Truck.Speed = speed;
                    SetGameplayCamRelativeHeading( 0 );
                }
            }
            if( Team == 1 && !CanKill ) {
                Game.PlayerPed.CanBeKnockedOffBike = false;
                if( !Game.PlayerPed.IsInVehicle() && Bike != null ) {
                    Game.PlayerPed.SetIntoVehicle( Bike, VehicleSeat.Driver );
                }
                uint streetName = 0;
                uint crossingName = 0;
                GetStreetNameAtCoord( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref streetName, ref crossingName );
                if( streetName == 3436239235 || crossingName == 3436239235 ) {

                    if( Bike != null ) {
                        Bike.MaxSpeed = 500;
                        Bike.EnginePowerMultiplier = 500;
                        Bike.Position = ClientGlobals.LastSpawn;
                        Bike.Heading = ClientGlobals.LastSpawnHeading;
                        Bike.Velocity = Bike.ForwardVector * 5f;
                        Bike.Speed *= 15f;
                        SetGameplayCamRelativeHeading( 0 );
                        TriggerEvent( "salty:icmDriverKillable" );
                    }

                    GiveWeaponToPed( PlayerPedId(), (uint)GetHashKey( "WEAPON_MICROSMG" ), 100, false, true );
                    SetWeaponDamageModifier( (uint)GetHashKey( "WEAPON_MICROSMG" ), 9999 );
                    Game.PlayerPed.IsInvincible = true;
                    Game.PlayerPed.Weapons.Current.InfiniteAmmo = true;
                    Game.PlayerPed.Weapons.Current.InfiniteAmmoClip = true;
 
                }
            }

            if( CanKill ) {

            }

            base.Update();

        }

        public async Task SpawnBike() {
            if( Bike != null )
                Bike.Delete();
            Game.PlayerPed.Position = ClientGlobals.LastSpawn;
            Bike = await World.CreateVehicle( Bikes[rand.Next( 0, Bikes.Count )], Game.PlayerPed.Position, ClientGlobals.LastSpawnHeading );
            Bike.MaxSpeed = 60;
            Bike.EnginePowerMultiplier = 60;
            Game.PlayerPed.SetIntoVehicle( Bike, VehicleSeat.Driver );
            SetGameplayCamRelativeHeading( 0 );

        }

        public override void SetTeam( int team ) {
            base.SetTeam( team );
            switch( team ) {
                case 0:
                    HUD.TeamText.Caption = "Ice Cream Man";
                    HubNUI.ShowRoleReveal( "Ice Cream Man", "#f59e0b" );
                    HUD.SetGoal( "Splatter all the kids on Ice Cream Lane!", 245, 158, 11, 255, 10 );
                    break;
                case 1:
                    HUD.TeamText.Caption = "Kid";
                    HubNUI.ShowRoleReveal( "Kid", "#06b6d4" );
                    HUD.SetGoal( "Survive and reach the end of Ice Cream Lane!", 6, 182, 212, 255, 10 );
                    break;
            }
        }

        public void OnRoundResult( string winner, string color, string reason ) {
            HubNUI.ShowRoundEnd( winner, color, reason );
        }

        private static string EscapeJsonString( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
        }

        public string BuildEntityListJson() {
            var entries = new List<string>();

            string playerTeam = Team == 0 ? "Ice Cream Man" : "Kid";
            entries.Add( "{\"id\":" + LocalPlayer.ServerId + ",\"name\":\"" + EscapeJsonString( LocalPlayer.Name ) + " (You)\",\"type\":\"player\",\"team\":\"" + EscapeJsonString( playerTeam ) + "\"}" );

            foreach( var player in Players ) {
                if( player.ServerId == LocalPlayer.ServerId ) continue;
                entries.Add( "{\"id\":" + player.ServerId + ",\"name\":\"" + EscapeJsonString( player.Name ) + "\",\"type\":\"player\",\"team\":\"\"}" );
            }

            foreach( var bot in Bots ) {
                string botTeam = bot.Team == 0 ? "Ice Cream Man" : "Kid";
                entries.Add( "{\"id\":" + bot.FakeServerId + ",\"name\":\"Bot_" + bot.FakeServerId + "\",\"type\":\"bot\",\"team\":\"" + EscapeJsonString( botTeam ) + "\"}" );
            }

            return "[" + string.Join( ",", entries ) + "]";
        }

        public void SendDebugEntityUpdate() {
            HubNUI.SendDebugEntityUpdate( "icm" );
        }

        public override void OnWinBarrierReached( Vector3 pos ) {
            if( Team == 1 ) {
                TriggerServerEvent( "salty:netICMKidWin" );
                HubNUI.ShowRoundEnd( "Kids", "#06b6d4", "A kid reached the finish line!" );
            }
        }

        public async Task SpawnKidBot() {
            Vector3 spawnPos = ClientGlobals.LastSpawn;

            // Try to find kid (team 1) spawn from cached maps
            foreach( var kvp in ClientGlobals.Maps ) {
                var mapSpawns = kvp.Value.Spawns.Where( s => s.SpawnType == SpawnType.PLAYER && s.Team == 1 ).ToList();
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
            string bikeModel = Bikes[rand.Next( Bikes.Count )];

            uint pedHash = (uint)GetHashKey( pedModelName );
            uint vehHash = (uint)GetHashKey( bikeModel );
            RequestModel( pedHash );
            RequestModel( vehHash );

            int timeout = 0;
            while( (!HasModelLoaded( pedHash ) || !HasModelLoaded( vehHash )) && timeout < 100 ) {
                await Delay( 50 );
                timeout++;
            }
            NewLoadSceneStop();

            if( !HasModelLoaded( pedHash ) || !HasModelLoaded( vehHash ) ) {
                WriteChat( "ICM", "Failed to load bot models (ped:" + HasModelLoaded( pedHash ) + " veh:" + HasModelLoaded( vehHash ) + ")", 200, 30, 30 );
                return;
            }

            float groundZ = spawnPos.Z;
            float outZ = 0f;
            if( GetGroundZFor_3dCoord( spawnPos.X, spawnPos.Y, spawnPos.Z + 5f, ref outZ, false ) ) {
                groundZ = outZ + 0.5f;
            }

            int vehicle = CreateVehicle( vehHash, spawnPos.X, spawnPos.Y, groundZ, 0f, false, false );
            if( vehicle == 0 ) {
                WriteChat( "ICM", "Failed to create bot vehicle", 200, 30, 30 );
                return;
            }
            SetEntityAsMissionEntity( vehicle, true, true );
            SetVehicleOnGroundProperly( vehicle );

            await Delay( 100 );

            int ped = CreatePed( 4, pedHash, spawnPos.X, spawnPos.Y, groundZ, 0f, false, false );
            if( ped == 0 ) {
                WriteChat( "ICM", "Failed to create bot ped", 200, 30, 30 );
                DeleteVehicle( ref vehicle );
                return;
            }
            SetEntityAsMissionEntity( ped, true, true );
            SetPedIntoVehicle( ped, vehicle, -1 );
            SetBlockingOfNonTemporaryEvents( ped, true );
            SetPedFleeAttributes( ped, 0, false );

            SetModelAsNoLongerNeeded( pedHash );
            SetModelAsNoLongerNeeded( vehHash );

            // Target: win barrier if available, else team 0 spawn
            Vector3 target = ClientGlobals.LastSpawn;
            if( WinBarriers.Count > 0 ) {
                target = WinBarriers[0].Position;
            } else {
                foreach( var kvp in ClientGlobals.Maps ) {
                    var icmSpawns = kvp.Value.Spawns.Where( s => s.SpawnType == SpawnType.PLAYER && s.Team == 0 ).ToList();
                    if( icmSpawns.Count > 0 ) {
                        target = icmSpawns[0].Position;
                        break;
                    }
                }
            }

            TaskVehicleDriveToCoordLongrange( ped, vehicle, target.X, target.Y, target.Z, 30f, 786468, 5f );

            int botId = nextBotId++;
            Bots.Add( new ICMBot { PedHandle = ped, VehicleHandle = vehicle, FakeServerId = botId, Team = 1 } );
            WriteChat( "ICM", "Spawned kid bot on " + bikeModel + " at " + spawnPos.X.ToString("F1") + ", " + spawnPos.Y.ToString("F1") + ", " + groundZ.ToString("F1"), 30, 200, 30 );
        }

        public async Task SpawnICMBot() {
            Vector3 spawnPos = ClientGlobals.LastSpawn;

            // Try to find ICM (team 0) spawn from cached maps
            foreach( var kvp in ClientGlobals.Maps ) {
                var mapSpawns = kvp.Value.Spawns.Where( s => s.SpawnType == SpawnType.PLAYER && s.Team == 0 ).ToList();
                if( mapSpawns.Count > 0 ) {
                    spawnPos = mapSpawns[rand.Next( mapSpawns.Count )].Position;
                    break;
                }
            }

            // Load collision at spawn point
            RequestCollisionAtCoord( spawnPos.X, spawnPos.Y, spawnPos.Z );
            NewLoadSceneStart( spawnPos.X, spawnPos.Y, spawnPos.Z, spawnPos.X, spawnPos.Y, spawnPos.Z, 50f, 0 );

            uint pedHash = (uint)GetHashKey( "a_m_m_business_01" );
            uint vehHash = (uint)GetHashKey( "cutter" );
            RequestModel( pedHash );
            RequestModel( vehHash );

            int timeout = 0;
            while( (!HasModelLoaded( pedHash ) || !HasModelLoaded( vehHash )) && timeout < 100 ) {
                await Delay( 50 );
                timeout++;
            }
            NewLoadSceneStop();

            if( !HasModelLoaded( pedHash ) || !HasModelLoaded( vehHash ) ) {
                WriteChat( "ICM", "Failed to load bot models (ped:" + HasModelLoaded( pedHash ) + " veh:" + HasModelLoaded( vehHash ) + ")", 200, 30, 30 );
                return;
            }

            float groundZ = spawnPos.Z;
            float outZ = 0f;
            if( GetGroundZFor_3dCoord( spawnPos.X, spawnPos.Y, spawnPos.Z + 5f, ref outZ, false ) ) {
                groundZ = outZ + 0.5f;
            }

            int vehicle = CreateVehicle( vehHash, spawnPos.X, spawnPos.Y, groundZ, 0f, false, false );
            if( vehicle == 0 ) {
                WriteChat( "ICM", "Failed to create bot vehicle", 200, 30, 30 );
                return;
            }
            SetEntityAsMissionEntity( vehicle, true, true );
            SetVehicleOnGroundProperly( vehicle );

            await Delay( 100 );

            int ped = CreatePed( 4, pedHash, spawnPos.X, spawnPos.Y, groundZ, 0f, false, false );
            if( ped == 0 ) {
                WriteChat( "ICM", "Failed to create bot ped", 200, 30, 30 );
                DeleteVehicle( ref vehicle );
                return;
            }
            SetEntityAsMissionEntity( ped, true, true );
            SetPedIntoVehicle( ped, vehicle, -1 );
            SetBlockingOfNonTemporaryEvents( ped, true );
            SetPedFleeAttributes( ped, 0, false );

            // Apply truck handling
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fCamberStiffnesss", 0.1f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fMass", 10000f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "FTRACTIONSPRINGDELTAMAX", 100f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fSteeringLock", 40f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fDriveInertia", 1f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fDriveBiasFront", 0.5f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fTractionCurveLateral", 25f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fTractionCurveMax", 5f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fTractionCurveMin", 5f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fTractionBiasFront", 0.5f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fTractionLossMult", 0.1f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fSuspensionReboundDamp", 2f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fSuspensionCompDamp", 2f );
            SetVehicleHandlingFloat( vehicle, "CHandlingData", "fSuspensionForce", 3f );
            SetVehicleHasStrongAxles( vehicle, true );
            SetVehicleHighGear( vehicle, 1 );

            SetModelAsNoLongerNeeded( pedHash );
            SetModelAsNoLongerNeeded( vehHash );

            // Target: team 1 (kid) spawn
            Vector3 target = ClientGlobals.LastSpawn;
            foreach( var kvp in ClientGlobals.Maps ) {
                var kidSpawns = kvp.Value.Spawns.Where( s => s.SpawnType == SpawnType.PLAYER && s.Team == 1 ).ToList();
                if( kidSpawns.Count > 0 ) {
                    target = kidSpawns[0].Position;
                    break;
                }
            }

            TaskVehicleDriveToCoordLongrange( ped, vehicle, target.X, target.Y, target.Z, 50f, 786468, 5f );

            int botId = nextBotId++;
            Bots.Add( new ICMBot { PedHandle = ped, VehicleHandle = vehicle, FakeServerId = botId, Team = 0 } );
            WriteChat( "ICM", "Spawned ICM bot at " + spawnPos.X.ToString("F1") + ", " + spawnPos.Y.ToString("F1") + ", " + groundZ.ToString("F1"), 30, 200, 30 );
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
            WriteChat( "ICM", "All bots cleared.", 200, 200, 30 );
        }

        public async Task SpawnTruck() {
            if( Truck != null )
                Truck.Delete();
            Game.PlayerPed.Position = ClientGlobals.LastSpawn;
            Truck = await World.CreateVehicle( "cutter", Game.PlayerPed.Position, ClientGlobals.LastSpawnHeading );
            Truck.CanBeVisiblyDamaged = false;
            Truck.CanEngineDegrade = false;
            Truck.CanTiresBurst = false;
            Truck.CanWheelsBreak = false;
            Truck.EngineHealth = 999999;
            Truck.MaxHealth = 999999;
            Truck.Health = 999999;
            Truck.EnginePowerMultiplier = 150;
            Truck.MaxSpeed = 999;
            Truck.Gravity = 50;
            Truck.IsInvincible = true;
            Truck.IsFireProof = true;

            SetPlayerVehicleDamageModifier( PlayerId(), 99999f );
            SetVehicleDamageModifier( Truck.Handle, 99999 );


            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fCamberStiffnesss", 0.1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fMass", 10000f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "FTRACTIONSPRINGDELTAMAX", 100f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSteeringLock", 40f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fDriveInertia", 1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fDriveBiasFront", 0.5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionCurveLateral", 25f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionCurveMax", 5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionCurveMin", 5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionBiasFront", 0.5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionLossMult", 0.1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSuspensionReboundDamp", 2f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSuspensionCompDamp", 2f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSuspensionForce", 3f );
            SetVehicleHasStrongAxles( NetworkGetEntityFromNetworkId( Truck.NetworkId ), true );
            SetVehicleHighGear( NetworkGetEntityFromNetworkId( Truck.NetworkId ), 1 );

            Game.PlayerPed.SetIntoVehicle( Truck, VehicleSeat.Driver );

            SetGameplayCamRelativeHeading( 0 );

        }


    }


}
