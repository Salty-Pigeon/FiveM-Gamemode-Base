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

        MapManager MapManager;
        Database Database;

        public Main() {

            MapManager = new MapManager();
            Database = new Database( MapManager );

            EventHandlers["salty:netStartGame"] += new Action<Player, string>(StartGame);
            EventHandlers["salty:netOpenMapGUI"] += new Action<Player>( OpenMapGUI );

            EventHandlers["saltyMap:netUpdate"] += new Action<Player, ExpandoObject>( MapManager.Update );

            EventHandlers["baseevents:onPlayerKilled"] += new Action<Player, int, ExpandoObject>( PlayerKilled );
            EventHandlers["baseevents:onPlayerDied"] += new Action<Player, int, List<dynamic>>( PlayerDied );

            base.Tick += Tick;

        }

        private async Task Tick() {
            if( ServerGlobals.CurrentGame != null ) {
                ServerGlobals.CurrentGame.Update();
               if( ServerGlobals.CurrentGame.GameTime < GetGameTimer() ) {
                    ServerGlobals.CurrentGame.End();
                    ServerGlobals.CurrentGame = null;
               }
            }
        }

        private void PlayerKilled( [FromSource] Player ply, int killerID, ExpandoObject deathData ) {

            int killerType = 0;
            List<dynamic> deathCoords = new List<dynamic>();
            foreach( var data in deathData ) {
                if( data.Key == "killertype" ) {
                    killerType = (int)data.Value;
                }
                if( data.Key == "killerpos" ) {
                    deathCoords = data.Value as List<dynamic>;
                }
            }

            if( killerID > -1 ) {
                Debug.WriteLine("Killer is " + GetPlayerName( GetPlayerFromIndex( killerID ) ) );
                ServerGlobals.CurrentGame.OnPlayerKilled( ply, GetPlayerFromIndex( killerID ) );
            }

        }

        private void PlayerDied( [FromSource] Player ply, int killerType, List<dynamic> deathcords ) {
            Vector3 coords = new Vector3( (float)deathcords[0], (float)deathcords[1], (float)deathcords[2] );
            ServerGlobals.CurrentGame.OnPlayerDied( ply, killerType, coords );
            
        }


        void OpenMapGUI( [FromSource] Player ply ) {
            foreach( ServerMap map in MapManager.Maps ) {
                ply.TriggerEvent( "salty:CacheMap", map.ID, map.Name, string.Join(",", map.Gamemodes), map.Position, map.Size, map.SpawnsAsSendable() );
            }
            ply.TriggerEvent( "salty:OpenMapGUI" );
        }



        public void StartGame( [FromSource] Player ply, string ID ) {
            ServerGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ServerGlobals.Gamemodes[ID.ToLower()].GetType() );
            ServerGlobals.CurrentGame.Map = MapManager.FindMap( ID );
            ServerGlobals.CurrentGame.Start();
            ServerGlobals.WriteChat( ID.ToUpper(), "Playing map " + ServerGlobals.CurrentGame.Map.Name, 200, 30, 30 );
        }



    }
}
