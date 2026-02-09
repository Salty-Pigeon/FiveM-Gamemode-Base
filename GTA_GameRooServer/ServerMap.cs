using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using GTA_GameRooShared;

namespace GTA_GameRooServer {
    public class ServerMap : Map {

        public ServerMap( int id, string name, List<string> gamemode, Vector3 position, Vector3 size ) : base( id, name, gamemode, position, size ) {
        }

        public static ServerMap FromMapData( MapData data ) {
            var map = new ServerMap(
                data.Id,
                data.Name,
                new List<string>( data.Gamemodes ),
                new Vector3( data.PosX, data.PosY, data.PosZ ),
                new Vector3( data.SizeX, data.SizeY, data.SizeZ )
            );
            map.Author = data.Author;
            map.Description = data.Description;
            map.Enabled = data.Enabled;
            map.Rotation = data.Rotation;
            map.MinPlayers = data.MinPlayers;
            map.MaxPlayers = data.MaxPlayers;

            map.Spawns = new List<Spawn>();
            foreach( var spawnData in data.Spawns ) {
                map.Spawns.Add( Spawn.FromSpawnData( spawnData ) );
            }

            map.Vertices = new List<Vector2>();
            if( data.Vertices != null ) {
                foreach( var v in data.Vertices ) {
                    map.Vertices.Add( new Vector2( v.X, v.Y ) );
                }
            }

            if( map.Vertices.Count >= 3 ) {
                map.RecalculateCentroid();
            }

            return map;
        }

        public void SpawnGuns() {
            foreach( var spawn in GetSpawns( SpawnType.WEAPON ) ) {
                ServerGlobals.CurrentGame.SpawnWeapon( spawn.Position, GrabWeapon( false ) );
            }
        }

        public uint GrabWeapon( bool melee ) {
            uint wep = 0;
            if( !melee ) {
                wep = ServerGlobals.CurrentGame.Settings.Weapons.OrderBy( x => Guid.NewGuid() ).Where( x => Globals.Weapons[x]["Group"] != "GROUP_MELEE" && Globals.Weapons[x]["Group"] != "GROUP_UNARMED" ).First();
            }
            return wep;
        }
    }
}
