using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class MapStorage {

        private string MapsPath;

        public MapStorage() {
            MapsPath = Path.Combine( GetResourcePath( GetCurrentResourceName() ), "maps" );
            if( !Directory.Exists( MapsPath ) ) {
                Directory.CreateDirectory( MapsPath );
            }
        }

        public List<ServerMap> LoadAll() {
            var maps = new List<ServerMap>();
            if( !Directory.Exists( MapsPath ) ) return maps;

            foreach( var file in Directory.GetFiles( MapsPath, "*.json" ) ) {
                try {
                    string json = File.ReadAllText( file );
                    MapData data = SimpleJson.Deserialize( json );
                    if( data != null ) {
                        maps.Add( ServerMap.FromMapData( data ) );
                    }
                } catch( Exception ex ) {
                    Debug.WriteLine( $"[MapStorage] Failed to load map file {file}: {ex.Message}" );
                }
            }

            Debug.WriteLine( $"[MapStorage] Loaded {maps.Count} maps from disk." );
            return maps;
        }

        public void Save( ServerMap map ) {
            if( map.ID <= 0 ) {
                map.ID = GetNextId();
            }

            string sanitizedName = SanitizeFileName( map.Name );
            string fileName = $"{map.ID}_{sanitizedName}.json";
            string filePath = Path.Combine( MapsPath, fileName );

            // Remove old file if name changed
            CleanOldFiles( map.ID );

            MapData data = map.ToMapData();
            string json = SimpleJson.Serialize( data );
            File.WriteAllText( filePath, json );

            Debug.WriteLine( $"[MapStorage] Saved map '{map.Name}' (ID: {map.ID}) to {fileName}" );
        }

        public void Delete( int mapId ) {
            CleanOldFiles( mapId );
            Debug.WriteLine( $"[MapStorage] Deleted map ID: {mapId}" );
        }

        private int GetNextId() {
            int maxId = 0;
            if( !Directory.Exists( MapsPath ) ) return 1;

            foreach( var file in Directory.GetFiles( MapsPath, "*.json" ) ) {
                string name = Path.GetFileNameWithoutExtension( file );
                int underscoreIdx = name.IndexOf( '_' );
                if( underscoreIdx > 0 ) {
                    int id;
                    if( int.TryParse( name.Substring( 0, underscoreIdx ), out id ) ) {
                        if( id > maxId ) maxId = id;
                    }
                }
            }
            return maxId + 1;
        }

        private void CleanOldFiles( int mapId ) {
            if( !Directory.Exists( MapsPath ) ) return;

            string prefix = mapId + "_";
            foreach( var file in Directory.GetFiles( MapsPath, "*.json" ) ) {
                string name = Path.GetFileName( file );
                if( name.StartsWith( prefix ) ) {
                    File.Delete( file );
                }
            }
        }

        private string SanitizeFileName( string name ) {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach( char c in invalid ) {
                name = name.Replace( c, '_' );
            }
            return name.Replace( ' ', '_' ).ToLower();
        }
    }
}
