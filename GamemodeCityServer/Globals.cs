using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class Globals : BaseScript {
        public static Dictionary<string, BaseGamemode> Gamemodes = new Dictionary<string, BaseGamemode>();


        public static void SpawnPlayer( Spawn spawn, Player player ) {

        }

        public static void WriteChat( string prefix, string str, int r, int g, int b ) {
            TriggerClientEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }

        public static void WriteChat( Player ply, string prefix, string str, int r, int g, int b ) {
            ply.TriggerEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }

    }


}
