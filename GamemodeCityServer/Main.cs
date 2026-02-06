using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class Main : BaseScript {

        public static MapManager MapManager;

        public static Vote CurrentVote;

        int currentRound = 0;
        static float gameStartTimer = 0;
        static string gameStartID;

        Dictionary<string, int> GameVotes = new Dictionary<string, int>();
        Dictionary<int, int> MapVotes = new Dictionary<int, int>();

        public Main() {

            MapManager = new MapManager();

            EventHandlers["salty:netStartGame"] += new Action<string>( StartGame );
            EventHandlers["salty:netOpenMapGUI"] += new Action<Player>( OpenMapGUI );

            EventHandlers["salty:netBeginMapVote"] += new Action( BeginMapVote );
            EventHandlers["salty:netBeginGameVote"] += new Action( BeginGameVote );

            EventHandlers["salty:netVote"] += new Action<dynamic>( MakeVote );

            EventHandlers["salty:netUpdatePlayerDetail"] += new Action<Player, string, dynamic>( UpdateDetail );

            EventHandlers["saltyMap:netUpdate"] += new Action<Player, string>( MapManager.Update );
            EventHandlers["saltyMap:netDelete"] += new Action<Player, int>( MapManager.DeleteMap );

            EventHandlers["baseevents:onPlayerKilled"] += new Action<Player, int, dynamic>( PlayerKilled );
            EventHandlers["baseevents:onPlayerDied"] += new Action<Player, int, List<dynamic>>( PlayerDied );

            base.Tick += Tick;
        }

        private async Task Tick() {
            if( ServerGlobals.CurrentGame != null ) {
                ServerGlobals.CurrentGame.Update();
                if( ServerGlobals.CurrentGame.GameTime < GetGameTimer() ) {
                    ServerGlobals.CurrentGame.OnTimerEnd();
                }
            }
            if( gameStartTimer > 0 && gameStartTimer < GetGameTimer() ) {
                StartGame( gameStartID );
                gameStartTimer = 0;
            }
        }

        private void UpdateDetail( [FromSource] Player ply, string key, dynamic data ) {
            if( ServerGlobals.CurrentGame != null ) {
                ServerGlobals.CurrentGame.SetPlayerDetail( ply, key, data );
            }
        }

        private void MakeVote( dynamic ID ) {
            CurrentVote.MakeVote( ID );
        }

        public void BeginMapVote() {
            CurrentVote = new Vote( EndMapVote );
            TriggerClientEvent( "salty:MapVote", MapManager.MapList() );
        }

        public void EndMapVote( dynamic id ) {
            int ID = Convert.ToInt32( id );
            ServerMap winner = MapManager.Maps.Where( x => x.ID == ID ).FirstOrDefault();
            if( winner != null ) {
                BaseGamemode.WriteChat( "Map Vote", "Winner is " + winner.Name, 200, 200, 0 );
            }
        }

        public static void BeginGameVote() {
            CurrentVote = new Vote( EndGameVote );
            TriggerClientEvent( "salty:GameVote", ServerGlobals.GamemodeList() );
        }

        public static void EndGameVote( dynamic id ) {
            string ID = id.ToString();
            BaseGamemode.WriteChat( "Game Vote", "Winner is " + ID, 200, 200, 0 );
            gameStartTimer = GetGameTimer() + (1 * 1000 * 10);
            gameStartID = ID;
        }

        private void PlayerKilled( [FromSource] Player ply, int killerID, dynamic deathData ) {
            try {
                int killerType = 0;
                List<object> deathCoords = new List<object>();
                uint weaponHash = 0;

                var deathDict = deathData as IDictionary<string, object>;
                if( deathDict != null ) {
                    if( deathDict.ContainsKey( "killertype" ) )
                        killerType = Convert.ToInt32( deathDict["killertype"] );
                    if( deathDict.ContainsKey( "killerpos" ) )
                        deathCoords = deathDict["killerpos"] as List<object>;
                    if( deathDict.ContainsKey( "weaponhash" ) )
                        weaponHash = Convert.ToUInt32( deathDict["weaponhash"] );
                }

                Vector3 DeathCoords = new Vector3( 0, 0, 0 );
                if( deathCoords != null && deathCoords.Count >= 3 ) {
                    DeathCoords = new Vector3( Convert.ToSingle( deathCoords[0] ), Convert.ToSingle( deathCoords[1] ), Convert.ToSingle( deathCoords[2] ) );
                }

                if( killerID > -1 ) {
                    if( ServerGlobals.CurrentGame != null )
                        ServerGlobals.CurrentGame.OnPlayerKilled( ply, ServerGlobals.CurrentGame.GetPlayer( GetPlayerFromIndex( killerID ) ), DeathCoords, weaponHash );
                } else {
                    if( ServerGlobals.CurrentGame != null )
                        ServerGlobals.CurrentGame.OnPlayerKilled( ply, null, DeathCoords, weaponHash );
                }
            } catch( Exception ex ) {
                Debug.WriteLine( $"[GamemodeCity] Error in PlayerKilled: {ex.Message}" );
            }
        }

        private void PlayerDied( [FromSource] Player ply, int killerType, List<dynamic> deathcords ) {
            try {
                Vector3 coords = new Vector3( Convert.ToSingle( deathcords[0] ), Convert.ToSingle( deathcords[1] ), Convert.ToSingle( deathcords[2] ) );
                if( ServerGlobals.CurrentGame != null )
                    ServerGlobals.CurrentGame.OnPlayerDied( ply, killerType, coords );
            } catch( Exception ex ) {
                Debug.WriteLine( $"[GamemodeCity] Error in PlayerDied: {ex.Message}" );
            }
        }

        void OpenMapGUI( [FromSource] Player ply ) {
            if( !IsPlayerAceAllowed( ply.Handle, "mapdesigner.use" ) ) {
                ply.TriggerEvent( "chat:addMessage", new { args = new[] { "^1[Maps] You don't have permission to use the map editor." } } );
                return;
            }

            foreach( ServerMap map in MapManager.Maps ) {
                string mapJson = map.ToJson();
                ply.TriggerEvent( "salty:CacheMap", mapJson );
            }
            ply.TriggerEvent( "salty:OpenMapGUI" );
        }

        public static void StartGame( string ID ) {
            var map = MapManager.FindMap( ID );
            if( map == null ) {
                BaseGamemode.WriteChat( ID.ToUpper(), "No map available for this gamemode!", 200, 30, 30 );
                return;
            }

            ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes[ID.ToLower()].GetType() );
            ServerGlobals.CurrentGame.GameTime = GetGameTimer() + ServerGlobals.CurrentGame.Settings.GameLength;
            ServerGlobals.CurrentGame.Map = map;
            ServerGlobals.CurrentGame.Start();
            BaseGamemode.WriteChat( ID.ToUpper(), "Playing map " + ServerGlobals.CurrentGame.Map.Name, 200, 30, 30 );
        }
    }
}
