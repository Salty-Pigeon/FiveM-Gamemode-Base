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
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

            Globals.GameCoins = 1;

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

            if( IsControlJustReleased( 0, (int)eControl.ControlInteractionMenu ) ) { // M 244
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


            if( IsControlJustPressed( 0, (int)eControl.ControlVehicleFlyAttackCamera ) ) { // Insert 121
                if( CanTeleport ) {
                    WriteChat( "TTT", "Position set", 200, 200, 0 );
                    SavedTeleport = Game.PlayerPed.Position;
                }
            }

            if( IsControlJustPressed( 0, (int)eControl.ControlFrontendSocialClub ) ) { // Home 212
                if( CanTeleport )
                    TeleportToSaved( 3 * 1000 );
            }

            if( IsControlJustPressed( 0, 38 ) ) { // E 38 dead body
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

            if( IsControlJustPressed( 0, 243 ) ) { // Tilde
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
