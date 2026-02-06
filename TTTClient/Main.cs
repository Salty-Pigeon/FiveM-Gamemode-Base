using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;
using MenuAPI;

namespace TTTClient
{
    public enum Teams {
        Innocent,
        Traitor,
        Detective
    }

    // Test bot for solo mode
    public class TestBot {
        public int Handle;
        public int FakeServerId;
        public string Name;
        public bool IsDisguised;
        public Teams Team;

        public TestBot( int handle, int fakeId, string name, Teams team ) {
            Handle = handle;
            FakeServerId = fakeId;
            Name = name;
            Team = team;
            IsDisguised = false;
        }
    }

    public class Main : BaseGamemode
    {

        public static Dictionary<int, DeadBody> DeadBodies = new Dictionary<int, DeadBody>();

        public static BuyMenu BuyMenu;
        ControlsMenu ControlsMenu;

        public static bool CanTeleport = false;
        public static Vector3 SavedTeleport;
        float teleportLength;
        float teleportTime = 0;
        bool hasTeleported = false;
        bool isTeleporting = false;
        float teleportWait = 0;
        float teleportDelay = 5 * 1000;

        public static bool CanDisguise = false;
        public bool isDisguised = false;

        bool menuOpen = false;

        // Test bots for solo mode
        public static List<TestBot> TestBots = new List<TestBot>();
        static int nextBotId = 9000; // Fake server IDs for bots

        public Main() : base( "TTT" ) {
            RequestStreamedTextureDict( "saltyTextures", true );
            HUD = new TTTHUD();
            CanTeleport = false;
            CanDisguise = false;
            EventHandlers["salty::SpawnDeadBody"] += new Action<Vector3, int, int, uint>( SpawnDeadBody );
            EventHandlers["salty::UpdateDeadBody"] += new Action<int>( BodyDiscovered );

            RegisterCommand( "controls", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ControlsMenu = new ControlsMenu( "Control Menu", "TTT Controls" );
                ControlsMenu.controlMenu.OpenMenu();
            } ), false );

            // Solo testing bot commands
            RegisterCommand( "spawnbot", new Action<int, List<object>, string>( async ( source, args, raw ) => {
                await SpawnTestBot();
            } ), false );

