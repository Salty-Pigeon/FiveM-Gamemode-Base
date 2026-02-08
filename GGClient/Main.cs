using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooClient;
using GTA_GameRooShared;

namespace GGClient
{
    public class Main : BaseGamemode
    {
        // Weapon progression matching server
        public static readonly uint[] WeaponProgression = new uint[] {
            205991906,   // 0  Heavy Sniper
            3342088282,  // 1  Marksman Rifle
            2144741730,  // 2  Combat MG
            2634544996,  // 3  MG
            3220176749,  // 4  Assault Rifle
            2210333304,  // 5  Carbine Rifle
            3231910285,  // 6  Special Carbine
            2937143193,  // 7  Advanced Rifle
            3800352039,  // 8  Assault Shotgun
            487013001,   // 9  Pump Shotgun
            4024951519,  // 10 Assault SMG
            736523883,   // 11 SMG
            324215364,   // 12 Micro SMG
            2578377531,  // 13 Pistol .50
            1593441988,  // 14 Combat Pistol
            453432689,   // 15 Pistol
            3218215474,  // 16 SNS Pistol
            2578778090   // 17 Knife
        };

        public static readonly string[] WeaponNames = new string[] {
            "Heavy Sniper",
            "Marksman Rifle",
            "Combat MG",
            "MG",
            "Assault Rifle",
            "Carbine Rifle",
            "Special Carbine",
            "Advanced Rifle",
            "Assault Shotgun",
            "Pump Shotgun",
            "Assault SMG",
            "SMG",
            "Micro SMG",
            "Pistol .50",
            "Combat Pistol",
            "Pistol",
            "SNS Pistol",
            "Knife"
        };

        private int currentLevel = 0;

