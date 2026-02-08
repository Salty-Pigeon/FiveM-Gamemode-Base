using GTA_GameRooServer;
using GTA_GameRooShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMServer
{

    public enum Teams {
        IceCreamMan,
        Kiddie      
    }


    public class Main : BaseGamemode
    {

        Player IceCreamMan;

        public bool SoloTestMode = false;

        public Main() : base( "ICM" ) {
            Settings.GameLength = (10 * 1000 * 60);
            Settings.Name = "Ice Cream Man";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);

            EventHandlers["salty:netStartSoloICM"] += new Action<Player>( OnNetStartSoloICM );
            EventHandlers["salty:netICMKidWin"] += new Action( OnKidWin );
        }

        

        private void OnNetStartSoloICM( [FromSource] Player player ) {
            if( GTA_GameRooServer.PlayerProgression.GetAdminLevel( player ) < 1 ) return;
            StartSoloMode();
        }

        private void StartSoloMode() {
            SoloTestMode = true;
            WriteChat( "ICM", "Solo test mode enabled - starting as Ice Cream Man", 200, 200, 0 );

            if( ServerGlobals.CurrentGame != null ) {
                ServerGlobals.CurrentGame.End();
            }

            var existingMap = GTA_GameRooServer.Main.MapManager.FindMap( "icm" );
            if( existingMap != null ) {
                ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes["icm"].GetType() );
                ServerGlobals.CurrentGame.GameTime = GetGameTimer() + ServerGlobals.CurrentGame.Settings.GameLength;
                ServerGlobals.CurrentGame.Map = existingMap;
                ServerGlobals.CurrentGame.Start();
                WriteChat( "ICM", "Playing on " + existingMap.Name, 200, 200, 0 );
            } else {
                StartWithTestMap();
            }
        }

        private void StartWithTestMap() {
            Vector3 mapCenter = new Vector3( 195f, -935f, 30f );
            Vector3 mapSize = new Vector3( 200f, 200f, 50f );

            ServerMap testMap = new ServerMap( -999, "ICM Solo Test Arena", new List<string> { "icm" }, mapCenter, mapSize );
            testMap.Author = "Solo Mode";
            testMap.Description = "Temporary test map for solo ICM debugging";
            testMap.MinPlayers = 1;

            testMap.Spawns.Add( new Spawn( 1, new Vector3( 195f, -935f, 30.5f ), SpawnType.PLAYER, "icm_spawn", 0, 0f ) );
            testMap.Spawns.Add( new Spawn( 2, new Vector3( 195f, -835f, 30.5f ), SpawnType.PLAYER, "kid_spawn", 1, 180f ) );
            var winBarrier = new Spawn( 3, new Vector3( 195f, -785f, 30.5f ), SpawnType.WIN_BARRIER, "finish_line", 0, 0f );
            winBarrier.SizeX = 30f;
            winBarrier.SizeY = 5f;
            testMap.Spawns.Add( winBarrier );

            ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes["icm"].GetType() );
            ServerGlobals.CurrentGame.GameTime = GetGameTimer() + ServerGlobals.CurrentGame.Settings.GameLength;
            ServerGlobals.CurrentGame.Map = testMap;
            ServerGlobals.CurrentGame.Start();
            WriteChat( "ICM", "Playing on ICM Solo Test Arena", 200, 200, 0 );
        }

        private void OnKidWin() {
            WriteChat( "Ice Cream Man", "A kid reached the finish line!", 6, 182, 212 );
            TriggerClientEvent( "salty::ICMRoundResult", "Kids", "#06b6d4", "A kid reached the finish line!" );
            WinningPlayers.AddRange( GetTeamPlayers( (int)Teams.Kiddie ) );
            End();
        }

        public override void Start() {

            base.Start();

            List<Player> playerList = new PlayerList().ToList();

            if( SoloTestMode && playerList.Count == 1 ) {
                IceCreamMan = playerList[0];
                SetTeam( IceCreamMan, (int)Teams.IceCreamMan );
                SpawnPlayer( IceCreamMan );
                WriteChat( "ICM", "Solo mode: You are the Ice Cream Man. Use /spawnbot for kid bots.", 200, 200, 0 );
                return;
            }

            IceCreamMan = playerList.OrderBy( x => Guid.NewGuid() ).First();
            playerList.Remove( IceCreamMan );
            SetTeam( IceCreamMan, (int)Teams.IceCreamMan );
            SpawnPlayer( IceCreamMan );

            foreach( var player in playerList ) {
                SetTeam( player, (int)Teams.Kiddie );
                SpawnPlayer( player );
            }

        }
        

        public override void OnTimerEnd() {
            WriteChat( "Ice Cream Man", "Ice cream man delivered ice cream safely", 255, 0, 0 );
            TriggerClientEvent( "salty::ICMRoundResult", "Ice Cream Man", "#f59e0b", "All ice cream delivered safely!" );
            if( IceCreamMan != null ) WinningPlayers.Add( IceCreamMan );
            base.OnTimerEnd();
        }

        public override void OnPlayerKilled( Player victim, Player attacker, Vector3 deathCoords, uint weaponHash ) {
            Teams team = (Teams)GetPlayerDetail( victim, "team" );
            if( team == Teams.IceCreamMan ) {
                WriteChat( "Ice Cream Man", "Ice cream man defeated. Bikers win.", 255, 0, 0 );
                TriggerClientEvent( "salty::ICMRoundResult", "Kids", "#06b6d4", "The Ice Cream Man has been stopped!" );
                WinningPlayers.AddRange( GetTeamPlayers( (int)Teams.Kiddie ) );
                End();
            }
            base.OnPlayerKilled( victim, attacker, deathCoords, weaponHash );
        }



        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathcords ) {
            Teams team = (Teams)GetPlayerDetail( victim, "team" );
            if( team == Teams.Kiddie ) {
                AddScore( IceCreamMan, 1 );

                // Check if all kids are dead
                List<Player> kids = GetTeamPlayers( (int)Teams.Kiddie );
                bool allDead = true;
                foreach( var kid in kids ) {
                    if( kid != victim ) {
                        allDead = false;
                        break;
                    }
                }
                if( allDead ) {
                    TriggerClientEvent( "salty::ICMRoundResult", "Ice Cream Man", "#f59e0b", "All kids have been splattered!" );
                    if( IceCreamMan != null ) WinningPlayers.Add( IceCreamMan );
                    End();
                    return;
                }
            }
            base.OnPlayerDied( victim, killerType, deathcords );
        }



        public override void End() {
            SoloTestMode = false;
            base.End();
        }

    }
}
