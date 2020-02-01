using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityShared {
    public class Spawn : BaseScript {

        
        public Vector3 Position;
        public SpawnType SpawnType;
        public string Entity;
        public int Team;

        public Spawn( Vector3 position, SpawnType type ) {
            Position = position;
            SpawnType = type;
        }

        public Spawn( Vector3 position, SpawnType type, string entName, int team ) {
            Position = position;
            SpawnType = type;
            Entity = entName;
            Team = team;
        }

        public IDictionary<string,dynamic> SpawnAsSendable() {
            return new Dictionary<string, dynamic> { { "position", Position }, { "spawntype", (int)SpawnType }, { "entity", Entity }, { "team", Team } };
        }

        public static Spawn SpawnRecieved( IDictionary<string, dynamic> spawn ) {
            return new Spawn( spawn["position"], spawn["spawntype"], spawn["entity"], spawn["team"] );
        }

    }

    public enum SpawnType {
        PLAYER,
        WEAPON,
        OBJECT
    }
}
