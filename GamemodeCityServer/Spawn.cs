using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityServer {
    public class Spawn : BaseScript {

        

        public Vector3 Position;
        public SpawnType SpawnType;
        public string Entity;

        public Spawn( Vector3 position, SpawnType type ) {
            Position = position;
            SpawnType = type;
        }

        public Spawn( Vector3 position, SpawnType type, string entName ) {
            Position = position;
            SpawnType = type;
            Entity = entName;
        }
    }

    public enum SpawnType {
        PLAYER,
        WEAPON,
        OBJECT
    }
}
