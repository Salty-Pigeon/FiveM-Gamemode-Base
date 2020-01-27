using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class Spawn : BaseScript {

        public enum Type {
            PLAYER,
            WEAPON,
            OBJECT
        }

        public Vector3 Position;
        public Type SpawnType;
        public string Entity;

        public Spawn( Vector3 position, Type type ) {
            Position = position;
            SpawnType = type;
        }

        public Spawn( Vector3 position, Type type, string entName ) {
            Position = position;
            SpawnType = type;
            Entity = entName;
        }
    }
}
