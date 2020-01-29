using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityServer {
    class Map {

        public Vector3 Position;
        public Vector3 Size;
        public string Name;
        public int ID;
        public List<string> Gamemodes = new List<string>();

        List<Spawn> Spawns = new List<Spawn>();

        public Map( int id, string name, string gamemode, float posX, float posY, float posZ, float sizeX, float sizeY, float sizeZ ) {
            Debug.WriteLine( "Map loaded with ID " + id );
            ID = id;
            Name = name;
            Gamemodes = gamemode.Split( ',' ).ToList();
            Position = new Vector3( posX, posY, posZ );
            Size = new Vector3( sizeX, sizeY, sizeZ );
        }

        public Map( int id, string name, string gamemode, Vector3 position, Vector3 size ) {
            ID = id;
            Name = name;
            Gamemodes = gamemode.Split( ',' ).ToList();
            Position = position;
            Size = size;
        }

        public bool IsInZone( Vector3 pos ) {
            return (pos.X > Position.X - (Size.X / 2) && pos.X < Position.X + (Size.X / 2) && pos.Y > Position.Y - (Size.Y / 2) && pos.Y < Position.Y + (Size.Y / 2));
        }

    }
}
