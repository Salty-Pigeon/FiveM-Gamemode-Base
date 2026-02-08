using GTA_GameRooServer;
using GTA_GameRooShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTTServer
{

    public enum Teams {
        Innocent,
        Traitor,
        Detective
    }

    public class Main : BaseGamemode
    {

        List<Player> Traitors = new List<Player>();
        List<Player> Detectives = new List<Player>();
        List<Player> Innocents = new List<Player>();

        double traitorsPerPlayers = 4;
        double detectivesPerPlayers = 4;
        float timeAddedOnDeath = 1 * 1000 * 45;

        public Dictionary<int, bool> DeadBodies = new Dictionary<int, bool>();

        // Solo testing mode - allows 1 player to test as traitor
        public static bool SoloTestMode = false;


        public Main() : base( "TTT" ) {
            Settings.Weapons = new List<uint>() { 2725352035, 453432689, 736523883, 3220176749, 4024951519, 2937143193, 3173288789 };
            Settings.Name = "Trouble in Terrorist Town";
            Settings.Rounds = 1;
            Settings.GameLength = (10 * 1000 * 60);
            Settings.PreGameTime = (1 * 1000 * 15);

            EventHandlers["salty::netBodyDiscovered"] += new Action<Player, int>( BodyDiscovered );
            EventHandlers["salty:netStartSoloTTT"] += new Action<Player>( OnNetStartSoloTTT );
        }

        private void OnNetStartSoloTTT( [FromSource] Player player ) {
            if( GTA_GameRooServer.PlayerProgression.GetAdminLevel( player ) < 1 ) return;
            StartSoloMode();
        }

        private void StartSoloMode() {
            SoloTestMode = true;
            WriteChat( "TTT", "Solo test mode enabled - starting as Traitor", 200, 200, 0 );

            // End any current game first
            if( ServerGlobals.CurrentGame != null ) {
                ServerGlobals.CurrentGame.End();
            }

            var existingMap = GTA_GameRooServer.Main.MapManager.FindMap( "ttt" );
            if( existingMap != null ) {
                // Start directly, bypassing normal StartGame to avoid any issues
                ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes["ttt"].GetType() );
                ServerGlobals.CurrentGame.GameTime = GetGameTimer() + ServerGlobals.CurrentGame.Settings.GameLength;
                ServerGlobals.CurrentGame.Map = existingMap;
                ServerGlobals.CurrentGame.Start();
                WriteChat( "TTT", "Playing on " + existingMap.Name, 200, 200, 0 );
            } else {
                StartWithTestMap();
            }
        }

        private void StartWithTestMap() {
            // Create a test map centered around Legion Square in Los Santos
            Vector3 mapCenter = new Vector3( 195f, -935f, 30f );
            Vector3 mapSize = new Vector3( 100f, 100f, 50f );

            ServerMap testMap = new ServerMap( -999, "Solo Test Arena", new List<string> { "ttt" }, mapCenter, mapSize );
            testMap.Author = "Solo Mode";
            testMap.Description = "Temporary test map for solo debugging";
            testMap.MinPlayers = 1;

            // Add player spawn points (team 0 for all teams in solo mode)
            testMap.Spawns.Add( new Spawn( 1, new Vector3( 195f, -935f, 30.5f ), SpawnType.PLAYER, "spawn1", 0 ) );
            testMap.Spawns.Add( new Spawn( 2, new Vector3( 200f, -930f, 30.5f ), SpawnType.PLAYER, "spawn2", 0 ) );
            testMap.Spawns.Add( new Spawn( 3, new Vector3( 190f, -940f, 30.5f ), SpawnType.PLAYER, "spawn3", 0 ) );

            // Add weapon spawn points around the area
            testMap.Spawns.Add( new Spawn( 10, new Vector3( 198f, -932f, 30.5f ), SpawnType.WEAPON, "wep1", 0 ) );
            testMap.Spawns.Add( new Spawn( 11, new Vector3( 192f, -938f, 30.5f ), SpawnType.WEAPON, "wep2", 0 ) );
            testMap.Spawns.Add( new Spawn( 12, new Vector3( 205f, -928f, 30.5f ), SpawnType.WEAPON, "wep3", 0 ) );
            testMap.Spawns.Add( new Spawn( 13, new Vector3( 188f, -945f, 30.5f ), SpawnType.WEAPON, "wep4", 0 ) );
            testMap.Spawns.Add( new Spawn( 14, new Vector3( 210f, -935f, 30.5f ), SpawnType.WEAPON, "wep5", 0 ) );

            // Start the game with this test map
            ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes["ttt"].GetType() );
            ServerGlobals.CurrentGame.GameTime = GetGameTimer() + ServerGlobals.CurrentGame.Settings.GameLength;
            ServerGlobals.CurrentGame.Map = testMap;
            ServerGlobals.CurrentGame.Start();
            WriteChat( "TTT", "Playing on Solo Test Arena", 200, 200, 0 );
        }

        public override void Start() {

            base.Start();

            Map.SpawnGuns();

            List<Player> playerList = new PlayerList().ToList();

            // Solo test mode - single player becomes traitor, no win conditions
            if( SoloTestMode && playerList.Count == 1 ) {
                var player = playerList[0];
                Traitors.Add( player );
                SetTeam( player, (int)Teams.Traitor );
                SetPlayerDetail( player, "coins", 5 ); // Give extra coins for testing
                WriteChat( "TTT", "Solo mode: You are a Traitor. Use /endttt to end.", 200, 200, 0 );
                SpawnPlayers( 0 );
                return;
            }

            for( var i = 0; i < Math.Ceiling(playerList.Count / traitorsPerPlayers); i++ ) {
                var player = playerList.OrderBy( x => Guid.NewGuid() ).First();
                Debug.WriteLine( playerList.Count.ToString() );

                playerList.Remove( player );

                Traitors.Add( player );
                SetTeam( player, (int)Teams.Traitor );
                SetPlayerDetail( player, "coins", 1 );
            }


            if( playerList.Count > 0 ) {
                for( var i = 0; i < Math.Floor( playerList.Count / detectivesPerPlayers ); i++ ) {

                    var player = playerList.OrderBy( x => Guid.NewGuid() ).First();
                    playerList.Remove( player );
                    Detectives.Add( player );
                    SetTeam( player, (int)Teams.Detective );
                    SetPlayerDetail( player, "coins", 1 );
                }
            }

            if( playerList.Count > 0 ) {
                foreach( var player in playerList ) {
                    Innocents.Add( player );
                    SetTeam( player, (int)Teams.Innocent );
                }
            }

            SpawnPlayers( 0 );

        }

        public override void OnPlayerKilled( Player victim, Player attacker, Vector3 deathCoords, uint weaponHash ) {
            TriggerClientEvent( "salty::SpawnDeadBody", deathCoords, Convert.ToInt32( victim.Handle ), Convert.ToInt32( attacker.Handle ), weaponHash );
            if( attacker != null && Traitors.Contains( attacker )  ) {
                AddPlayerDetail( attacker, "coins", 1 );
            }
                
            base.OnPlayerKilled( attacker, victim, deathCoords, weaponHash );
        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {

            // Skip win conditions in solo mode
            if( SoloTestMode ) {
                base.OnPlayerDied( victim, killerType, deathCoords );
                return;
            }

            object teamObj = GetPlayerDetail( victim, "team" );
            if( Traitors.Contains( victim ) ) {
                Traitors.Remove( victim );
                if( Traitors.Count == 0 ) {
                    TriggerClientEvent( "salty::TTTRoundResult", "Innocents", "#22c55e", "All traitors eliminated" );
                    WriteChat( "TTT", "Innocents win", 20, 200, 20 );
                    WinningPlayers.AddRange( Innocents );
                    WinningPlayers.AddRange( Detectives );
                    End();
                    return;
                }
            }

            if( !Traitors.Contains( victim ) ) {
                GameTime += timeAddedOnDeath;
                foreach( var ply in Traitors ) {
                    ply.TriggerEvent( "salty:UpdateTime", GameTime );
                }
            }

            if( Detectives.Contains( victim ) ) {
                Detectives.Remove( victim );
            }
            if( Innocents.Contains( victim ) ) {
                Innocents.Remove( victim );
            }

            if( Innocents.Count + Detectives.Count == 0 ) {
                TriggerClientEvent( "salty::TTTRoundResult", "Traitors", "#ef4444", "All innocents eliminated" );
                WriteChat( "TTT", "Traitors win", 200, 20, 20 );
                WinningPlayers.AddRange( Traitors );
                End();
                return;
            }

            base.OnPlayerDied( victim, killerType, deathCoords );
        }

        public override void OnDetailUpdate( Player ply, string key, object oldValue, object newValue ) {
            if( key == "disguise" ) {

            }
            base.OnDetailUpdate( ply, key, oldValue, newValue );
        }

        public void BodyDiscovered( [FromSource] Player ply, int body ) {
            DeadBodies[body] = true;
            TriggerClientEvent( "salty::UpdateDeadBody", body );
        }


        public override void End( ) {
            if( !SoloTestMode && GameTime < GetGameTimer() ) {
                TriggerClientEvent( "salty::TTTRoundResult", "Innocents", "#22c55e", "Time expired" );
                WriteChat( "TTT", "Time over! Innocents win", 200, 20, 20 );
                WinningPlayers.AddRange( Innocents );
                WinningPlayers.AddRange( Detectives );
            }
            SoloTestMode = false; // Reset solo mode
            base.End();
        }

    }
}
