using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace TTTClient {
    class TTTHUD : HUD {

        public bool isRadarActive = false;
        public List<Vector3> RadarPositions = new List<Vector3>();

        public float RadarTime = 0f;
        public float RadarScanTime = 30 * 1000;


        public override void Draw() {

            DrawRectangle( 0.025f, 0.878f, 0.12f, 0.093f, 122, 127, 140, 255 );;

            DrawHealth();
            //DrawScore();
            TeamText.Draw();
            
            DrawTeam();

            DrawRectangle( 0.098f, 0.87824f, 0.007f, 0.025f, 68, 74, 96, 255 );

            if( isRadarActive )
                ShowRadar();

            base.Draw();
        }


        public void DrawHealth() {

            //HideHudAndRadarThisFrame();

            HideReticle();

            HealthText.Caption = Game.Player.Character.Health.ToString();


            AmmoText.Caption = Game.PlayerPed.Weapons.Current.AmmoInClip.ToString() + " + " + latestAmmo;

            if( Game.PlayerPed.IsReloading ) {
                latestAmmo = Game.PlayerPed.Weapons.Current.Ammo - Game.PlayerPed.Weapons.Current.AmmoInClip;
            }

            DrawRectangle( 0.025f, 0.9038f, 0.12f, 0.035f, 0, 0, 0, 200 );
            float healthPercent = (float)Game.Player.Character.Health / Game.Player.Character.MaxHealth;
            if( healthPercent < 0 )
                healthPercent = 0;
            if( healthPercent > 1 )
                healthPercent = 1;
            DrawRectangle( 0.025f, 0.9038f, healthPercent * 0.12f, 0.035f, 200, 46, 48, 255 );
            DrawRectangle( 0.025f, 0.9038f, 0.007f, 0.035f, 150, 1, 3, 255 );

            float ammoPercent = (float)Game.PlayerPed.Weapons.Current.AmmoInClip / Game.PlayerPed.Weapons.Current.MaxAmmoInClip;

            DrawRectangle( 0.025f, 0.94f, 0.12f, 0.03f, 0, 0, 0, 200 );
            DrawRectangle( 0.025f, 0.94f, ammoPercent * 0.12f, 0.03f, 206, 155, 1, 200 );
            DrawRectangle( 0.025f, 0.94f, 0.007f, 0.03f, 160, 106, 0, 255 );

            HealthText.Draw();
            AmmoText.Draw();

        }

        public void DrawTeam() {
            switch( ClientGlobals.Team ) {
                case 0:
                    DrawRectangle( 0.025f, 0.8782425f, 0.073f, 0.025f, 0, 200, 0, 255 );
                    DrawRectangle( 0.025f, 0.8782425f, 0.007f, 0.025f, 0, 150, 0, 255 );
                    break;
                case 1:
                    DrawRectangle( 0.025f, 0.8782425f, 0.073f, 0.025f, 200, 0, 0, 255 );
                    DrawRectangle( 0.025f, 0.8782425f, 0.007f, 0.025f, 150, 0, 0, 255 );
                    break;
                case 2:
                    DrawRectangle( 0.025f, 0.8782425f, 0.073f, 0.025f, 0, 0, 200, 255 );
                    DrawRectangle( 0.025f, 0.8782425f, 0.007f, 0.025f, 0, 0, 150, 255 );
                    break;
                default:
                    DrawRectangle( 0.025f, 0.8782425f, 0.073f, 0.025f, 200, 200, 0, 255 );
                    DrawRectangle( 0.025f, 0.8782425f, 0.007f, 0.025f, 150, 150, 0, 255 );
                    break;
            }
        }

        public void ShowRadar() {

            if( RadarTime < GetGameTimer() ) {
                RadarTime += RadarScanTime;
                UpdateRadar();
            }

            foreach( var pos in RadarPositions ) {

                Vector3 camPos = GetGameplayCamCoords();
                float dist = GetDistanceBetweenCoords( pos.X, pos.Y, pos.Z, camPos.X, camPos.Y, camPos.Z, true );

                DrawText3D( pos, Math.Round( dist ) + "m", 0.3f, 255, 255, 255, 255, 999999 );

                float x = 0, y = 0;
                var offscreen = Get_2dCoordFrom_3dCoord( pos.X, pos.Y, pos.Z - 0.08f, ref x, ref y );
                if( !offscreen )
                    DrawRect( x, y, 0.02f, 0.02f, 0, 200, 0, 255 );

            }

            DrawText2D( 0.025f, 0.97f, "Radar update in " + (Math.Round( (RadarTime - GetGameTimer()) / 1000 )).ToString(), 0.3f, 255, 255, 255, 255, false );

        }

        public void UpdateRadar() {
            RadarPositions = new List<Vector3>();
            RadarTime = GetGameTimer() + RadarScanTime;
            foreach( var ply in ClientGlobals.GetInGamePlayers() ) {
                RadarPositions.Add( ply.Character.Position );
            }
        }

        public void SetRadarActive( bool active ) {
            if( isRadarActive )
                return;
            UpdateRadar();
            isRadarActive = active;
        }


    }
}
