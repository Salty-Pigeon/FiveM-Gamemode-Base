using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
namespace GTA_GameRooShared {
    public class Map : BaseScript {

        public Vector3 Position;
        public Vector3 Size;
        public float Rotation = 0f; // degrees around Z axis
        public string Name;
        public int ID;
        public string Author = "";
        public string Description = "";
        public bool Enabled = true;
        public int MinPlayers = 2;
        public int MaxPlayers = 32;
        public List<string> Gamemodes = new List<string>();
        public List<Spawn> Spawns = new List<Spawn>();
        public bool JustCreated = false;

        public Map( int id, string name, List<string> gamemode, Vector3 position, Vector3 size ) {
            ID = id;
            Name = name;
            Gamemodes = gamemode;
            Position = position;
            Size = size;
        }

        public bool IsInZone( Vector3 pos ) {
            // Rotate the point into local (unrotated) space around the center
            float rad = -Rotation * ((float)Math.PI / 180f);
            float cos = (float)Math.Cos( rad );
            float sin = (float)Math.Sin( rad );
            float dx = pos.X - Position.X;
            float dy = pos.Y - Position.Y;
            float localX = dx * cos - dy * sin;
            float localY = dx * sin + dy * cos;

            bool inXY = localX > -(Size.X / 2) && localX < (Size.X / 2) &&
                        localY > -(Size.Y / 2) && localY < (Size.Y / 2);

            if( Size.Z > 0 ) {
                return inXY &&
                       pos.Z > Position.Z - (Size.Z / 2) &&
                       pos.Z < Position.Z + (Size.Z / 2);
            }

            return inXY;
        }

        public Spawn GetSpawn( SpawnType type, int team ) {
            List<Spawn> shuffledSpawns = Spawns.OrderBy( a => Guid.NewGuid() ).ToList();
            Spawn fallback = null;
            foreach( var spawn in shuffledSpawns ) {
                if( spawn.SpawnType == type ) {
                    if( type == SpawnType.PLAYER && spawn.Team == team ) {
                        return spawn;
                    }
                    // Keep any matching type as fallback
                    if( fallback == null ) fallback = spawn;
                }
            }
            // Return any spawn of the right type if team-specific wasn't found
            if( fallback != null ) return fallback;
            return new Spawn( -1, Position, SpawnType.PLAYER, "random", 0 );
        }

        public List<Spawn> GetSpawns( SpawnType type ) {
            return Spawns.Where( x => x.SpawnType == type ).ToList();
        }

        public List<Spawn> GetWinBarriers() {
            return Spawns.Where( x => x.SpawnType == SpawnType.WIN_BARRIER ).ToList();
        }

        public MapData ToMapData() {
            var data = new MapData {
                Id = ID,
                Name = Name,
                Author = Author,
                Description = Description,
                Enabled = Enabled,
                PosX = Position.X,
                PosY = Position.Y,
                PosZ = Position.Z,
                SizeX = Size.X,
                SizeY = Size.Y,
                SizeZ = Size.Z,
                Rotation = Rotation,
                Gamemodes = new List<string>( Gamemodes ),
                MinPlayers = MinPlayers,
                MaxPlayers = MaxPlayers,
                Spawns = new List<SpawnData>()
            };

            foreach( var spawn in Spawns ) {
                data.Spawns.Add( spawn.ToSpawnData() );
            }

            return data;
        }

        public void LoadFromMapData( MapData data ) {
            ID = data.Id;
            Name = data.Name;
            Author = data.Author;
            Description = data.Description;
            Enabled = data.Enabled;
            Position = new Vector3( data.PosX, data.PosY, data.PosZ );
            Size = new Vector3( data.SizeX, data.SizeY, data.SizeZ );
            Rotation = data.Rotation;
            Gamemodes = new List<string>( data.Gamemodes );
            MinPlayers = data.MinPlayers;
            MaxPlayers = data.MaxPlayers;

            Spawns = new List<Spawn>();
            foreach( var spawnData in data.Spawns ) {
                Spawns.Add( Spawn.FromSpawnData( spawnData ) );
            }
        }

        public string ToJson() {
            return SimpleJson.Serialize( ToMapData() );
        }

        public static MapData FromJson( string json ) {
            return SimpleJson.Deserialize( json );
        }
    }
}
