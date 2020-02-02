using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace GamemodeCityShared {
    public class Map : BaseScript {

        public Vector3 Position;
        public Vector3 Size;
        public string Name;
        public int ID;
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
            return (pos.X > Position.X - (Size.X / 2) && pos.X < Position.X + (Size.X / 2) && pos.Y > Position.Y - (Size.Y / 2) && pos.Y < Position.Y + (Size.Y / 2));
        }

        public Spawn GetSpawn( SpawnType type, int team ) {
            List<Spawn> shuffledSpawns = Spawns.OrderBy( a => Guid.NewGuid() ).ToList();
            foreach( var spawn in shuffledSpawns ) {
                if( (spawn.Team == team && type == SpawnType.PLAYER) && spawn.SpawnType == type ) {
                    return spawn;
                }
            }
            return new Spawn( -1, new Vector3( 0, 0, 0 ), SpawnType.PLAYER, "random", 0 );
        }

        public List<Spawn> GetSpawns( SpawnType type ) {
            return Spawns.Select( x => x ).Where( x => x.SpawnType == type ).ToList();
        }

        public List<Dictionary<string,dynamic>> SpawnsAsSendable() {
            var spawns = new List<Dictionary<string, dynamic>>();
            foreach( var spawn in Spawns ) {
                spawns.Add( spawn.SpawnAsSendable() );
            }
            return spawns;
        }


        public List<Spawn> SpawnsFromSendable( dynamic spawns ) {
            List<Spawn> spawnList = new List<Spawn>();

            foreach( ExpandoObject spawn in spawns as List<dynamic> ) {

                int id = -1;
                Vector3 position = new Vector3( 0, 0, 0 );
                int spawnType = 0;
                string spawnItem = "";
                int team = 0;

                Dictionary<string, dynamic> spawnData = new Dictionary<string, dynamic>();
                foreach( var data in spawn as IDictionary<string, dynamic> ) {

                    if( data.Key == "id" ) {
                        id = (int)data.Value;
                    }

                    if( data.Key == "position" ) {
                        position = (Vector3)data.Value;
                    }

                    if( data.Key == "spawntype" ) {
                        spawnType = (int)data.Value;
                    }

                    if( data.Key == "spawnitem" ) {
                        spawnItem = (string)data.Value;
                    }

                    if( data.Key == "team" ) {
                        team = (int)data.Value;
                    }
                }

                Spawn spawnPoint = new Spawn( id, position, (SpawnType)spawnType, spawnItem, team );
                spawnList.Add( spawnPoint );

            }
            Spawns = spawnList;
            return spawnList;
        }

    }
}
