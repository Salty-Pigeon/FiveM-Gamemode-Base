using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;
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

        public override void Draw() {

            DrawRectangle( 0.025f, 0.878f, 0.12f, 0.093f, 122, 127, 140, 255 );;

            DrawHealth();
            ShowNames();
            TeamText.Draw();
            
            DrawTeam();
            DrawGoal();
            DrawRectangle( 0.098f, 0.87824f, 0.007f, 0.025f, 68, 74, 96, 255 );

            if( isRadarActive )
                ShowRadar();

            if( DetectiveTracing != -1 )
                ShowDNA();

            if( Main.CanTeleport )
                ShowTeleport();

            if( Main.CanDisguise )
                ShowDisguise();

            ShowTalking();

            if( lastLooked + 300 > GetGameTimer() ) {
                HUDText.Draw();
            }

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
            switch( Main.Team ) {
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

            // Status bar — bottom-left above HUD bars
            float barX = 0.025f;
            float barY = 0.835f;
            float barW = 0.12f;
            float barH = 0.022f;

            // Dark background
            DrawRectangle( barX, barY, barW, barH, 0, 0, 0, 180 );

            // Progress fill — depletes as timer counts down
            float progress = Math.Max( remaining / RadarScanTime, 0f );
            DrawRectangle( barX, barY, progress * barW, barH, 34, 197, 94, 80 );

            // Accent edge
            DrawRectangle( barX, barY, 0.004f, barH, 34, 197, 94, 200 );

            // Label + countdown
            float secs = (float)Math.Ceiling( remaining / 1000 );
            DrawText2D( barX + 0.007f, barY + 0.002f, "RADAR  " + secs + "s", 0.27f, 255, 255, 255, 220, false );

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

        public void ShowDNA() {

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

            // Status bar — positioned below radar bar if both active, otherwise same spot
            float barX = 0.025f;
            float barY = isRadarActive ? 0.860f : 0.835f;
            float barW = 0.12f;
            float barH = 0.022f;

            // Dark background
            DrawRectangle( barX, barY, barW, barH, 0, 0, 0, 180 );

            // Progress fill
            float progress = Math.Max( remaining / DNAScanTime, 0f );
            DrawRectangle( barX, barY, progress * barW, barH, 59, 130, 246, 80 );

            // Accent edge
            DrawRectangle( barX, barY, 0.004f, barH, 59, 130, 246, 200 );

            // Label + countdown
            float secs = (float)Math.Ceiling( remaining / 1000 );
            DrawText2D( barX + 0.007f, barY + 0.002f, "DNA TRACE  " + secs + "s", 0.27f, 255, 255, 255, 220, false );

        }

        public void UpdateDNA() {
            DNATime = GetGameTimer() + DNAScanTime;
            Vector3 newCoord = GetEntityCoords( DetectiveTracing, true );
            if( newCoord != Vector3.Zero ) {
                DNALastPos = newCoord;
            }
        }

        public void ShowTeleport() {
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
                    // Outer glow — pulsing alpha via sine wave
                    float pulse = (float)Math.Sin( gameTime / 400.0 );
                    int glowAlpha = 40 + (int)(20 * pulse); // 20–60
                    DrawRect( sx, sy, 0.016f, 0.010f, tcR, tcG, tcB, glowAlpha );

                    // Inner dot
                    DrawRect( sx, sy, 0.006f, 0.0035f, tcR, tcG, tcB, 180 );

                    // Distance label
                    Vector3 camPos = GetGameplayCamCoords();
                    float dist = GetDistanceBetweenCoords( Main.SavedTeleport.X, Main.SavedTeleport.Y, Main.SavedTeleport.Z, camPos.X, camPos.Y, camPos.Z, true );
                    float labelY = sy + 0.018f;
                    DrawRect( sx, labelY, 0.038f, 0.016f, 0, 0, 0, 160 );
                    DrawText2D( sx - 0.013f, labelY - 0.007f, Math.Round( dist ) + "m", 0.28f, 255, 255, 255, 220, false );
                }
            }

            // Status bar — stacks above radar and DNA bars
            float barX = 0.025f;
            float barY = 0.835f;
            if( isRadarActive ) barY -= 0.025f;
            if( DetectiveTracing != -1 ) barY -= 0.025f;
            float barW = 0.12f;
            float barH = 0.022f;

            // Dark background
            DrawRectangle( barX, barY, barW, barH, 0, 0, 0, 180 );

            // Accent edge
            DrawRectangle( barX, barY, 0.004f, barH, tcR, tcG, tcB, 200 );

            if( Main.isTeleporting ) {
                // Teleporting — progress bar fills over the duration
                float remaining = Main.teleportTime - gameTime;
                float progress = 1f - Math.Max( remaining / Main.teleportLength, 0f );
                DrawRectangle( barX, barY, progress * barW, barH, tcR, tcG, tcB, 80 );
                DrawText2D( barX + 0.007f, barY + 0.002f, "TELEPORT", 0.27f, 255, 255, 255, 220, false );
            } else if( Main.teleportWait > gameTime ) {
                // Cooldown — depleting bar
                float remaining = Main.teleportWait - gameTime;
                float progress = Math.Max( remaining / Main.teleportDelay, 0f );
                DrawRectangle( barX, barY, progress * barW, barH, tcR, tcG, tcB, 80 );
                float secs = (float)Math.Ceiling( remaining / 1000 );
                DrawText2D( barX + 0.007f, barY + 0.002f, "COOLDOWN  " + secs + "s", 0.27f, 255, 255, 255, 220, false );
            } else if( Main.teleportStatus != "" && gameTime - Main.teleportStatusTime < 2000f ) {
                // Status text — fades out over 2s
                float elapsed = gameTime - Main.teleportStatusTime;
                int textAlpha = (int)(220 * (1f - elapsed / 2000f));
                int bgAlpha = (int)(80 * (1f - elapsed / 2000f));
                DrawRectangle( barX, barY, barW, barH, tcR, tcG, tcB, bgAlpha );
                DrawText2D( barX + 0.007f, barY + 0.002f, Main.teleportStatus, 0.27f, 255, 255, 255, textAlpha, false );
            } else {
                // Idle — just show "TELEPORT" label
                DrawText2D( barX + 0.007f, barY + 0.002f, "TELEPORT", 0.27f, 255, 255, 255, 120, false );
            }
        }

        public void ShowDisguise() {
            bool isTraitor = Main.Team == (int)Teams.Traitor;
            int tcR, tcG, tcB;
            if( isTraitor ) { tcR = 239; tcG = 68; tcB = 68; }
            else { tcR = 59; tcG = 130; tcB = 246; }

            float gameTime = GetGameTimer();

            // Status bar — stacks above radar, DNA, and teleport bars
            float barX = 0.025f;
            float barY = 0.835f;
            if( isRadarActive ) barY -= 0.025f;
            if( DetectiveTracing != -1 ) barY -= 0.025f;
            if( Main.CanTeleport ) barY -= 0.025f;
            float barW = 0.12f;
            float barH = 0.022f;

            // Dark background
            DrawRectangle( barX, barY, barW, barH, 0, 0, 0, 180 );

            // Accent edge
            DrawRectangle( barX, barY, 0.004f, barH, tcR, tcG, tcB, 200 );

            // Toggle flash — brief highlight when status changes (within 2000ms)
            bool recentToggle = Main.disguiseStatus != "" && gameTime - Main.disguiseStatusTime < 2000f;

            if( Main.isDisguised ) {
                // Active — team-colored fill across the bar
                DrawRectangle( barX, barY, barW, barH, tcR, tcG, tcB, 80 );

                if( recentToggle ) {
                    float elapsed = gameTime - Main.disguiseStatusTime;
                    int flashAlpha = (int)(60 * (1f - elapsed / 2000f));
                    DrawRectangle( barX, barY, barW, barH, tcR, tcG, tcB, flashAlpha );
                }

                DrawText2D( barX + 0.007f, barY + 0.002f, "DISGUISE  ON", 0.27f, 255, 255, 255, 220, false );
            } else {
                // Inactive — dimmed label

                if( recentToggle ) {
                    float elapsed = gameTime - Main.disguiseStatusTime;
                    int flashAlpha = (int)(60 * (1f - elapsed / 2000f));
                    DrawRectangle( barX, barY, barW, barH, tcR, tcG, tcB, flashAlpha );
                }

                DrawText2D( barX + 0.007f, barY + 0.002f, "DISGUISE  OFF", 0.27f, 255, 255, 255, 120, false );
            }
        }

    }
}
