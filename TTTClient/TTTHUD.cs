using GTA_GameRooClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooShared;
using CitizenFX.Core.Native;

namespace TTTClient {
    class TTTHUD : HUD {

        public bool isRadarActive = false;
        public List<Vector3> RadarPositions = new List<Vector3>();

        public int DetectiveTracing = -1;

        public float RadarTime = 0f;
        public float RadarScanTime = 30 * 1000;
        public float RadarLastScanAt = 0f;

        public float DNATime = 0f;
        public float DNAScanTime = 10 * 1000;
        public Vector3 DNALastPos;
        public float DNALastScanAt = 0f;

        private bool nuiTimerHidden = false;

        public override void Draw() {
            if( !nuiTimerHidden ) {
                HideGameTimer();
                nuiTimerHidden = true;
            }

            HideReticle();
            DrawPanel();
            ShowNames();
            DrawGoal();

            // Ability bars — stack dynamically above the main panel
            float nextBarY = 0.922f;
            float barStep = 0.025f;

            if( isRadarActive ) {
                nextBarY -= barStep;
                ShowRadar( nextBarY );
            }
            if( DetectiveTracing != -1 ) {
                nextBarY -= barStep;
                ShowDNA( nextBarY );
            }
            if( Main.CanTeleport ) {
                nextBarY -= barStep;
                ShowTeleport( nextBarY );
            }
            if( Main.CanDisguise ) {
                nextBarY -= barStep;
                ShowDisguise( nextBarY );
            }

            ShowTalking();

            if( lastLooked + 300 > GetGameTimer() ) {
                HUDText.Draw();
            }
        }

        private void DrawPanel() {
            // Layout
            float px = 0.015f;
            float py = 0.922f;
            float pw = 0.155f;
            float accentW = 0.004f;
            float topH = 0.025f;
            float sep = 0.002f;
            float botH = 0.027f;
            float ph = topH + sep + botH;

            float innerX = px + accentW;
            float innerW = pw - accentW;
            float botY = py + topH + sep;

            // Team color
            int tcR, tcG, tcB;
            GetTeamColor( out tcR, out tcG, out tcB );

            // Accent strip — team color, full height
            DrawRectangle( px, py, accentW, ph, tcR, tcG, tcB, 255 );

            // Top row — role + timer
            DrawRectangle( innerX, py, innerW, topH, 22, 24, 35, 215 );

            string role = GetRoleName();
            DrawText2D( innerX + 0.006f, py + 0.004f, role, 0.28f, tcR, tcG, tcB, 255, false );

            // Timer
            float msLeft = 0;
            if( ClientGlobals.CurrentGame != null )
                msLeft = ClientGlobals.CurrentGame.GameTimerEnd - GetGameTimer();
            if( msLeft < 0 ) msLeft = 0;
            int totalSec = (int)Math.Ceiling( msLeft / 1000 );
            int mins = totalSec / 60;
            int secs = totalSec % 60;
            string timer = mins + ":" + secs.ToString( "00" );
            bool urgent = totalSec <= 30 && totalSec > 0;

            if( urgent ) {
                float pulse = (float)( Math.Sin( GetGameTimer() / 300.0 ) * 0.5 + 0.5 );
                int tA = 180 + (int)( 75 * pulse );
                DrawText2D( px + pw - 0.042f, py + 0.004f, timer, 0.28f, 239, 68, 68, tA, false );
            } else {
                DrawText2D( px + pw - 0.042f, py + 0.004f, timer, 0.28f, 200, 200, 200, 180, false );
            }

            // Separator
            DrawRectangle( innerX, py + topH, innerW, sep, 35, 38, 52, 120 );

            // Health bar — background
            DrawRectangle( innerX, botY, innerW, botH, 12, 12, 20, 225 );

            // Health bar — fill
            float health = (float)Game.Player.Character.Health;
            float maxHealth = (float)Game.Player.Character.MaxHealth;
            float pct = maxHealth > 0 ? health / maxHealth : 0;
            pct = Math.Max( 0, Math.Min( 1, pct ) );

            if( pct > 0 ) {
                DrawRectangle( innerX, botY, pct * innerW, botH, 200, 46, 48, 220 );
                DrawRectangle( innerX, botY, 0.003f, botH, 230, 60, 62, 255 );
            }

            // Low health warning pulse
            if( pct > 0 && pct < 0.25f ) {
                float pulse = (float)( Math.Sin( GetGameTimer() / 200.0 ) * 0.5 + 0.5 );
                DrawRectangle( innerX, botY, innerW, botH, 255, 0, 0, (int)( 40 * pulse ) );
            }

            // Health number
            string healthStr = Math.Max( 0, (int)health ).ToString();
            DrawText2D( innerX + 0.006f, botY + 0.005f, healthStr, 0.28f, 255, 255, 255, 240, false );
        }

