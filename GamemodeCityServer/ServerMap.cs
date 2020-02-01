using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityServer {
    class ServerMap : Map {

        
        public ServerMap( int id, string name, List<string> gamemode, Vector3 position, Vector3 size ) : base ( id, name, gamemode, position, size ) {
            
        }

    }
}
