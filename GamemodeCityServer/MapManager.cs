using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class MapManager {

        public List<ServerMap> Maps = new List<ServerMap>();
        private MapStorage Storage;

        public MapManager() {
            Storage = new MapStorage();
            Maps = Storage.LoadAll();
        }

        public ServerMap FindMap( Vector3 pos ) {
            foreach( ServerMap map in Maps ) {
                if( map.IsInZone( pos ) )
                    return map;
            }
            return null;
        }

        public ServerMap FindMap( string gamemode ) {
            var eligible = Maps.Where( x => x.Enabled && x.Gamemodes.Contains( gamemode ) ).ToList();
            if( eligible.Count == 0 ) return null;
            return eligible.OrderBy( a => Guid.NewGuid() ).First();
        }

        public Dictionary<int, string> MapList() {
            return Maps.Where( x => x.Enabled ).ToDictionary( x => x.ID, x => x.Name );
        }

        public void Update( [FromSource] Player ply, string mapJson ) {
            if( PlayerProgression.GetAdminLevel( ply ) < 2 ) return;
            try {
                MapData data = SimpleJson.Deserialize( mapJson );
                if( data == null ) return;

                bool isCreate = data.Id <= 0;

                if( !isCreate ) {
                    ServerMap existing = Maps.Find( x => x.ID == data.Id );
                    if( existing != null ) {
                        existing.LoadFromMapData( data );
                        Storage.Save( existing );
                        Debug.WriteLine( $"[MapManager] Updated map '{existing.Name}' (ID: {existing.ID})" );
                        // Send updated map back to client so it has correct data
                        string updatedJson = existing.ToJson();
                        ply.TriggerEvent( "salty:CacheMap", updatedJson );
                    }
                } else {
                    ServerMap map = ServerMap.FromMapData( data );
                    Storage.Save( map );
                    Maps.Add( map );
                    Debug.WriteLine( $"[MapManager] Created map '{map.Name}' (ID: {map.ID})" );
                    // Send the new map back with its assigned ID
                    string newJson = map.ToJson();
                    ply.TriggerEvent( "salty:CacheMap", newJson );
                }
            } catch( Exception ex ) {
                Debug.WriteLine( $"[MapManager] Error updating map: {ex.Message}" );
            }
        }

        public void DeleteMap( [FromSource] Player ply, int mapId ) {
            if( PlayerProgression.GetAdminLevel( ply ) < 2 ) return;
            var map = Maps.Find( x => x.ID == mapId );
            if( map != null ) {
                Maps.Remove( map );
                Storage.Delete( mapId );
                Debug.WriteLine( $"[MapManager] Deleted map '{map.Name}' (ID: {mapId})" );
            }
        }
    }
}
