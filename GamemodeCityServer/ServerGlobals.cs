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
    public class ServerGlobals : BaseScript {
        public static Dictionary<string, BaseGamemode> Gamemodes = new Dictionary<string, BaseGamemode>();

        public static BaseGamemode CurrentGame;

        public static int CurrentRound = 0;

        public static void SpawnPlayer( Spawn spawn, Player player ) {

        }

        public static Dictionary<string, string> GamemodeList() {
            return Gamemodes.ToDictionary( x => x.Key, x => x.Value.Settings.Name );
        }

    }


}
