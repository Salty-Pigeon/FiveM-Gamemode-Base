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

    public class Main : BaseGamemode
    {

        TraitorMenu BuyMenu;

        public bool CanTeleport = false;
        public Vector3 SavedTeleport;
        float teleportLength;
        float teleportTime = 0;
        bool hasTeleported = false;
        bool isTeleporting = false;
        float teleportWait = 0;
        float teleportDelay = 5 * 1000;

        public Main() : base( "TTT" ) {
            HUD = new TTTHUD();
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

        }

        public override void Controls() {
            base.Controls();

            if( IsControlJustReleased( 0, (int)eControl.ControlInteractionMenu ) ) { // M 244
                int Team = ClientGlobals.Team;
                if( Team == (int)Teams.Traitor ) {
                    BuyMenu = new TraitorMenu( "Traitor Menu", "Traitor Buy Menu" );
                }
            }


            if( IsControlJustPressed( 0, (int)eControl.ControlVehicleFlyAttackCamera ) ) { // Insert 121
                if( CanTeleport ) {
                    ClientGlobals.WriteChat( "TTT", "Position set", 200, 200, 0 );
                    SavedTeleport = Game.PlayerPed.Position;
                }
            }

            if( IsControlJustPressed( 0, (int)eControl.ControlFrontendSocialClub ) ) { // Home 212
                if( CanTeleport )
                    TeleportToSaved( 3 * 1000 );
            }

        }

        public void TeleportToSaved( float time ) {
            if( isTeleporting ) {
                return;
            }
            if( teleportWait > GetGameTimer() ) {
                TimeSpan timer = TimeSpan.FromMilliseconds( teleportWait - GetGameTimer() );
                ClientGlobals.WriteChat( "TTT", "Cooldown " + timer.Seconds + "s", 200, 20, 20 );
                return;
            }
            if( Game.PlayerPed.IsShooting || Game.PlayerPed.IsJumping || Game.PlayerPed.IsInAir || Game.PlayerPed.IsReloading || Game.PlayerPed.IsClimbing || Game.PlayerPed.IsGoingIntoCover || Game.PlayerPed.IsRagdoll || Game.PlayerPed.IsGettingUp )
                return;

            if( SavedTeleport == Vector3.Zero ) {
                ClientGlobals.WriteChat( "TTT", "No destination set", 0, 0, 200 );
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

        public override void SetTeam( int team ) {
            base.SetTeam( team );
            switch( team ) {
                case 0:
                    HUD.TeamText.Caption = "Innocent";
                    break;
                case 1:
                    HUD.TeamText.Caption = "Traitor";
                    break;
                case 2:
                    HUD.TeamText.Caption = "Detective";
                    break;
                default:
                    HUD.TeamText.Caption = "Spectator";
                    break;
            }
        }
    }
}
