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
            DrawInventoryBar();
            ShowContainerPrompt();
        }

        void ShowContainerPrompt() {
            // Debug overlay
            if( Main.ContainerDebugActive && Main.ContainerDebugText.Length > 0 ) {
                string[] lines = Main.ContainerDebugText.Split( '\n' );
                for( int i = 0; i < lines.Length; i++ ) {
                    DrawText2D( 0.01f, 0.45f + i * 0.022f, lines[i], 0.28f, 30, 200, 30, 220, false );
                }
            }

            if( !Main.NearContainerFound ) return;

            // 3D floating prompt above the container
            Vector3 promptPos = Main.NearContainerPos + new Vector3( 0, 0, 1.0f );
            string label = Main.NearContainerIsVehicle ? "Search boot [E]" : "Search [E]";

            if( Main.IsSearchingContainer ) {
                DrawText3D( promptPos, "Searching...", 0.35f, ACCENT_R, ACCENT_G, ACCENT_B, 255, 8f );

                // Progress bar (2D, center screen)
                float barW = 0.14f;
                float barH = 0.016f;
                float barX = 0.5f - barW / 2f;
                float barY = 0.82f;

                // Background
                DrawRectangle( barX, barY, barW, barH, 15, 15, 15, 200 );
                // Fill
                DrawRectangle( barX, barY, barW * Main.SearchProgress, barH, ACCENT_R, ACCENT_G, ACCENT_B, 230 );
                // Border accent
                DrawRectangle( barX, barY, 0.003f, barH, 255, 255, 255, 180 );

                string pct = ((int)( Main.SearchProgress * 100 )) + "%";
                DrawText2D( 0.5f, barY - 0.001f, pct, 0.26f, 255, 255, 255, 255, true );
            } else {
                DrawText3D( promptPos, label, 0.35f, 255, 255, 255, 255, 8f );
            }
        }

        void DrawInventoryBar() {
            float slotW = 0.065f;
            float slotH = 0.065f;
            float gap = 0.005f;
            float totalW = slotW * 3 + gap * 2;
            float startX = 0.5f - totalW / 2f;
            float startY = 0.88f;

            for( int i = 0; i < 3; i++ ) {
                float x = startX + i * ( slotW + gap );
                bool isActive = i == Main.InventoryActiveSlot;
                uint hash = Main.InventorySlots[i];
                bool isEmpty = hash == 0;

                // Background
                int bgR = isActive ? 30 : 15;
                int bgG = isActive ? 25 : 15;
                int bgB = isActive ? 20 : 15;
                int bgA = isActive ? 220 : 190;
                DrawRectangle( x, startY, slotW, slotH, bgR, bgG, bgB, bgA );

                // Border - orange for active, dark for inactive
                if( isActive ) {
                    // Top
                    DrawRectangle( x, startY, slotW, 0.003f, ACCENT_R, ACCENT_G, ACCENT_B, 255 );
                    // Bottom
                    DrawRectangle( x, startY + slotH - 0.003f, slotW, 0.003f, ACCENT_R, ACCENT_G, ACCENT_B, 255 );
                    // Left
                    DrawRectangle( x, startY, 0.002f, slotH, ACCENT_R, ACCENT_G, ACCENT_B, 255 );
                    // Right
                    DrawRectangle( x + slotW - 0.002f, startY, 0.002f, slotH, ACCENT_R, ACCENT_G, ACCENT_B, 255 );
                }

                // Slot number
                float centerX = x + slotW / 2f;
                string slotNum = ( i + 1 ).ToString();
                DrawText2D( x + 0.005f, startY + 0.003f, slotNum, 0.22f,
                    isActive ? ACCENT_R : 150, isActive ? ACCENT_G : 150, isActive ? ACCENT_B : 150,
                    isActive ? 255 : 180, false );

                if( isEmpty ) {
                    DrawText2D( centerX, startY + 0.022f, "Empty", 0.22f, 100, 100, 100, 140, true );
                } else {
                    // Weapon name
                    string name = "";
                    if( GTA_GameRooShared.Globals.Weapons.ContainsKey( hash ) ) {
                        name = GTA_GameRooShared.Globals.Weapons[hash]["Name"];
                    }
                    // Truncate long names
                    if( name.Length > 10 ) name = name.Substring( 0, 9 ) + ".";
                    int nameA = isActive ? 255 : 200;
                    DrawText2D( centerX, startY + 0.018f, name, 0.22f, 255, 255, 255, nameA, true );

                    // Ammo (gun slots only)
                    if( i < 2 ) {
                        string ammo = Main.SlotAmmoClip[i] + "/" + Main.SlotAmmoReserve[i];
                        DrawText2D( centerX, startY + 0.038f, ammo, 0.20f, 200, 200, 200, 180, true );
                    }
                }
            }

            // Weight indicator below slots
            float weightY = startY + slotH + 0.005f;
            float weight = Main.InventoryTotalWeight;
            string weightStr = "Weight: " + weight.ToString( "F0" ) + "/" + BRInventory.MAX_WEIGHT.ToString( "F0" );
            int wR = weight > 12 ? 200 : ( weight > 8 ? ACCENT_R : 200 );
            int wG = weight > 12 ? 80 : ( weight > 8 ? ACCENT_G : 200 );
            int wB = weight > 12 ? 80 : ( weight > 8 ? ACCENT_B : 200 );
            DrawText2D( 0.5f, weightY, weightStr, 0.22f, wR, wG, wB, 180, true );
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