            RegisterCommand( "botdisguise", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ToggleBotDisguise();
            } ), false );

            RegisterCommand( "clearbots", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ClearTestBots();
            } ), false );
        }

        public static async Task SpawnTestBot() {
            Vector3 playerPos = Game.PlayerPed.Position;
            Vector3 spawnPos = playerPos + Game.PlayerPed.ForwardVector * 3f;

            // Random ped models for variety
            string[] pedModels = { "a_m_y_hipster_01", "a_f_y_hipster_02", "a_m_m_business_01", "a_f_m_business_02", "a_m_y_skater_01" };
            string modelName = pedModels[new Random().Next( pedModels.Length )];

            uint modelHash = (uint)GetHashKey( modelName );
            RequestModel( modelHash );

            int timeout = 0;
            while( !HasModelLoaded( modelHash ) && timeout < 100 ) {
                await BaseScript.Delay( 10 );
                timeout++;
            }

            if( !HasModelLoaded( modelHash ) ) {
                WriteChat( "TTT", "Failed to load bot model", 200, 30, 30 );
                return;
            }

            int ped = CreatePed( 4, modelHash, spawnPos.X, spawnPos.Y, spawnPos.Z, 0f, true, false );
            SetEntityAsMissionEntity( ped, true, true );
            SetBlockingOfNonTemporaryEvents( ped, true );
            SetPedFleeAttributes( ped, 0, false );
            SetPedCombatAttributes( ped, 17, true );
            TaskStandStill( ped, -1 );

            SetModelAsNoLongerNeeded( modelHash );

            int botId = nextBotId++;
            string botName = "Bot_" + (TestBots.Count + 1);
            TestBot bot = new TestBot( ped, botId, botName, Teams.Innocent );
            TestBots.Add( bot );

            // Register bot in player details system
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.SetPlayerDetail( botId, "team", (int)Teams.Innocent );
                ClientGlobals.CurrentGame.SetPlayerDetail( botId, "disguised", false );
            }

            WriteChat( "TTT", $"Spawned {botName} as Innocent. Use /botdisguise to toggle disguise.", 30, 200, 30 );
        }

        public static void ToggleBotDisguise() {
            if( TestBots.Count == 0 ) {
                WriteChat( "TTT", "No bots spawned. Use /spawnbot first.", 200, 200, 30 );
                return;
            }

            // Toggle disguise on the most recent bot
            TestBot bot = TestBots[TestBots.Count - 1];
            bot.IsDisguised = !bot.IsDisguised;

            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.SetPlayerDetail( bot.FakeServerId, "disguised", bot.IsDisguised );
            }

            string status = bot.IsDisguised ? "ON (name hidden)" : "OFF (name visible)";
            WriteChat( "TTT", $"{bot.Name} disguise: {status}", 200, 200, 30 );
        }

        public static void ClearTestBots() {
            foreach( var bot in TestBots ) {
                if( DoesEntityExist( bot.Handle ) ) {
                    int handle = bot.Handle;
                    SetEntityAsMissionEntity( handle, false, true );
                    DeletePed( ref handle );
                }
            }
            TestBots.Clear();
            WriteChat( "TTT", "All test bots cleared.", 200, 200, 30 );
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

            Globals.GameCoins = 1;

            // Clear any leftover bots from previous games
            ClearTestBots();
            DeadBodies.Clear();
        }

        public override void Update() {
            base.Update();

            if( isTeleporting ) {
                DoTeleport();
            }

            CantEnterVehichles();
            foreach( var body in DeadBodies ) {
                if( !IsPedRagdoll( body.Value.ID ) )
                    body.Value.Update();
            }

            // Update test bots - show markers and handle interactions
            UpdateTestBots();
        }

        void UpdateTestBots() {
            foreach( var bot in TestBots.ToList() ) {
                if( !DoesEntityExist( bot.Handle ) ) {
                    TestBots.Remove( bot );
                    continue;
                }

                Vector3 botPos = GetEntityCoords( bot.Handle, true );

                // Draw marker above innocent bots (green for innocent)
                int r = 30, g = 200, b = 30;
                if( bot.IsDisguised ) {
                    r = 200; g = 200; b = 30; // Yellow when disguised
                }
                DrawMarker( 2, botPos.X, botPos.Y, botPos.Z + 1.2f, 0, 0, 0, 0, 180, 0, 0.3f, 0.3f, 0.3f, r, g, b, 150, false, true, 2, false, null, null, false );
            }
        }

        public void DetectiveMenu() {
            BuyMenu = new BuyMenu( "Buy Menu", "Detective" );
            Action radar = () => {
                ((TTTHUD)HUD).SetRadarActive( true );
            };
            BuyMenu.AddItem( "Radar", 1, radar );
            Action teleport = () => {
                CanTeleport = true;
            };
            BuyMenu.AddItem( "Teleporter", 1, teleport );
            Action disguise = () => {
                CanDisguise = true;
            };
            BuyMenu.AddItem( "Disguise", 1, disguise );
            menuOpen = true;
            BuyMenu.buyMenu.OpenMenu();
        }

        public void TraitorMenu() {
            BuyMenu = new BuyMenu( "Buy Menu", "Traitor" );
            Action radar = () => {
                ((TTTHUD)HUD).SetRadarActive( true );
            };
            BuyMenu.AddItem( "Radar", 1, radar );
            Action teleport = () => {
                CanTeleport = true;
            };
            BuyMenu.AddItem( "Teleporter", 1, teleport );
            Action disguise = () => {
                CanDisguise = true;
            };
            BuyMenu.AddItem( "Disguise", 1, disguise );
            menuOpen = true;
            BuyMenu.buyMenu.OpenMenu();
        }

        public override void Controls() {
            base.Controls();

            int buyMenuKey = ControlConfig.GetControl( "ttt", "BuyMenu" );
            int setTeleportKey = ControlConfig.GetControl( "ttt", "SetTeleport" );
            int useTeleportKey = ControlConfig.GetControl( "ttt", "UseTeleport" );
            int interactKey = ControlConfig.GetControl( "ttt", "Interact" );
            int disguiseKey = ControlConfig.GetControl( "ttt", "Disguise" );

            if( IsControlJustReleased( 0, buyMenuKey ) ) {
                menuOpen = !menuOpen;
                if( menuOpen ) {
                    if( Team == (int)Teams.Traitor ) {
                        TraitorMenu();
                    }
                    else if( Team == (int)Teams.Detective ) {
                        DetectiveMenu();
                    }
                }
            }

            if( IsControlJustPressed( 0, setTeleportKey ) ) {
                if( CanTeleport ) {
                    WriteChat( "TTT", "Position set", 200, 200, 0 );
                    SavedTeleport = Game.PlayerPed.Position;
                }
            }

            if( IsControlJustPressed( 0, useTeleportKey ) ) {
                if( CanTeleport )
                    TeleportToSaved( 3 * 1000 );
            }

            if( IsControlJustPressed( 0, interactKey ) ) {
                foreach( var body in DeadBodies ) {
                    Vector3 myPos = Game.PlayerPed.Position;
                    float dist = GetDistanceBetweenCoords( myPos.X, myPos.Y, myPos.Z, body.Value.Position.X, body.Value.Position.Y, body.Value.Position.Z, true );
                    if( dist > 2 ) { continue; }
                    if( body.Value.isDiscovered ) {
                        if( Team == (int)Teams.Detective ) {
                            WriteChat( "TTT", "Scanning DNA", 0, 0, 230 );
                            ((TTTHUD)HUD).DetectiveTracing = body.Value.KillerPed;
                        }
                    }
                    else {
                        body.Value.View();
                        TriggerServerEvent( "salty::netBodyDiscovered", body.Key );
                    }
                }
            }

            if( IsControlJustPressed( 0, disguiseKey ) ) {
                if( CanDisguise ) {
                    isDisguised = !isDisguised;
                    TriggerServerEvent( "salty:netUpdatePlayerDetail", "disguised", isDisguised );
                    if( isDisguised ) {
                        WriteChat( "TTT", "Disguise enabled", 200, 200, 20 );
                    } else {
                        WriteChat( "TTT", "Disguise disabled", 200, 200, 20 );
                    }
                }
            }

        }

        public override void OnDetailUpdate( int ply, string key, object oldValue, object newValue ) {
            base.OnDetailUpdate( ply, key, oldValue, newValue );
        }

        public override void PlayerSpawn() {
            base.PlayerSpawn();
            LocalPlayer.Character.Position = ClientGlobals.LastSpawn;
        }


        public void TeleportToSaved( float time ) {
            if( isTeleporting ) {
                return;
            }
            if( teleportWait > GetGameTimer() ) {
                TimeSpan timer = TimeSpan.FromMilliseconds( teleportWait - GetGameTimer() );
                WriteChat( "TTT", "Cooldown " + timer.Seconds + "s", 200, 20, 20 );
                return;
            }
            if( Game.PlayerPed.IsShooting || Game.PlayerPed.IsJumping || Game.PlayerPed.IsInAir || Game.PlayerPed.IsReloading || Game.PlayerPed.IsClimbing || Game.PlayerPed.IsGoingIntoCover || Game.PlayerPed.IsRagdoll || Game.PlayerPed.IsGettingUp )
                return;

            if( SavedTeleport == Vector3.Zero ) {
                WriteChat( "TTT", "No destination set", 0, 0, 200 );
                return;
            }
            teleportLength = time;
            teleportTime = GetGameTimer() + teleportLength;
            isTeleporting = true;
        }

        public void DoTeleport() {
            float gameTime = GetGameTimer();
            if( teleportTime > gameTime ) {
                DisableControlAction( 0, 30, true ); // Disable movement
                DisableControlAction( 0, 31, true );
                DisableControlAction( 0, 24, true );
                DisableControlAction( 0, 257, true );
                DisableControlAction( 0, 22, true );
                DisableControlAction( 0, 21, true );

                int alpha = (int)Math.Round( 255 * ((teleportTime - gameTime) / (teleportLength / 2)) );
                if( teleportTime - gameTime <= (teleportLength / 2) ) {
                    if( !hasTeleported ) {
                        hasTeleported = true;
                        Game.PlayerPed.Position = SavedTeleport;
                    }
                    alpha = 255 - alpha;
                }
                Game.PlayerPed.Opacity = alpha;
                //SetEntityAlpha( PlayerPedId(), alpha, 0 );
            }
            else if( teleportTime < gameTime ) {
                isTeleporting = false;
                hasTeleported = false;
                teleportWait = GetGameTimer() + teleportDelay;
            }
        }

        public void BodyDiscovered( int body ) {
            if( !DeadBodies[body].isDiscovered ) {
                DeadBodies[body].Discovered();
                WriteChat( "TTT", DeadBodies[body].Name + "'s body has been discovered", 255, 0, 0 );
            }
        }

        public void SpawnDeadBody( Vector3 position, int ply, int killer, uint weaponHash ) {
            int player = GetPlayerFromServerId( ply );
            int kill = GetPlayerFromServerId( killer );
            DeadBodies.Add( ply, new DeadBody( position, player, kill, weaponHash ) );
        }

        public override void SetTeam( int team ) {
            base.SetTeam( team );
            switch( team ) {
                case 0:
                    HUD.TeamText.Caption = "Innocent";
                    HUD.SetGoal( "Defeat the traitors", 20, 200, 20, 200, 5 );
                    break;
                case 1:
                    HUD.TeamText.Caption = "Traitor";
                    HUD.SetGoal( "You are a Traitor", 200, 20, 20, 200, 5 );
                    break;
                case 2:
                    HUD.TeamText.Caption = "Detective";
                    HUD.SetGoal( "Help the innocents find the traitors", 20, 20, 200, 200, 5 );
                    break;
                default:
                    HUD.TeamText.Caption = "Spectator";
                    break;
            }
        }
    }
}
