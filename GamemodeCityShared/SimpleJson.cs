using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GamemodeCityShared {

    public static class SimpleJson {

        public static string Serialize( MapData map ) {
            var sb = new StringBuilder();
            sb.Append( "{" );
            sb.Append( JsonPair( "Id", map.Id ) ).Append( "," );
            sb.Append( JsonPair( "Name", map.Name ) ).Append( "," );
            sb.Append( JsonPair( "Author", map.Author ) ).Append( "," );
            sb.Append( JsonPair( "Description", map.Description ) ).Append( "," );
            sb.Append( JsonPair( "Enabled", map.Enabled ) ).Append( "," );
            sb.Append( JsonPair( "PosX", map.PosX ) ).Append( "," );
            sb.Append( JsonPair( "PosY", map.PosY ) ).Append( "," );
            sb.Append( JsonPair( "PosZ", map.PosZ ) ).Append( "," );
            sb.Append( JsonPair( "SizeX", map.SizeX ) ).Append( "," );
            sb.Append( JsonPair( "SizeY", map.SizeY ) ).Append( "," );
            sb.Append( JsonPair( "SizeZ", map.SizeZ ) ).Append( "," );
            sb.Append( JsonPair( "Rotation", map.Rotation ) ).Append( "," );
            sb.Append( JsonPair( "MinPlayers", map.MinPlayers ) ).Append( "," );
            sb.Append( JsonPair( "MaxPlayers", map.MaxPlayers ) ).Append( "," );

            // Gamemodes array
            sb.Append( "\"Gamemodes\":[" );
            for( int i = 0; i < map.Gamemodes.Count; i++ ) {
                if( i > 0 ) sb.Append( "," );
                sb.Append( JsonString( map.Gamemodes[i] ) );
            }
            sb.Append( "]," );

            // Spawns array
            sb.Append( "\"Spawns\":[" );
            for( int i = 0; i < map.Spawns.Count; i++ ) {
                if( i > 0 ) sb.Append( "," );
                sb.Append( SerializeSpawn( map.Spawns[i] ) );
            }
            sb.Append( "]" );

            sb.Append( "}" );
            return sb.ToString();
        }

        private static string SerializeSpawn( SpawnData spawn ) {
            var sb = new StringBuilder();
            sb.Append( "{" );
            sb.Append( JsonPair( "Id", spawn.Id ) ).Append( "," );
            sb.Append( JsonPair( "PosX", spawn.PosX ) ).Append( "," );
            sb.Append( JsonPair( "PosY", spawn.PosY ) ).Append( "," );
            sb.Append( JsonPair( "PosZ", spawn.PosZ ) ).Append( "," );
            sb.Append( JsonPair( "Heading", spawn.Heading ) ).Append( "," );
            sb.Append( JsonPair( "SpawnType", spawn.SpawnType ) ).Append( "," );
            sb.Append( JsonPair( "Entity", spawn.Entity ) ).Append( "," );
            sb.Append( JsonPair( "Team", spawn.Team ) );
            sb.Append( "}" );
            return sb.ToString();
        }

        public static MapData Deserialize( string json ) {
            var data = new MapData();
            var dict = ParseObject( json.Trim() );

            if( dict.ContainsKey( "Id" ) ) data.Id = ToInt( dict["Id"] );
            if( dict.ContainsKey( "Name" ) ) data.Name = Unquote( dict["Name"] );
            if( dict.ContainsKey( "Author" ) ) data.Author = Unquote( dict["Author"] );
            if( dict.ContainsKey( "Description" ) ) data.Description = Unquote( dict["Description"] );
            if( dict.ContainsKey( "Enabled" ) ) data.Enabled = ToBool( dict["Enabled"] );
            if( dict.ContainsKey( "PosX" ) ) data.PosX = ToFloat( dict["PosX"] );
            if( dict.ContainsKey( "PosY" ) ) data.PosY = ToFloat( dict["PosY"] );
            if( dict.ContainsKey( "PosZ" ) ) data.PosZ = ToFloat( dict["PosZ"] );
            if( dict.ContainsKey( "SizeX" ) ) data.SizeX = ToFloat( dict["SizeX"] );
            if( dict.ContainsKey( "SizeY" ) ) data.SizeY = ToFloat( dict["SizeY"] );
            if( dict.ContainsKey( "SizeZ" ) ) data.SizeZ = ToFloat( dict["SizeZ"] );
            if( dict.ContainsKey( "Rotation" ) ) data.Rotation = ToFloat( dict["Rotation"] );
            if( dict.ContainsKey( "MinPlayers" ) ) data.MinPlayers = ToInt( dict["MinPlayers"] );
            if( dict.ContainsKey( "MaxPlayers" ) ) data.MaxPlayers = ToInt( dict["MaxPlayers"] );

            if( dict.ContainsKey( "Gamemodes" ) ) {
                data.Gamemodes = ParseStringArray( dict["Gamemodes"] );
            }

            if( dict.ContainsKey( "Spawns" ) ) {
                data.Spawns = ParseSpawns( dict["Spawns"] );
            }

            return data;
        }

        private static List<SpawnData> ParseSpawns( string arrayJson ) {
            var spawns = new List<SpawnData>();
            var items = SplitArray( arrayJson );
            foreach( var item in items ) {
                var trimmed = item.Trim();
                if( trimmed.Length < 2 ) continue;
                var sDict = ParseObject( trimmed );
                var spawn = new SpawnData();
                if( sDict.ContainsKey( "Id" ) ) spawn.Id = ToInt( sDict["Id"] );
                if( sDict.ContainsKey( "PosX" ) ) spawn.PosX = ToFloat( sDict["PosX"] );
                if( sDict.ContainsKey( "PosY" ) ) spawn.PosY = ToFloat( sDict["PosY"] );
                if( sDict.ContainsKey( "PosZ" ) ) spawn.PosZ = ToFloat( sDict["PosZ"] );
                if( sDict.ContainsKey( "Heading" ) ) spawn.Heading = ToFloat( sDict["Heading"] );
                if( sDict.ContainsKey( "SpawnType" ) ) spawn.SpawnType = ToInt( sDict["SpawnType"] );
                if( sDict.ContainsKey( "Entity" ) ) spawn.Entity = Unquote( sDict["Entity"] );
                if( sDict.ContainsKey( "Team" ) ) spawn.Team = ToInt( sDict["Team"] );
                spawns.Add( spawn );
            }
            return spawns;
        }

        private static List<string> ParseStringArray( string arrayJson ) {
            var result = new List<string>();
            var trimmed = arrayJson.Trim();
            if( trimmed.Length < 2 ) return result;
            // Remove [ ]
            trimmed = trimmed.Substring( 1, trimmed.Length - 2 ).Trim();
            if( trimmed.Length == 0 ) return result;

            var items = SplitTopLevel( trimmed, ',' );
            foreach( var item in items ) {
                result.Add( Unquote( item.Trim() ) );
            }
            return result;
        }

        private static List<string> SplitArray( string arrayJson ) {
            var trimmed = arrayJson.Trim();
            if( trimmed.Length < 2 ) return new List<string>();
            // Remove [ ]
            trimmed = trimmed.Substring( 1, trimmed.Length - 2 ).Trim();
            if( trimmed.Length == 0 ) return new List<string>();
            return SplitTopLevel( trimmed, ',' );
        }

        private static Dictionary<string, string> ParseObject( string json ) {
            var dict = new Dictionary<string, string>();
            if( json.Length < 2 ) return dict;

            // Remove { }
            json = json.Substring( 1, json.Length - 2 ).Trim();
            if( json.Length == 0 ) return dict;

            var pairs = SplitTopLevel( json, ',' );
            foreach( var pair in pairs ) {
                int colonIdx = FindTopLevelColon( pair );
                if( colonIdx < 0 ) continue;

                string key = Unquote( pair.Substring( 0, colonIdx ).Trim() );
                string value = pair.Substring( colonIdx + 1 ).Trim();
                dict[key] = value;
            }

            return dict;
        }

        private static int FindTopLevelColon( string s ) {
            int depth = 0;
            bool inString = false;
            for( int i = 0; i < s.Length; i++ ) {
                char c = s[i];
                if( c == '"' && (i == 0 || s[i - 1] != '\\') ) inString = !inString;
                if( inString ) continue;
                if( c == '{' || c == '[' ) depth++;
                if( c == '}' || c == ']' ) depth--;
                if( c == ':' && depth == 0 ) return i;
            }
            return -1;
        }

        private static List<string> SplitTopLevel( string s, char delimiter ) {
            var result = new List<string>();
            int depth = 0;
            bool inString = false;
            int start = 0;

            for( int i = 0; i < s.Length; i++ ) {
                char c = s[i];
                if( c == '"' && (i == 0 || s[i - 1] != '\\') ) inString = !inString;
                if( inString ) continue;
                if( c == '{' || c == '[' ) depth++;
                if( c == '}' || c == ']' ) depth--;
                if( c == delimiter && depth == 0 ) {
                    result.Add( s.Substring( start, i - start ) );
                    start = i + 1;
                }
            }
            if( start < s.Length ) {
                result.Add( s.Substring( start ) );
            }
            return result;
        }

        private static string JsonPair( string key, int value ) {
            return "\"" + key + "\":" + value;
        }

        private static string JsonPair( string key, float value ) {
            return "\"" + key + "\":" + value.ToString( CultureInfo.InvariantCulture );
        }

        private static string JsonPair( string key, bool value ) {
            return "\"" + key + "\":" + (value ? "true" : "false");
        }

        private static string JsonPair( string key, string value ) {
            return "\"" + key + "\":" + JsonString( value ?? "" );
        }

        private static string JsonString( string s ) {
            return "\"" + (s ?? "").Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ).Replace( "\n", "\\n" ).Replace( "\r", "\\r" ) + "\"";
        }

        private static string Unquote( string s ) {
            s = s.Trim();
            if( s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"' ) {
                s = s.Substring( 1, s.Length - 2 );
            }
            return s.Replace( "\\\"", "\"" ).Replace( "\\\\", "\\" ).Replace( "\\n", "\n" ).Replace( "\\r", "\r" );
        }

        private static int ToInt( string s ) {
            int result;
            int.TryParse( s.Trim(), out result );
            return result;
        }

        private static float ToFloat( string s ) {
            float result;
            float.TryParse( s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result );
            return result;
        }

        private static bool ToBool( string s ) {
            return s.Trim().ToLower() == "true";
        }
    }
}
