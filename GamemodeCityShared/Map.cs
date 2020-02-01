using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityShared {
    public class Map {

        public Vector3 Position;
        public Vector3 Size;
        public string Name;
        public int ID;
        public List<string> Gamemodes = new List<string>();

        public List<Spawn> Spawns = new List<Spawn>();

        public Map( int id, string name, List<string> gamemode, Vector3 position, Vector3 size ) {
            ID = id;
            Name = name;
            Gamemodes = gamemode;
            Position = position;
            Size = size;
        }

        public bool IsInZone( Vector3 pos ) {
            return (pos.X > Position.X - (Size.X / 2) && pos.X < Position.X + (Size.X / 2) && pos.Y > Position.Y - (Size.Y / 2) && pos.Y < Position.Y + (Size.Y / 2));
        }

        public List<IDictionary<string,dynamic>> SpawnsAsSendable() {
            var spawns = new List<IDictionary<string, dynamic>>();
            foreach( var spawn in Spawns ) {
                spawns.Add( spawn.SpawnAsSendable() );
            }
            return spawns;
        }

    }
}
