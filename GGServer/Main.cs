using GTA_GameRooServer;
using GTA_GameRooShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GGServer
{
    public class Main : BaseGamemode
    {
        // Weapon progression: level 0 (Heavy Sniper) through level 17 (Knife)
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

        // Known melee weapon hashes
        private static readonly HashSet<uint> MeleeWeapons = new HashSet<uint> {
            2578778090,  // Knife
            2508868239,  // Bat
            1737195953,  // Bottle
            1317494643,  // Crowbar
            4192643659,  // Flashlight
            2343591895,  // Golf Club
            1141786504,  // Hammer
            4191993645,  // Hatchet
            3638508604,  // Knuckles
            2228533352,  // Machete
            3756226112,  // Nightstick
            2460120199,  // Wrench
            2725352035,  // Switchblade
            2484171525,  // Fist (unarmed)
        };

        public Main() : base( "gg" ) {
            Settings.GameLength = (10 * 1000 * 60);
            Settings.Name = "Gun Game";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);
        }

        public override void Start() {
            base.Start();

            List<Player> playerList = new PlayerList().ToList();

            foreach( var player in playerList ) {
                SetTeam( player, 0 );
                SetPlayerDetail( player, "level", 0 );
                SpawnPlayer( player, 0 );
            }
        }

        public override void OnPlayerKilled( Player victim, Player attacker, Vector3 deathCoords, uint weaponHash ) {
            if( attacker == null || victim == null ) return;
            if( attacker.Handle == victim.Handle ) {
                // Suicide — just respawn
                SpawnPlayer( victim, 0 );
                return;
            }

            // Check if melee kill — demote victim
            if( MeleeWeapons.Contains( weaponHash ) ) {
                object victimLevelObj = GetPlayerDetail( victim, "level" );
                int victimLevel = victimLevelObj != null ? Convert.ToInt32( victimLevelObj ) : 0;
                if( victimLevel > 0 ) {
                    int newLevel = victimLevel - 1;
                    SetPlayerDetail( victim, "level", newLevel );
                    victim.TriggerEvent( "salty::GGDemoted", newLevel );
                    WriteChat( "Gun Game", victim.Name + " was demoted to level " + newLevel + "!", 239, 68, 68 );
                }
            }

            // Check if attacker killed with their current level weapon
            object attackerLevelObj = GetPlayerDetail( attacker, "level" );
            int attackerLevel = attackerLevelObj != null ? Convert.ToInt32( attackerLevelObj ) : 0;

            if( attackerLevel >= 0 && attackerLevel < WeaponProgression.Length && weaponHash == WeaponProgression[attackerLevel] ) {
                int newLevel = attackerLevel + 1;

                if( newLevel >= WeaponProgression.Length ) {
                    // Attacker completed the final level — they win!
                    WriteChat( "Gun Game", attacker.Name + " wins! Completed all " + WeaponProgression.Length + " levels!", 245, 158, 11 );
                    TriggerClientEvent( "salty::GGWinner", attacker.Name );
                    WinningPlayers.Add( attacker );
                    SpawnPlayer( victim, 0 );
                    End();
                    return;
                } else {
                    SetPlayerDetail( attacker, "level", newLevel );
                    attacker.TriggerEvent( "salty::GGLevelUp", newLevel );
                    WriteChat( "Gun Game", attacker.Name + " advanced to level " + newLevel + "!", 245, 158, 11 );
                }
            }

            SpawnPlayer( victim, 0 );
        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {
            SpawnPlayer( victim, 0 );
            base.OnPlayerDied( victim, killerType, deathCoords );
        }

        public override void OnTimerEnd() {
            // Find the player with the highest level
            Player winner = null;
            int highestLevel = -1;

            foreach( var player in new PlayerList() ) {
                object levelObj = GetPlayerDetail( player, "level" );
                int level = levelObj != null ? Convert.ToInt32( levelObj ) : 0;
                if( level > highestLevel ) {
                    highestLevel = level;
                    winner = player;
                }
            }

            string winnerName = winner != null ? winner.Name : "Nobody";
            WriteChat( "Gun Game", "Time's up! " + winnerName + " wins at level " + highestLevel + "!", 245, 158, 11 );
            TriggerClientEvent( "salty::GGWinner", winnerName );
            if( winner != null ) WinningPlayers.Add( winner );
            base.OnTimerEnd();
        }
    }
}
