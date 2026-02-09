using GTA_GameRooClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;

namespace BRClient {
    public class BRHUD : HUD {

        public int AliveCount = 0;
        public int ZonePhase = -1;
        public string ZoneStatus = "Waiting";

        // Orange/gold theme accent
        const int ACCENT_R = 230;
        const int ACCENT_G = 160;
        const int ACCENT_B = 30;

        public override void Start() {
            base.Start();
            DisplayRadar( true );
        }

        public override void Draw() {
            DrawGameTimer();
            DrawBRPanel();
        }

        void DrawBRPanel() {
            float panelW = 0.18f;
            float panelH = 0.065f;
            float panelX = 0.5f - panelW / 2f;
            float panelY = 0.015f;

            // Dark background
            DrawRectangle( panelX, panelY, panelW, panelH, 15, 15, 15, 200 );

            // Left accent strip
            DrawRectangle( panelX, panelY, 0.004f, panelH, ACCENT_R, ACCENT_G, ACCENT_B, 255 );

            // Top row: "BATTLE ROYALE" label
            DrawText2D( 0.5f, panelY + 0.006f, "BATTLE ROYALE", 0.28f, ACCENT_R, ACCENT_G, ACCENT_B, 255, true );

            // Bottom row: alive count + zone phase
            string aliveStr = "Alive: " + AliveCount;
            DrawText2D( 0.5f - 0.04f, panelY + 0.032f, aliveStr, 0.30f, 255, 255, 255, 255, true );

            string phaseStr = ZonePhase >= 0 ? "P" + ( ZonePhase + 1 ) : "--";
            DrawText2D( 0.5f + 0.055f, panelY + 0.032f, phaseStr, 0.28f, 200, 200, 200, 220, true );
        }

        public void DrawZoneWarning( float secondsLeft, float gracePeriod ) {
            float bannerW = 0.22f;
            float bannerH = 0.038f;
            float bannerX = 0.5f - bannerW / 2f;
            float bannerY = 0.09f;

            float urgency = 1f - Math.Max( 0, secondsLeft ) / gracePeriod;

            int bgR = (int)( 15 + urgency * 50 );
            int bgA = (int)( 190 + urgency * 55 );
            DrawRectangle( bannerX, bannerY, bannerW, bannerH, bgR, 8, 8, bgA );

            float fillPct = Math.Max( 0, secondsLeft ) / gracePeriod;
            DrawRectangle( bannerX, bannerY, fillPct * bannerW, bannerH, 200, 40, 40, 40 );

            DrawRectangle( bannerX, bannerY, 0.004f, bannerH, 200, 40, 40, 255 );

            int secDisplay = Math.Max( 0, (int)Math.Ceiling( secondsLeft / 1000 ) );
            string warnText = "OUTSIDE THE ZONE   " + secDisplay + "s";

            int textA = 255;
            if( secDisplay <= 2 ) {
                float pulse = (float)( Math.Sin( GetGameTimer() / 150.0 ) * 0.5 + 0.5 );
                textA = 180 + (int)( 75 * pulse );
            }
            DrawText2D( 0.5f, bannerY + 0.008f, warnText, 0.32f, 255, 255, 255, textA, true );
        }
    }
}
