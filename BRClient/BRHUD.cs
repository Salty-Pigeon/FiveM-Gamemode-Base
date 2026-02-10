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

        // NUI panel change tracking
        int lastSentAlive = -1;
        string lastSentPhase = null;
        string lastSentZoneTimer = null;

        public override void Start() {
            base.Start();
            DisplayRadar( true );
        }

        public override void Draw() {
            DrawGameTimer();
            DrawBRPanel();
            DrawInventoryBar();
            ShowContainerPrompt();
            DrawConsumableProgress();
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

        int FA( int baseAlpha, float fade ) {
            return (int)( baseAlpha * fade );
        }

        void DrawInventoryBar() {
            // Auto-hide fade logic
            float now = GetGameTimer();
            float elapsed = now - Main.LastWeaponChangeTime;
            float fadeAlpha;
            if( elapsed < 3000f ) fadeAlpha = 1.0f;
            else if( elapsed < 3500f ) fadeAlpha = 1.0f - ( elapsed - 3000f ) / 500f;
            else return; // Fully hidden

            float slotW = 0.055f;
            float slotH = 0.050f;
            float gap = 0.004f;
            float totalW = slotW * 3 + gap * 2;
            float startX = 0.5f - totalW / 2f;
            float startY = 0.90f;

            for( int i = 0; i < 3; i++ ) {
                float x = startX + i * ( slotW + gap );
                bool isActive = i == Main.InventoryActiveSlot;
                uint hash = Main.InventorySlots[i];
                bool isEmpty = hash == 0;

                // Dim slots that can't be used in vehicles
                bool dimmed = Main.InVehicle && hash != 0 && !BRInventory.CanUseInVehicleStatic( hash );
                float dimMul = dimmed ? 0.35f : 1.0f;

                // Darker backgrounds
                int bgR = isActive ? 20 : 10;
                int bgG = isActive ? 18 : 10;
                int bgB = isActive ? 15 : 15;
                int bgA = isActive ? 220 : 190;
                DrawRectangle( x, startY, slotW, slotH, bgR, bgG, bgB, FA( (int)( bgA * dimMul ), fadeAlpha ) );

                // Bottom-only accent line for active slot
                if( isActive && !dimmed ) {
                    DrawRectangle( x, startY + slotH - 0.003f, slotW, 0.003f, ACCENT_R, ACCENT_G, ACCENT_B, FA( 255, fadeAlpha ) );
                }

                // Slot number
                float centerX = x + slotW / 2f;
                string slotNum = ( i + 1 ).ToString();
                DrawText2D( x + 0.004f, startY + 0.002f, slotNum, 0.20f,
                    isActive && !dimmed ? ACCENT_R : 150, isActive && !dimmed ? ACCENT_G : 150, isActive && !dimmed ? ACCENT_B : 150,
                    FA( (int)( ( isActive ? 255 : 180 ) * dimMul ), fadeAlpha ), false );

                if( isEmpty ) {
                    DrawText2D( centerX, startY + 0.016f, "---", 0.20f, 100, 100, 100, FA( 140, fadeAlpha ), true );
                } else {
                    // Weapon name
                    string name = "";
                    if( GTA_GameRooShared.Globals.Weapons.ContainsKey( hash ) ) {
                        name = GTA_GameRooShared.Globals.Weapons[hash]["Name"];
                    }
                    if( name.Length > 10 ) name = name.Substring( 0, 9 ) + ".";
                    int nameA = isActive ? 255 : 200;
                    DrawText2D( centerX, startY + 0.013f, name, 0.20f, 255, 255, 255, FA( (int)( nameA * dimMul ), fadeAlpha ), true );

                    // Ammo (gun slots only)
                    if( i < 2 ) {
                        string ammo = Main.SlotAmmoClip[i] + "/" + Main.SlotAmmoReserve[i];
                        DrawText2D( centerX, startY + 0.030f, ammo, 0.18f, 200, 200, 200, FA( (int)( 180 * dimMul ), fadeAlpha ), true );
                    }
                }
            }

            // Consumable icons after weapon slots
            float consX = startX + totalW + 0.008f;
            float consY = startY + 0.006f;
            float iconW = 0.008f;
            float iconH = 0.014f;

            // Bandage: green rectangle + count
            DrawRectangle( consX, consY, iconW, iconH, 30, 200, 80, FA( 220, fadeAlpha ) );
            DrawText2D( consX + iconW + 0.004f, consY, Main.BandageCount.ToString(), 0.22f, 255, 255, 255, FA( 220, fadeAlpha ), false );

            // Adrenaline: orange rectangle + count (with pulse when active)
            float adrenY = consY + 0.022f;
            if( Main.AdrenalineActive ) {
                float pulse = (float)( Math.Sin( GetGameTimer() / 200.0 ) * 0.5 + 0.5 );
                int aA = 160 + (int)( 95 * pulse );
                DrawRectangle( consX, adrenY, iconW, iconH, ACCENT_R, ACCENT_G, ACCENT_B, FA( aA, fadeAlpha ) );
                DrawText2D( consX + iconW + 0.004f, adrenY, Main.AdrenalineCount.ToString(), 0.22f, ACCENT_R, ACCENT_G, ACCENT_B, FA( aA, fadeAlpha ), false );
            } else {
                DrawRectangle( consX, adrenY, iconW, iconH, ACCENT_R, ACCENT_G, ACCENT_B, FA( 180, fadeAlpha ) );
                DrawText2D( consX + iconW + 0.004f, adrenY, Main.AdrenalineCount.ToString(), 0.22f, 200, 200, 200, FA( 180, fadeAlpha ), false );
            }

            // Weight indicator below slots
            float weightY = startY + slotH + 0.004f;
            float weight = Main.InventoryTotalWeight;
            string weightStr = "Weight: " + weight.ToString( "F0" ) + "/" + BRInventory.MAX_WEIGHT.ToString( "F0" );
            int wR = weight > 12 ? 200 : ( weight > 8 ? ACCENT_R : 200 );
            int wG = weight > 12 ? 80 : ( weight > 8 ? ACCENT_G : 200 );
            int wB = weight > 12 ? 80 : ( weight > 8 ? ACCENT_B : 200 );
            DrawText2D( 0.5f, weightY, weightStr, 0.20f, wR, wG, wB, FA( 180, fadeAlpha ), true );
        }

        void DrawBRPanel() {
            string phaseStr = ZonePhase >= 0 ? "Phase " + ( ZonePhase + 1 ) : "--";

            // Zone countdown timer
            float now = GetGameTimer();
            string zoneTimer = "--";
            bool timerOrange = false;
            if( Main.ZoneShrinkEnd > now && Main.ZoneShrinkEnd > 0 ) {
                int sec = Math.Max( 0, (int)Math.Ceiling( ( Main.ZoneShrinkEnd - now ) / 1000f ) );
                zoneTimer = "Shrinking: " + sec + "s";
                timerOrange = true;
            } else if( Main.ZoneWaitUntil > now && Main.ZoneWaitUntil > 0 ) {
                int sec = Math.Max( 0, (int)Math.Ceiling( ( Main.ZoneWaitUntil - now ) / 1000f ) );
                zoneTimer = "Next zone: " + sec + "s";
            }

            if( AliveCount != lastSentAlive || phaseStr != lastSentPhase || zoneTimer != lastSentZoneTimer ) {
                lastSentAlive = AliveCount;
                lastSentPhase = phaseStr;
                lastSentZoneTimer = zoneTimer;
                SendNuiMessage( "{\"type\":\"brUpdatePanel\",\"alive\":" + AliveCount + ",\"phase\":\"" + phaseStr + "\",\"zoneTimer\":\"" + zoneTimer + "\",\"zoneTimerOrange\":" + ( timerOrange ? "true" : "false" ) + "}" );
            }
        }

        public void HideBRPanel() {
            lastSentAlive = -1;
            lastSentPhase = null;
            SendNuiMessage( "{\"type\":\"brHidePanel\"}" );
        }

        void DrawConsumableProgress() {
            if( !Main.IsUsingConsumable ) return;

            float barW = 0.16f;
            float barH = 0.018f;
            float barX = 0.5f - barW / 2f;
            float barY = 0.76f;

            // Dark background
            DrawRectangle( barX - 0.004f, barY - 0.022f, barW + 0.008f, barH + 0.032f, 15, 15, 15, 200 );
            // Left accent strip
            DrawRectangle( barX - 0.004f, barY - 0.022f, 0.003f, barH + 0.032f, ACCENT_R, ACCENT_G, ACCENT_B, 255 );

            // Label text
            DrawText2D( 0.5f, barY - 0.019f, Main.ConsumableLabel, 0.26f, 255, 255, 255, 240, true );

            // Bar background
            DrawRectangle( barX, barY, barW, barH, 30, 30, 30, 220 );
            // Bar fill
            bool isBandage = Main.ConsumableLabel.Contains( "Bandage" );
            int fillR = isBandage ? 30 : ACCENT_R;
            int fillG = isBandage ? 200 : ACCENT_G;
            int fillB = isBandage ? 80 : ACCENT_B;
            DrawRectangle( barX, barY, barW * Main.ConsumableProgress, barH, fillR, fillG, fillB, 230 );

            // Percentage
            string pct = ((int)( Main.ConsumableProgress * 100 )) + "%";
            DrawText2D( 0.5f, barY + 0.001f, pct, 0.24f, 255, 255, 255, 255, true );
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
