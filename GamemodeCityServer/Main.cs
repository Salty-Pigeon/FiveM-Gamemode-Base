using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using GamemodeCityShared;

namespace GamemodeCityServer
{
    public class Main : BaseScript
    {

        public static MapManager MapManager;
        Database Database;

        public static Vote CurrentVote;

        int currentRound = 0;

        Dictionary<string, int> GameVotes = new Dictionary<string, int>();
        Dictionary<int, int> MapVotes = new Dictionary<int, int>();

        public Main() {

            MapManager = new MapManager();
            Database = new Database( MapManager );

            EventHandlers["salty:netStartGame"] += new Action<string>(StartGame);
            EventHandlers["salty:netOpenMapGUI"] += new Action<Player>( OpenMapGUI );

            EventHandlers["salty:netBeginMapVote"] += new Action( BeginMapVote );
            EventHandlers["salty:netBeginGameVote"] += new Action( BeginGameVote );

            EventHandlers["salty:netVote"] += new Action<dynamic>( MakeVote );

            EventHandlers["salty:netUpdatePlayerDetail"] += new Action<Player, string, dynamic>( UpdateDetail );


            EventHandlers["saltyMap:netUpdate"] += new Action<Player, ExpandoObject>( MapManager.Update );

            EventHandlers["baseevents:onPlayerKilled"] += new Action<Player, int, ExpandoObject>( PlayerKilled );
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
            BaseGamemode.WriteChat( "Map Vote", "Winner is " + winner.Name, 200, 200, 0 );
        }

        public static void BeginGameVote() {
            CurrentVote = new Vote( EndGameVote );
            TriggerClientEvent( "salty:GameVote", ServerGlobals.GamemodeList() );
        }


        public static void EndGameVote( dynamic id ) {
            string ID = id.ToString();
            BaseGamemode.WriteChat( "Game Vote", "Winner is " + ID, 200, 200, 0 );
            StartGame( ID );
        }



        private void PlayerKilled( [FromSource] Player ply, int killerID, ExpandoObject deathData ) {

            int killerType = 0;
            List<dynamic> deathCoords = new List<dynamic>();
            uint weaponHash = 0;
            foreach( var data in deathData ) {
                if( data.Key == "killertype" ) {
                    killerType = (int)data.Value;
                }
                if( data.Key == "killerpos" ) {
                    deathCoords = data.Value as List<dynamic>;
                }
                if( data.Key == "weaponhash" ) {
                    weaponHash = (uint)data.Value;
                }
            }

            Vector3 DeathCoords = new Vector3( (float)deathCoords[0], (float)deathCoords[1], (float)deathCoords[2] );

            if( killerID > -1 ) {
                if( ServerGlobals.CurrentGame != null )
                    ServerGlobals.CurrentGame.OnPlayerKilled( ply, ServerGlobals.CurrentGame.GetPlayer( GetPlayerFromIndex( killerID ) ), DeathCoords, weaponHash );
            } else {
                ServerGlobals.CurrentGame.OnPlayerKilled( ply, null, DeathCoords, weaponHash );
            }

        }

        private void PlayerDied( [FromSource] Player ply, int killerType, List<dynamic> deathcords ) {
            Vector3 coords = new Vector3( (float)deathcords[0], (float)deathcords[1], (float)deathcords[2] );
            if( ServerGlobals.CurrentGame != null )
                ServerGlobals.CurrentGame.OnPlayerDied( ply, killerType, coords );
            
        }


        void OpenMapGUI( [FromSource] Player ply ) {
            foreach( ServerMap map in MapManager.Maps ) {
                ply.TriggerEvent( "salty:CacheMap", map.ID, map.Name, string.Join(",", map.Gamemodes), map.Position, map.Size, map.SpawnsAsSendable() );
            }
            ply.TriggerEvent( "salty:OpenMapGUI" );
        }



        public static void StartGame( string ID ) {
            ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes[ID.ToLower()].GetType() );
            ServerGlobals.CurrentGame.GameTime = GetGameTimer() + ServerGlobals.CurrentGame.Settings.GameLength;
            ServerGlobals.CurrentGame.Map = MapManager.FindMap( ID );
            ServerGlobals.CurrentGame.Start();
            BaseGamemode.WriteChat( ID.ToUpper(), "Playing map " + ServerGlobals.CurrentGame.Map.Name, 200, 30, 30 );
        }



    }
}
