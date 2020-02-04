using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class HUD {

        public SaltyText HealthText = new SaltyText( 0.034f, 0.906f, 0, 0, 0.5f, "Health: ", 255, 255, 255, 255, false, false, 0, true );
        public SaltyText AmmoText = new SaltyText( 0.034f, 0.938f, 0, 0, 0.5f, "Ammo: ", 255, 255, 255, 255, false, false, 0, true );
        public SaltyText GameTimeText = new SaltyText( 0.125f, 0.877f, 0, 0, 0.5f, "", 255, 255, 255, 255, false, true, 0, true );
        public SaltyText BoundText = new SaltyText( 0.5f, 0.1f, 0, 0, 1, "", 255, 255, 255, 255, true, true, 0, true );
        public SaltyText GoalText = new SaltyText( 0.5f, 0.1f, 0, 0, 1f, "", 255, 255, 255, 255, false, true, 0, true );
        public SaltyText HUDText = new SaltyText( 0.5f, 0.5f, 0, 0, 0.5f, "", 255, 255, 255, 255, false, true, 0, true );
        public SaltyText ScoreText = new SaltyText( 0.5f, 0.01f, 0, 0, 0.7f, "Score: 0", 255, 255, 255, 255, false, true, 0, true );
        public SaltyText AddScoreText = new SaltyText( 0.5f + (8), 0.025f, 0, 0, 0.3f, "", 255, 255, 255, 255, false, true, 0, true );
        public SaltyText TeamText = new SaltyText( 0.063f, 0.878f, 0, 0, 0.3f, "Traitor", 255, 255, 255, 255, false, true, 0, true );

        public float lastLooked = 0;
        private float showScoreTimer = 0;
        private float showScoreLength = 500;
        public float GoalTextTime = 0;

        public float latestAmmo = 0f;


        public virtual void Start() {
            HealthText.Scale = 0.4f;
            AmmoText.Scale = 0.4f;
            GameTimeText.Scale = 0.35f;
            DisplayRadar( false );
        }



        public void DrawRectangle( float x, float y, float width, float height, int r, int g, int b, int alpha ) {
            DrawRect( x + (width / 2), y + (height / 2), width, height, r, g, b, alpha );
        }

        public virtual void Draw() {
            
        }

        public void DrawText2D( float x, float y, string text, float scale, int r, int g, int b, int a, bool centre ) {
            SetTextScale( scale, scale );
            SetTextFont( 0 );
            SetTextProportional( true );
            SetTextColour( r, g, b, a );
            SetTextDropshadow( 0, 0, 0, 0, 55 );
            SetTextEdge( 2, 0, 0, 0, 150 );
            SetTextDropShadow();
            SetTextOutline();
            SetTextEntry( "STRING" );
            SetTextCentre( centre );
            AddTextComponentString( text );
            DrawText( x, y );
        }
        public void DrawScore() {
            ScoreText.Draw();
            float time = showScoreTimer - GetGameTimer();
            if( time >= 0 ) {
                if( time <= (showScoreLength / 2) ) {
                    float percent = time / (showScoreLength / 2);
                    AddScoreText.Scale = percent * 0.3f;
                }
                else {
                    float percent = (time - (showScoreLength / 2)) / (showScoreLength / 2);
                    AddScoreText.Scale = 0.3f - (percent * 0.3f);
                }
                AddScoreText.Draw();
            }
        }

        public void DrawText3D( Vector3 pos, string text, float scale, int r, int g, int b, int a, float minDistance ) {
            float x = 0, y = 0;
            bool offScreen = Get_2dCoordFrom_3dCoord( pos.X, pos.Y, pos.Z, ref x, ref y );

            if( offScreen )
                return;

            Vector3 camPos = GetGameplayCamCoords();
            float dist = GetDistanceBetweenCoords( pos.X, pos.Y, pos.Z, camPos.X, camPos.Y, camPos.Z, true );
            if( dist > minDistance )
                return;

            SetTextScale( scale, scale );
            SetTextFont( 0 );
            SetTextProportional( true );
            SetTextColour( r, g, b, a );
            SetTextDropshadow( 0, 0, 0, 0, 55 );
            SetTextEdge( 2, 0, 0, 0, 150 );
            SetTextDropShadow();
            SetTextOutline();
            SetTextEntry( "STRING" );
            SetTextCentre( true );
            AddTextComponentString( text );
            DrawText( x, y );
        }


        public void DrawText3D( Vector3 pos, Vector3 camPos, string text, float scale, int r, int g, int b, int a, float minDistance ) {
            float x = 0, y = 0;
            bool offScreen = Get_2dCoordFrom_3dCoord( pos.X, pos.Y, pos.Z, ref x, ref y );

            if( offScreen )
                return;

            float dist = GetDistanceBetweenCoords( pos.X, pos.Y, pos.Z, camPos.X, camPos.Y, camPos.Z, true );
            if( dist > minDistance )
                return;

            SetTextScale( scale, scale );
            SetTextFont( 0 );
            SetTextProportional( true );
            SetTextColour( r, g, b, a );
            SetTextDropshadow( 0, 0, 0, 0, 55 );
            SetTextEdge( 2, 0, 0, 0, 150 );
            SetTextDropShadow();
            SetTextOutline();
            SetTextEntry( "STRING" );
            SetTextCentre( true );
            AddTextComponentString( text );
            DrawText( x, y );
        }

        public void SetGoal( string caption, int r, int g, int b, int a, int duration ) {
            GoalText.Caption = caption;
            GoalText.Colour = System.Drawing.Color.FromArgb( a, r, g, b );
            GoalTextTime = GetGameTimer() + (duration * 1000);
        }

        public void SetGameTimePosition( int x, int y, bool centre ) {
            GameTimeText.Position = new Vector2( x, y );
            GameTimeText.Centre = centre;
        }

        public virtual void ShowNames() {
            Vector3 position = Game.PlayerPed.ForwardVector;

            RaycastResult result = Raycast( Game.PlayerPed.Position, position, 75, IntersectOptions.Peds1, null );
            if( result.DitHitEntity ) {
                if( result.HitEntity != Game.PlayerPed ) {
                    int ent = NetworkGetEntityFromNetworkId( result.HitEntity.NetworkId );
                    if( IsPedAPlayer( ent ) ) {
                        HUDText.Caption = GetPlayerName( GetPlayerPed( ent ) ).ToString();
                        lastLooked = GetGameTimer();
                    }

                }

            }
        }

        public static RaycastResult Raycast( Vector3 source, Vector3 direction, float maxDistance, IntersectOptions options, Entity ignoreEntity = null ) {
            Vector3 target = source + direction * maxDistance;

            return new RaycastResult( Function.Call<int>( Hash._CAST_RAY_POINT_TO_POINT, source.X, source.Y, source.Z, target.X, target.Y, target.Z, options, ignoreEntity == null ? 0 : ignoreEntity.Handle, 7 ) );
        }

        public virtual void ShowTalking() {
            if( IsControlPressed( 0, 249 ) ) {
                int offset = 0;

                /*
                if( ActiveGame.Team == 0 ) {
                    foreach( var ply in new PlayerList() ) {
                        if( !NetworkIsPlayerTalking( ply.Handle ) ) { continue; }
                        if( ActiveGame.GetTeam( ply.Handle ) == 0 ) {
                            DrawRectangle( 0.85f, 0.05f + (offset * 0.052f), 0.13f, 0.05f, 230, 230, 0, 255 );
                            DrawText2D( 0.915f, 0.061f + (offset * 0.052f), ply.Name, 0.5f, 255, 255, 255, 255, true );
                            offset++;
                        }
                    }
                }
                */
            }
        }

        public void DrawGameTimer() {

            TimeSpan time = TimeSpan.FromMilliseconds( ClientGlobals.CurrentGame.GameTimerEnd - GetGameTimer() );

            GameTimeText.Caption = string.Format( "{0:00}:{1:00}", Math.Ceiling( time.TotalMinutes - 1 ), time.Seconds );

            GameTimeText.Draw();
        }

        public void DrawSpriteOrigin( Vector3 pos, string texture, float width, float height, float rotation, bool throughWalls ) {
            SetDrawOrigin( pos.X, pos.Y, pos.Z, 0 );
            SetSeethrough( false );
            DrawSprite( "saltyTextures", texture, 0, 0, width, height, rotation, 255, 255, 255, 255 );
            ClearDrawOrigin();
        }

        public void DrawGoal() {
            if( GoalTextTime > GetGameTimer() ) {
                GoalText.Draw();
            }
        }

        public void HideReticle() {
            Weapon w = Game.PlayerPed?.Weapons?.Current;

            if( w != null ) {
                WeaponHash wHash = w.Hash;
                if( wHash.ToString() != "SniperRifle" ) {
                    HideHudComponentThisFrame( 14 );
                }
            }
        }

    }
}
