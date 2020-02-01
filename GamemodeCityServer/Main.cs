using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace GamemodeCityServer
{
    public class Main : BaseScript
    {

        BaseGamemode CurrentGame;
        MapManager MapManager;
        Database Database;

        public Main() {

            MapManager = new MapManager();
            Database = new Database( MapManager );

            EventHandlers["salty:netStartGame"] += new Action<Player, int>(StartGame);
            EventHandlers["salty:netOpenMapGUI"] += new Action<Player>( OpenMapGUI );

            EventHandlers["saltyMap:netUpdate"] += new Action<Player, ExpandoObject>( MapManager.Update );


        }


        void OpenMapGUI( [FromSource] Player ply ) {
            foreach( ServerMap map in MapManager.Maps ) {
                ply.TriggerEvent( "salty:CacheMap", map.ID, map.Name, map.Gamemodes, map.Position, map.Size, map.SpawnsAsSendable() );
            }
            ply.TriggerEvent( "salty:OpenMapGUI" );
        }

        public void StartGame( [FromSource] Player ply, int ID ) {
            TriggerClientEvent("salty:StartGame", ID);
        }



    }
}