        private void GetTeamColor( out int r, out int g, out int b ) {
            switch( Main.Team ) {
                case 0:  r = 34;  g = 197; b = 94;  return; // Innocent
                case 1:  r = 239; g = 68;  b = 68;  return; // Traitor
                case 2:  r = 59;  g = 130; b = 246; return; // Detective
                default: r = 200; g = 200; b = 60;  return;
            }
        }

        private string GetRoleName() {
            switch( Main.Team ) {
                case 0:  return "INNOCENT";
                case 1:  return "TRAITOR";
                case 2:  return "DETECTIVE";
                default: return "UNKNOWN";
            }
        }

        private void DrawAbilityBar( float barY, string label, float progress, int acR, int acG, int acB, int textAlpha ) {
            float px = 0.015f;
            float pw = 0.155f;
            float accentW = 0.004f;
            float barH = 0.022f;
            float innerX = px + accentW;
            float innerW = pw - accentW;

            // Background
            DrawRectangle( innerX, barY, innerW, barH, 22, 24, 35, 200 );

            // Progress fill
            if( progress > 0 )
                DrawRectangle( innerX, barY, Math.Min( progress, 1f ) * innerW, barH, acR, acG, acB, 50 );

            // Accent strip
            DrawRectangle( px, barY, accentW, barH, acR, acG, acB, 255 );

            // Label
            DrawText2D( innerX + 0.006f, barY + 0.003f, label, 0.25f, 255, 255, 255, textAlpha, false );
        }

