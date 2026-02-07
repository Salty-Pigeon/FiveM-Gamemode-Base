using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;
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

        public static bool CanTeleport = false;
        public static Vector3 SavedTeleport;
        public static float teleportLength;
        public static float teleportTime = 0;
        public static bool hasTeleported = false;
        public static bool isTeleporting = false;
        public static float teleportWait = 0;
        public static float teleportDelay = 5 * 1000;
        public static string teleportStatus = "";
        public static float teleportStatusTime = 0f;

        public static bool CanDisguise = false;
        public static bool isDisguised = false;
        public static string disguiseStatus = "";
        public static float disguiseStatusTime = 0f;

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
            EventHandlers["salty::TTTRoundResult"] += new Action<string, string, string>( OnRoundResult );

            var gmInfo = GamemodeRegistry.Register( "ttt", "Trouble in Terrorist Town",
                "Discover who among you is a traitor before it's too late.", "#e94560" );
            gmInfo.MinPlayers = 4;
            gmInfo.MaxPlayers = 16;
            gmInfo.Tags = new string[] { "Social Deduction", "FPS" };
            gmInfo.Teams = new string[] { "Innocent", "Traitor", "Detective" };
            gmInfo.Features = new string[] { "Buy Menu", "DNA Scanner", "Disguise", "Teleporter" };

            ControlConfig.RegisterDefaults( "ttt",
                new Dictionary<string, int>() {
                    { "BuyMenu", 244 },
                    { "SetTeleport", 121 },
                    { "UseTeleport", 212 },
                    { "Interact", 38 },
                    { "Disguise", 243 },
                    { "DropWeapon", 23 },
                },
                new Dictionary<string, string>() {
                    { "BuyMenu", "Buy Menu" },
                    { "SetTeleport", "Set Teleport" },
                    { "UseTeleport", "Use Teleport" },
                    { "Interact", "Interact / Scan Body" },
                    { "Disguise", "Toggle Disguise" },
                    { "DropWeapon", "Drop Weapon" },
                }
            );

            // Entity provider
            DebugRegistry.RegisterEntityProvider( "ttt", () => BuildEntityListJson() );

            // Self actions (non-targeted)
            DebugRegistry.Register( "ttt", "ttt_give_1_coin", "Give 1 Coin", "Self", () => DebugGiveCoins( 1 ) );
            DebugRegistry.Register( "ttt", "ttt_give_5_coins", "Give 5 Coins", "Self", () => DebugGiveCoins( 5 ) );

            // Target actions
            DebugRegistry.RegisterTargetAction( "ttt", "ttt_set_innocent", "Set Team: Innocent", "Target Actions", ( id ) => DebugSetEntityTeam( id, Teams.Innocent ) );
            DebugRegistry.RegisterTargetAction( "ttt", "ttt_set_traitor", "Set Team: Traitor", "Target Actions", ( id ) => DebugSetEntityTeam( id, Teams.Traitor ) );
            DebugRegistry.RegisterTargetAction( "ttt", "ttt_set_detective", "Set Team: Detective", "Target Actions", ( id ) => DebugSetEntityTeam( id, Teams.Detective ) );
            DebugRegistry.RegisterTargetAction( "ttt", "ttt_toggle_disguise", "Toggle Disguise", "Target Actions", ( id ) => DebugToggleEntityDisguise( id ) );
            DebugRegistry.RegisterTargetAction( "ttt", "ttt_kill_spawn_body", "Kill → Spawn Body", "Target Actions", ( id ) => DebugKillEntity( id ) );

            // Bot actions (non-targeted)
            DebugRegistry.Register( "ttt", "ttt_spawn_bot", "Spawn Bot", "Bots", async () => { await SpawnTestBot(); SendDebugEntityUpdate(); } );
            DebugRegistry.Register( "ttt", "ttt_clear_bots", "Clear All Bots", "Bots", () => { ClearTestBots(); SendDebugEntityUpdate(); } );

            // Solo testing bot commands
            RegisterCommand( "spawnbot", new Action<int, List<object>, string>( async ( source, args, raw ) => {
                await SpawnTestBot();
                SendDebugEntityUpdate();
            } ), false );

            RegisterCommand( "botdisguise", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ToggleBotDisguise();
            } ), false );

            RegisterCommand( "clearbots", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ClearTestBots();
                SendDebugEntityUpdate();
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

        public void DebugGiveCoins( int amount ) {
            int current = 0;
            if( ClientGlobals.CurrentGame != null ) {
                object val = ClientGlobals.CurrentGame.GetPlayerDetail( LocalPlayer.ServerId, "coins" );
                if( val != null ) current = Convert.ToInt32( val );
            }
            int total = current + amount;
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.SetPlayerDetail( LocalPlayer.ServerId, "coins", total );
            }
            TriggerServerEvent( "salty:netUpdatePlayerDetail", "coins", total );
            WriteChat( "Debug", "Gave " + amount + " coin(s). Total: " + total, 30, 200, 30 );
        }

        public void DebugSetEntityTeam( int entityId, Teams team ) {
            if( entityId == LocalPlayer.ServerId ) {
                SetTeam( (int)team );
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "team", (int)team );
                WriteChat( "Debug", "Your team set to: " + team.ToString(), 30, 200, 30 );
            } else {
                TestBot bot = TestBots.FirstOrDefault( b => b.FakeServerId == entityId );
                if( bot == null ) {
                    WriteChat( "Debug", "Entity not found.", 200, 30, 30 );
                    return;
                }
                bot.Team = team;
                if( ClientGlobals.CurrentGame != null ) {
                    ClientGlobals.CurrentGame.SetPlayerDetail( bot.FakeServerId, "team", (int)team );
                }
                WriteChat( "Debug", bot.Name + " team set to: " + team.ToString(), 30, 200, 30 );
            }
            SendDebugEntityUpdate();
        }

        public void DebugToggleEntityDisguise( int entityId ) {
            if( entityId == LocalPlayer.ServerId ) {
                isDisguised = !isDisguised;
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "disguised", isDisguised );
                string status = isDisguised ? "ON (name hidden)" : "OFF (name visible)";
                WriteChat( "Debug", "Your disguise: " + status, 30, 200, 30 );
            } else {
                TestBot bot = TestBots.FirstOrDefault( b => b.FakeServerId == entityId );
                if( bot == null ) {
                    WriteChat( "Debug", "Entity not found.", 200, 30, 30 );
                    return;
                }
                bot.IsDisguised = !bot.IsDisguised;
                if( ClientGlobals.CurrentGame != null ) {
                    ClientGlobals.CurrentGame.SetPlayerDetail( bot.FakeServerId, "disguised", bot.IsDisguised );
                }
                string status = bot.IsDisguised ? "ON (name hidden)" : "OFF (name visible)";
                WriteChat( "Debug", bot.Name + " disguise: " + status, 30, 200, 30 );
            }
            SendDebugEntityUpdate();
        }

        public void DebugKillEntity( int entityId ) {
            TestBot bot = TestBots.FirstOrDefault( b => b.FakeServerId == entityId );
            if( bot == null ) {
                WriteChat( "Debug", "Can only kill bots.", 200, 30, 30 );
                return;
            }
            if( !DoesEntityExist( bot.Handle ) ) {
                WriteChat( "Debug", "Bot entity no longer exists.", 200, 30, 30 );
                TestBots.Remove( bot );
                SendDebugEntityUpdate();
                return;
            }

            Vector3 pos = GetEntityCoords( bot.Handle, true );
            uint model = (uint)GetEntityModel( bot.Handle );
            string name = bot.Name;

            int handle = bot.Handle;
            SetEntityAsMissionEntity( handle, false, true );
            DeletePed( ref handle );
            TestBots.Remove( bot );

            int bodyKey = bot.FakeServerId;
            DeadBodies[bodyKey] = new DeadBody( pos, model, name, bot.Team.ToString() );
            WriteChat( "Debug", name + " killed. Dead body spawned.", 30, 200, 30 );
            SendDebugEntityUpdate();
        }

        private static string EscapeJsonString( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
        }

        public string BuildEntityListJson() {
            var entries = new List<string>();

            // Local player
            string playerTeam = "";
            if( ClientGlobals.CurrentGame != null ) {
                object teamVal = ClientGlobals.CurrentGame.GetPlayerDetail( LocalPlayer.ServerId, "team" );
                if( teamVal != null ) playerTeam = ((Teams)Convert.ToInt32( teamVal )).ToString();
            }
            entries.Add( "{\"id\":" + LocalPlayer.ServerId + ",\"name\":\"" + EscapeJsonString( LocalPlayer.Name ) + " (You)\",\"type\":\"player\",\"team\":\"" + EscapeJsonString( playerTeam ) + "\"}" );

            // Other players
            foreach( var player in Players ) {
                if( player.ServerId == LocalPlayer.ServerId ) continue;
                string pTeam = "";
                if( ClientGlobals.CurrentGame != null ) {
                    object teamVal = ClientGlobals.CurrentGame.GetPlayerDetail( player.ServerId, "team" );
                    if( teamVal != null ) pTeam = ((Teams)Convert.ToInt32( teamVal )).ToString();
                }
                entries.Add( "{\"id\":" + player.ServerId + ",\"name\":\"" + EscapeJsonString( player.Name ) + "\",\"type\":\"player\",\"team\":\"" + EscapeJsonString( pTeam ) + "\"}" );
            }

            // Test bots
            foreach( var bot in TestBots ) {
                string botDisguised = bot.IsDisguised ? "true" : "false";
                entries.Add( "{\"id\":" + bot.FakeServerId + ",\"name\":\"" + EscapeJsonString( bot.Name ) + "\",\"type\":\"bot\",\"team\":\"" + EscapeJsonString( bot.Team.ToString() ) + "\",\"disguised\":" + botDisguised + "}" );
            }

            return "[" + string.Join( ",", entries ) + "]";
        }

        public void SendDebugEntityUpdate() {
            HubNUI.SendDebugEntityUpdate( "ttt" );
        }

        public override async void Start( float gameTime ) {
            base.Start( gameTime );

            Globals.GameCoins = 1;

            // Clear any leftover bots from previous games
            ClearTestBots();
            DeadBodies.Clear();

            // Freeze player immediately
            FreezeEntityPosition( PlayerPedId(), true );

            // Wait for role reveal to finish
            await Delay( 3200 );

            HubNUI.ShowCountdown( 3 );
            await Delay( 1000 );
            HubNUI.ShowCountdown( 2 );
            await Delay( 1000 );
            HubNUI.ShowCountdown( 1 );
            await Delay( 1000 );
            HubNUI.ShowCountdown( 0 ); // "GO"

            // Unfreeze player
            FreezeEntityPosition( PlayerPedId(), false );
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
                    teleportStatus = "POSITION SET";
                    teleportStatusTime = GetGameTimer();
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
                            body.Value.isDetectiveScanned = true;
                        }
                    }
                    else {
                        body.Value.View();
                        if( body.Key >= 9000 ) {
                            BodyDiscovered( body.Key );
                        } else {
                            TriggerServerEvent( "salty::netBodyDiscovered", body.Key );
                        }
                        if( Team == (int)Teams.Detective ) {
                            body.Value.isDetectiveScanned = true;
                        }
                    }
                    // Show body inspect overlay
                    string bodyTeam = body.Value.Team ?? "Unknown";
                    string bodyTeamColor = "#8888aa";
                    switch( bodyTeam ) {
                        case "Innocent": bodyTeamColor = "#22c55e"; break;
                        case "Traitor": bodyTeamColor = "#ef4444"; break;
                        case "Detective": bodyTeamColor = "#3b82f6"; break;
                    }
                    string weaponClue = body.Value.isDetectiveScanned ? body.Value.GetWeaponGroupClue() : "";
                    string deathTimeClue = body.Value.isDetectiveScanned ? body.Value.GetDeathTimeAgo() : "";
                    HubNUI.ShowBodyInspect( body.Value.Name, bodyTeam, bodyTeamColor,
                        weaponClue, deathTimeClue );
                }
            }

            if( IsControlJustPressed( 0, disguiseKey ) ) {
                if( CanDisguise ) {
                    isDisguised = !isDisguised;
                    TriggerServerEvent( "salty:netUpdatePlayerDetail", "disguised", isDisguised );
                    disguiseStatus = isDisguised ? "DISGUISE ON" : "DISGUISE OFF";
                    disguiseStatusTime = GetGameTimer();
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
                teleportStatus = "COOLDOWN " + timer.Seconds + "s";
                teleportStatusTime = GetGameTimer();
                return;
            }
            if( Game.PlayerPed.IsShooting || Game.PlayerPed.IsJumping || Game.PlayerPed.IsInAir || Game.PlayerPed.IsReloading || Game.PlayerPed.IsClimbing || Game.PlayerPed.IsGoingIntoCover || Game.PlayerPed.IsRagdoll || Game.PlayerPed.IsGettingUp )
                return;

            if( SavedTeleport == Vector3.Zero ) {
                teleportStatus = "NO DESTINATION";
                teleportStatusTime = GetGameTimer();
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

                float remaining = teleportTime - gameTime;
                float half = teleportLength / 2;
                float progress = remaining / teleportLength; // 1.0 → 0.0

                int alpha = (int)Math.Round( 255 * (remaining / half) );
                if( remaining <= half ) {
                    if( !hasTeleported ) {
                        hasTeleported = true;
                        Game.PlayerPed.Position = SavedTeleport;
                    }
                    alpha = 255 - alpha;
                }
                Game.PlayerPed.Opacity = alpha;

                // Screen overlay — dark fade that peaks at midpoint
                int overlayAlpha;
                if( remaining > half ) {
                    // First half: ramp 0 → 200
                    overlayAlpha = (int)(200 * (1f - (remaining - half) / half));
                } else {
                    // Second half: ramp 200 → 0
                    overlayAlpha = (int)(200 * (remaining / half));
                }
                DrawRect( 0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, overlayAlpha );

                // White flash at midpoint — fades over ~300ms into second half
                if( remaining <= half ) {
                    float timeSinceMid = half - remaining;
                    if( timeSinceMid < 300f ) {
                        int flashAlpha = (int)(180 * (1f - timeSinceMid / 300f));
                        DrawRect( 0.5f, 0.5f, 1.0f, 1.0f, 255, 255, 255, flashAlpha );
                    }
                }

                // Team-colored vignette tint
                float distFromMid = Math.Abs( remaining - half ) / half; // 1 at edges, 0 at midpoint
                int vignetteAlpha = (int)(40 * (1f - distFromMid));
                if( Team == (int)Teams.Traitor ) {
                    DrawRect( 0.5f, 0.5f, 1.0f, 1.0f, 200, 30, 30, vignetteAlpha );
                } else if( Team == (int)Teams.Detective ) {
                    DrawRect( 0.5f, 0.5f, 1.0f, 1.0f, 30, 30, 200, vignetteAlpha );
                }
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
            var body = new DeadBody( position, player, kill, weaponHash );

            // Look up team from PlayerDetails
            if( ClientGlobals.CurrentGame != null ) {
                object teamVal = ClientGlobals.CurrentGame.GetPlayerDetail( ply, "team" );
                if( teamVal != null ) {
                    int teamInt = Convert.ToInt32( teamVal );
                    switch( teamInt ) {
                        case 0: body.Team = "Innocent"; break;
                        case 1: body.Team = "Traitor"; break;
                        case 2: body.Team = "Detective"; break;
                    }
                }
            }

            DeadBodies.Add( ply, body );
        }

        public override void SetTeam( int team ) {
            base.SetTeam( team );
            switch( team ) {
                case 0:
                    HUD.TeamText.Caption = "Innocent";
                    HubNUI.ShowRoleReveal( "Innocent", "#22c55e" );
                    break;
                case 1:
                    HUD.TeamText.Caption = "Traitor";
                    HubNUI.ShowRoleReveal( "Traitor", "#ef4444" );
                    break;
                case 2:
                    HUD.TeamText.Caption = "Detective";
                    HubNUI.ShowRoleReveal( "Detective", "#3b82f6" );
                    break;
                default:
                    HUD.TeamText.Caption = "Spectator";
                    break;
            }
        }

        public void OnRoundResult( string winner, string color, string reason ) {
            HubNUI.ShowRoundEnd( winner, color, reason );
        }
    }
}