        public Main() : base( "gg" ) {
            HUD = new HUD();

            EventHandlers["salty::GGLevelUp"] += new Action<int>( OnLevelUp );
            EventHandlers["salty::GGDemoted"] += new Action<int>( OnDemoted );
            EventHandlers["salty::GGWinner"] += new Action<string>( OnWinner );

            var gmInfo = GamemodeRegistry.Register( "gg", "Gun Game",
                "Free-for-all weapon progression. Get a kill with each weapon to advance through 18 levels. First to complete the knife kill wins!", "#f59e0b" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 16;
            gmInfo.Tags = new string[] { "FFA", "Shooter", "Progression" };
            gmInfo.Teams = new string[] { "FFA" };
            gmInfo.Features = new string[] { "Weapon Progression", "Melee Demotion", "Leaderboard" };
            gmInfo.Guide = new GuideSection {
                Overview = "Gun Game is a free-for-all weapon progression gamemode. Every player starts with a Heavy Sniper and must get a kill with each weapon to advance to the next level. Progress through 18 weapons ending with the Knife. First player to get a knife kill wins!",
                HowToWin = "Be the first player to get a kill with every weapon, finishing with the Knife at level 17.",
                Rules = new string[] {
                    "Kill with your current weapon to advance to the next level.",
                    "Kills with the wrong weapon do not count for progression.",
                    "Getting killed by a melee weapon demotes you one level.",
                    "Weapon wheel is disabled — you can only use your current level's weapon.",
                    "If time runs out, the player with the highest level wins."
                },
                TeamRoles = new GuideTeamRole[] {
                    new GuideTeamRole {
                        Name = "FFA", Color = "#f59e0b",
                        Goal = "Progress through all 18 weapon levels before anyone else.",
                        Tips = new string[] {
                            "Snipers (levels 0-1) reward patience and accuracy.",
                            "MGs and Rifles (levels 2-7) are versatile — play aggressively.",
                            "Shotguns (levels 8-9) need close range — push into buildings.",
                            "SMGs and Pistols (levels 10-16) require good aim at medium range.",
                            "Knife (level 17) is the final challenge — sneak up on enemies."
                        }
                    }
                },
                Tips = new string[] {
                    "Adapt your playstyle to each weapon category.",
                    "Avoid melee players — getting knifed demotes you!",
                    "Watch the leaderboard to track your competition.",
                    "Snipers are best from high ground. Shotguns need close quarters.",
                    "The knife level is the hardest — be stealthy and patient."
                }
            };

            DebugRegistry.Register( "gg", "gg_set_level_0", "Set Level: 0 (Heavy Sniper)", "Level", () => {
                SetLevel( 0 );
                WriteChat( "Debug", "Level set to 0", 30, 200, 30 );
            } );
            DebugRegistry.Register( "gg", "gg_set_level_5", "Set Level: 5 (Carbine Rifle)", "Level", () => {
                SetLevel( 5 );
                WriteChat( "Debug", "Level set to 5", 30, 200, 30 );
            } );
            DebugRegistry.Register( "gg", "gg_set_level_10", "Set Level: 10 (Assault SMG)", "Level", () => {
                SetLevel( 10 );
                WriteChat( "Debug", "Level set to 10", 30, 200, 30 );
            } );
            DebugRegistry.Register( "gg", "gg_set_level_15", "Set Level: 15 (Pistol)", "Level", () => {
                SetLevel( 15 );
                WriteChat( "Debug", "Level set to 15", 30, 200, 30 );
            } );
            DebugRegistry.Register( "gg", "gg_set_level_17", "Set Level: 17 (Knife)", "Level", () => {
                SetLevel( 17 );
                WriteChat( "Debug", "Level set to 17", 30, 200, 30 );
            } );
            DebugRegistry.Register( "gg", "gg_level_up", "Level Up", "Level", () => {
                if( currentLevel < WeaponProgression.Length - 1 ) {
                    SetLevel( currentLevel + 1 );
                    WriteChat( "Debug", "Leveled up to " + currentLevel, 30, 200, 30 );
                }
            } );
            DebugRegistry.Register( "gg", "gg_level_down", "Level Down", "Level", () => {
                if( currentLevel > 0 ) {
                    SetLevel( currentLevel - 1 );
                    WriteChat( "Debug", "Leveled down to " + currentLevel, 30, 200, 30 );
                }
            } );
            DebugRegistry.Register( "gg", "gg_respawn", "Respawn", "Self", () => {
                PlayerSpawn();
                WriteChat( "Debug", "Respawned", 30, 200, 30 );
            } );
        }

        private void SetLevel( int level ) {
            currentLevel = level;
            TriggerServerEvent( "salty:netUpdatePlayerDetail", "level", level );
            GiveWeaponForLevel( level );
            SendNUILevelUpdate();
        }

        public override async void Start( float gameTime ) {
            base.Start( gameTime );

            currentLevel = 0;
            GiveWeaponForLevel( 0 );

            // Send NUI init
            SendNuiMessage( "{\"type\":\"ggInit\"}" );
            SendNUILevelUpdate();
            SendNUILeaderboard();

            await RunCountdown();
        }

        private void GiveWeaponForLevel( int level ) {
            if( level < 0 || level >= WeaponProgression.Length ) return;

            RemoveAllPedWeapons( PlayerPedId(), true );

            uint weaponHash = WeaponProgression[level];

            if( level == 17 ) {
                // Knife — melee weapon, no ammo needed
                GiveWeaponToPed( PlayerPedId(), weaponHash, 0, false, true );
            } else {
                GiveWeaponToPed( PlayerPedId(), weaponHash, 9999, false, true );
            }

            SetCurrentPedWeapon( PlayerPedId(), weaponHash, true );
        }

        public override void Update() {
            // Disable weapon wheel
            DisableControlAction( 0, 37, true );  // Weapon wheel (SELECT)
            DisableControlAction( 0, 12, true );  // Weapon wheel up
            DisableControlAction( 0, 13, true );  // Weapon wheel down
            DisableControlAction( 0, 14, true );  // Weapon wheel next
            DisableControlAction( 0, 15, true );  // Weapon wheel prev
            DisableControlAction( 0, 16, true );  // Select next weapon
            DisableControlAction( 0, 17, true );  // Select prev weapon

            CantEnterVehichles();

            base.Update();
        }

        public override void PlayerSpawn() {
            base.PlayerSpawn();

            // Heal player
            LocalPlayer.Character.MaxHealth = 100;
            LocalPlayer.Character.Health = 100;
            Game.PlayerPed.Armor = 0;

            GiveWeaponForLevel( currentLevel );
        }

        private void OnLevelUp( int newLevel ) {
            currentLevel = newLevel;
            GiveWeaponForLevel( newLevel );

            string weaponName = newLevel < WeaponNames.Length ? WeaponNames[newLevel] : "Unknown";
            SendNuiMessage( "{\"type\":\"ggLevelUp\",\"level\":" + newLevel + ",\"weaponName\":\"" + EscapeJson( weaponName ) + "\",\"maxLevel\":" + ( WeaponProgression.Length - 1 ) + "}" );
            SendNUILeaderboard();
        }

        private void OnDemoted( int newLevel ) {
            currentLevel = newLevel;
            GiveWeaponForLevel( newLevel );

            string weaponName = newLevel < WeaponNames.Length ? WeaponNames[newLevel] : "Unknown";
            SendNuiMessage( "{\"type\":\"ggDemotion\",\"level\":" + newLevel + ",\"weaponName\":\"" + EscapeJson( weaponName ) + "\",\"maxLevel\":" + ( WeaponProgression.Length - 1 ) + "}" );
            SendNUILeaderboard();
        }

        private void OnWinner( string name ) {
            HubNUI.ShowRoundEnd( name, "#f59e0b", "Completed all weapon levels!" );
        }

        public override void OnDetailUpdate( int ply, string key, object oldValue, object newValue ) {
            if( key == "level" ) {
                SendNUILeaderboard();
            }
        }

        private void SendNUILevelUpdate() {
            string weaponName = currentLevel < WeaponNames.Length ? WeaponNames[currentLevel] : "Unknown";
            SendNuiMessage( "{\"type\":\"ggUpdateLevel\",\"level\":" + currentLevel + ",\"weaponName\":\"" + EscapeJson( weaponName ) + "\",\"maxLevel\":" + ( WeaponProgression.Length - 1 ) + "}" );
        }

        private void SendNUILeaderboard() {
            var entries = new List<string>();
            int localId = LocalPlayer.ServerId;

            // Add local player
            entries.Add( "{\"name\":\"" + EscapeJson( LocalPlayer.Name ) + "\",\"level\":" + currentLevel + ",\"isLocal\":true}" );

            // Add other players
            foreach( var ply in Players ) {
                if( ply.ServerId == localId ) continue;
                object levelObj = GetPlayerDetail( ply.ServerId, "level" );
                int level = levelObj != null ? Convert.ToInt32( levelObj ) : 0;
                entries.Add( "{\"name\":\"" + EscapeJson( ply.Name ) + "\",\"level\":" + level + ",\"isLocal\":false}" );
            }

            SendNuiMessage( "{\"type\":\"ggLeaderboard\",\"players\":[" + string.Join( ",", entries ) + "]}" );
        }

        public override void End() {
            SendNuiMessage( "{\"type\":\"hideGameTimer\"}" );
            base.End();
        }

        private static string EscapeJson( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
        }
    }
}