        public void ShowRadar( float barY ) {

            if( RadarTime < GetGameTimer() ) {
                RadarTime += RadarScanTime;
                RadarLastScanAt = GetGameTimer();
                UpdateRadar();
            }

            float elapsed = GetGameTimer() - RadarLastScanAt;
            float remaining = RadarTime - GetGameTimer();
            float freshness = 1f - Math.Min( elapsed / RadarScanTime, 1f );

            // Scan flash — large burst that fades over 400ms after each scan
            if( elapsed < 400f ) {
                float flashAlpha = (1f - elapsed / 400f);
                foreach( var pos in RadarPositions ) {
                    float fx = 0, fy = 0;
                    var off = Get_2dCoordFrom_3dCoord( pos.X, pos.Y, pos.Z - 0.08f, ref fx, ref fy );
                    if( !off )
                        DrawRect( fx, fy, 0.040f, 0.024f, 34, 197, 94, (int)(flashAlpha * 100) );
                }
            }

            foreach( var pos in RadarPositions ) {

                Vector3 camPos = GetGameplayCamCoords();
                float dist = GetDistanceBetweenCoords( pos.X, pos.Y, pos.Z, camPos.X, camPos.Y, camPos.Z, true );

                float x = 0, y = 0;
                var offscreen = Get_2dCoordFrom_3dCoord( pos.X, pos.Y, pos.Z - 0.08f, ref x, ref y );
                if( offscreen )
                    continue;

                // Outer glow — pulsing alpha based on freshness
                int glowAlpha = 20 + (int)(freshness * 60);
                DrawRect( x, y, 0.020f, 0.012f, 34, 197, 94, glowAlpha );

                // Inner dot — always bright
                DrawRect( x, y, 0.007f, 0.004f, 34, 197, 94, 220 );

                // Crosshair lines
                DrawRect( x, y, 0.028f, 0.0015f, 34, 197, 94, 60 );
                DrawRect( x, y, 0.0015f, 0.024f, 34, 197, 94, 60 );

                // Distance label background pill
                float labelY = y + 0.018f;
                DrawRect( x, labelY, 0.038f, 0.016f, 0, 0, 0, 160 );

                // Distance text
                DrawText2D( x - 0.013f, labelY - 0.007f, Math.Round( dist ) + "m", 0.28f, 255, 255, 255, 220, false );
            }

            float progress = Math.Max( remaining / RadarScanTime, 0f );
            float secs = (float)Math.Ceiling( remaining / 1000 );
            DrawAbilityBar( barY, "RADAR  " + secs + "s", progress, 34, 197, 94, 220 );

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

        public virtual void ShowNames() {
            ShowDeadName();
            Vector3 camPos = GetGameplayCamCoords();
            Vector3 camRot = GetGameplayCamRot( 2 );
            Vector3 camDir = RotationToDirection( camRot );
            RaycastResult result = Raycast( camPos, camDir, 75, IntersectOptions.Peds1, Game.PlayerPed );
            if( result.DitHitEntity ) {
                if( result.HitEntity != Game.PlayerPed ) {
                    int ent = result.HitEntity.Handle;

                    // Check if it's a test bot first
                    TestBot bot = Main.TestBots.FirstOrDefault( b => b.Handle == ent );
                    if( bot != null ) {
                        // Check if bot is disguised
                        if( bot.IsDisguised ) {
                            return; // Don't show name if disguised
                        }
                        HUDText.Caption = bot.Name + " [Innocent]";
                        lastLooked = GetGameTimer();
                        return;
                    }

                    Debug.WriteLine( NetworkGetPlayerIndex( result.HitEntity.Handle ) + " > " + Game.Player.ServerId + " > " + NetworkGetPlayerIndexFromPed( result.HitEntity.NetworkId ).ToString() );
                    object val = (ClientGlobals.CurrentGame).GetPlayerDetail( result.HitEntity.NetworkId, "disguised" );
                    if( val != null && Convert.ToBoolean( val ) ) {
                        return;
                    }
                    if( IsPedAPlayer( ent ) ) {
                        HUDText.Caption = GetPlayerName( GetPlayerPed( ent ) ).ToString();
                        lastLooked = GetGameTimer();
                    }

                }

            }
        }

        public static Vector3 RotationToDirection( Vector3 rotation ) {
            float radX = rotation.X * (float)Math.PI / 180f;
            float radZ = rotation.Z * (float)Math.PI / 180f;
            float absX = (float)Math.Abs( Math.Cos( radX ) );
            return new Vector3( (float)(-Math.Sin( radZ )) * absX, (float)(Math.Cos( radZ )) * absX, (float)Math.Sin( radX ) );
        }

        public static RaycastResult Raycast( Vector3 source, Vector3 direction, float maxDistance, IntersectOptions options, Entity ignoreEntity = null ) {
            Vector3 target = source + direction * maxDistance;

            return new RaycastResult( Function.Call<int>( Hash._CAST_RAY_POINT_TO_POINT, source.X, source.Y, source.Z, target.X, target.Y, target.Z, options, ignoreEntity == null ? 0 : ignoreEntity.Handle, 7 ) );
        }

        public virtual void ShowTalking() {
            if( IsControlPressed( 0, 249 ) ) {
                int offset = 0;
                foreach( var ply in new PlayerList() ) {
                    if( !NetworkIsPlayerTalking( ply.Handle ) ) { continue; }
                    DrawRectangle( 0.9f, 0.06f + (offset * 0.052f), 0.07f, 0.04f, 20, 200, 20, 255 );
                    DrawText2D( 0.934f, 0.067f + (offset * 0.052f), Game.Player.Name, 0.3f, 255, 255, 255, 255, true );
                    offset++;
                }
            }
        }

        public void ShowDeadName() {
            foreach( var body in Main.DeadBodies ) {
                Vector3 Position = GetPedBoneCoords( body.Value.ID, (int)Bone.SKEL_ROOT, 0, 0, 0 );

                if( !body.Value.isDiscovered )
                    DrawText3D( Position, Game.PlayerPed.Position, body.Value.Caption, 0.3f, 255, 255, 0, 255, 2 );
                else
                    DrawText3D( Position, Game.PlayerPed.Position, body.Value.Caption, 0.3f, 255, 255, 255, 255, 2 );

                if( Main.Team == -1 ) {
                    DrawText3D( Position - new Vector3( 0, 0, 0.2f ), Game.PlayerPed.Position, "Press E to scan for DNA", 0.3f, 230, 230, 0, 255, 2 );
                }
            }
        }

        public void ShowDNA( float barY ) {

            if( DNATime < GetGameTimer() ) {
                DNATime += DNAScanTime;
                DNALastScanAt = GetGameTimer();
                UpdateDNA();
            }

            float elapsed = GetGameTimer() - DNALastScanAt;
            float remaining = DNATime - GetGameTimer();
            float freshness = 1f - Math.Min( elapsed / DNAScanTime, 1f );

            if( DNALastPos != Vector3.Zero ) {
                Vector3 camPos = GetGameplayCamCoords();
                float dist = GetDistanceBetweenCoords( DNALastPos.X, DNALastPos.Y, DNALastPos.Z, camPos.X, camPos.Y, camPos.Z, true );

                float x = 0, y = 0;
                var offscreen = Get_2dCoordFrom_3dCoord( DNALastPos.X, DNALastPos.Y, DNALastPos.Z - 0.08f, ref x, ref y );
                if( !offscreen ) {

                    // Scan flash — burst that fades over 400ms
                    if( elapsed < 400f ) {
                        float flashAlpha = (1f - elapsed / 400f);
                        DrawRect( x, y, 0.040f, 0.024f, 59, 130, 246, (int)(flashAlpha * 100) );
                    }

                    // Outer glow — pulsing alpha
                    int glowAlpha = 20 + (int)(freshness * 60);
                    DrawRect( x, y, 0.020f, 0.012f, 59, 130, 246, glowAlpha );

                    // Inner dot — always bright
                    DrawRect( x, y, 0.007f, 0.004f, 59, 130, 246, 220 );

                    // Distance label background pill
                    float labelY = y + 0.018f;
                    DrawRect( x, labelY, 0.038f, 0.016f, 0, 0, 0, 160 );

                    // Distance text
                    DrawText2D( x - 0.013f, labelY - 0.007f, Math.Round( dist, 1 ) + "m", 0.28f, 255, 255, 255, 220, false );
                }
            }

            float progress = Math.Max( remaining / DNAScanTime, 0f );
            float secs = (float)Math.Ceiling( remaining / 1000 );
            DrawAbilityBar( barY, "DNA TRACE  " + secs + "s", progress, 59, 130, 246, 220 );

        }

        public void UpdateDNA() {
            DNATime = GetGameTimer() + DNAScanTime;
            Vector3 newCoord = GetEntityCoords( DetectiveTracing, true );
            if( newCoord != Vector3.Zero ) {
                DNALastPos = newCoord;
            }
        }

        public void ShowTeleport( float barY ) {
            bool isTraitor = Main.Team == (int)Teams.Traitor;
            int tcR, tcG, tcB;
            if( isTraitor ) { tcR = 239; tcG = 68; tcB = 68; }
            else { tcR = 59; tcG = 130; tcB = 246; }

            float gameTime = GetGameTimer();

            // 3D destination marker
            if( Main.SavedTeleport != Vector3.Zero ) {
                float sx = 0, sy = 0;
                var offscreen = Get_2dCoordFrom_3dCoord( Main.SavedTeleport.X, Main.SavedTeleport.Y, Main.SavedTeleport.Z, ref sx, ref sy );
                if( !offscreen ) {
                    float pulse = (float)Math.Sin( gameTime / 400.0 );
                    int glowAlpha = 40 + (int)(20 * pulse);
                    DrawRect( sx, sy, 0.016f, 0.010f, tcR, tcG, tcB, glowAlpha );
                    DrawRect( sx, sy, 0.006f, 0.0035f, tcR, tcG, tcB, 180 );

                    Vector3 camPos = GetGameplayCamCoords();
                    float dist = GetDistanceBetweenCoords( Main.SavedTeleport.X, Main.SavedTeleport.Y, Main.SavedTeleport.Z, camPos.X, camPos.Y, camPos.Z, true );
                    float labelY = sy + 0.018f;
                    DrawRect( sx, labelY, 0.038f, 0.016f, 0, 0, 0, 160 );
                    DrawText2D( sx - 0.013f, labelY - 0.007f, Math.Round( dist ) + "m", 0.28f, 255, 255, 255, 220, false );
                }
            }

            // Status bar
            if( Main.isTeleporting ) {
                float remaining = Main.teleportTime - gameTime;
                float progress = 1f - Math.Max( remaining / Main.teleportLength, 0f );
                DrawAbilityBar( barY, "TELEPORT", progress, tcR, tcG, tcB, 220 );
            } else if( Main.teleportWait > gameTime ) {
                float remaining = Main.teleportWait - gameTime;
                float progress = Math.Max( remaining / Main.teleportDelay, 0f );
                float secs = (float)Math.Ceiling( remaining / 1000 );
                DrawAbilityBar( barY, "COOLDOWN  " + secs + "s", progress, tcR, tcG, tcB, 220 );
            } else if( Main.teleportStatus != "" && gameTime - Main.teleportStatusTime < 2000f ) {
                float elapsed = gameTime - Main.teleportStatusTime;
                int textAlpha = (int)( 220 * ( 1f - elapsed / 2000f ) );
                DrawAbilityBar( barY, Main.teleportStatus, 0, tcR, tcG, tcB, textAlpha );
            } else {
                DrawAbilityBar( barY, "TELEPORT", 0, tcR, tcG, tcB, 120 );
            }
        }

        public void ShowDisguise( float barY ) {
            bool isTraitor = Main.Team == (int)Teams.Traitor;
            int tcR, tcG, tcB;
            if( isTraitor ) { tcR = 239; tcG = 68; tcB = 68; }
            else { tcR = 59; tcG = 130; tcB = 246; }

            float gameTime = GetGameTimer();

            if( Main.isDisguised ) {
                DrawAbilityBar( barY, "DISGUISE  ON", 1f, tcR, tcG, tcB, 220 );
            } else {
                DrawAbilityBar( barY, "DISGUISE  OFF", 0, tcR, tcG, tcB, 120 );
            }

            // Toggle flash overlay
            bool recentToggle = Main.disguiseStatus != "" && gameTime - Main.disguiseStatusTime < 2000f;
            if( recentToggle ) {
                float elapsed = gameTime - Main.disguiseStatusTime;
                int flashAlpha = (int)( 40 * ( 1f - elapsed / 2000f ) );
                DrawRectangle( 0.019f, barY, 0.151f, 0.022f, tcR, tcG, tcB, flashAlpha );
            }
        }

    }
}
