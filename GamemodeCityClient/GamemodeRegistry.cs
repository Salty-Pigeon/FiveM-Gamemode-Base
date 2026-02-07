using System.Collections.Generic;

namespace GamemodeCityClient {

    public class GamemodeInfo {
        public string Id;
        public string Name;
        public string Description;
        public string Color;
        public int MinPlayers = 0;
        public int MaxPlayers = 0;
        public string[] Tags = new string[0];
        public string[] Teams = new string[0];
        public string[] Features = new string[0];

        public GamemodeInfo( string id, string name, string description, string color ) {
            Id = id;
            Name = name;
            Description = description;
            Color = color;
        }
    }

    public static class GamemodeRegistry {

        private static Dictionary<string, GamemodeInfo> Registry = new Dictionary<string, GamemodeInfo>();

        public static GamemodeInfo Register( string id, string name, string description, string color ) {
            var info = new GamemodeInfo( id, name, description, color );
            Registry[id] = info;
            return info;
        }

        public static GamemodeInfo Get( string id ) {
            if( Registry.ContainsKey( id ) ) return Registry[id];
            return null;
        }

        public static Dictionary<string, GamemodeInfo> GetAll() {
            return Registry;
        }

        private static string EscapeJson( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
        }

        private static string BuildJsonArray( string[] items ) {
            var escaped = new List<string>();
            foreach( var item in items ) {
                escaped.Add( "\"" + EscapeJson( item ) + "\"" );
            }
            return "[" + string.Join( ",", escaped ) + "]";
        }

        public static string BuildAllGamemodesJson() {
            var entries = new List<string>();
            foreach( var kvp in Registry ) {
                var info = kvp.Value;
                bool hasControls = ControlConfig.GetActions( info.Id ).Count > 0;
                entries.Add(
                    "{\"id\":\"" + EscapeJson( info.Id ) +
                    "\",\"name\":\"" + EscapeJson( info.Name ) +
                    "\",\"description\":\"" + EscapeJson( info.Description ) +
                    "\",\"color\":\"" + EscapeJson( info.Color ) +
                    "\",\"hasControls\":" + ( hasControls ? "true" : "false" ) +
                    ",\"minPlayers\":" + info.MinPlayers +
                    ",\"maxPlayers\":" + info.MaxPlayers +
                    ",\"tags\":" + BuildJsonArray( info.Tags ) +
                    ",\"teams\":" + BuildJsonArray( info.Teams ) +
                    ",\"features\":" + BuildJsonArray( info.Features ) + "}"
                );
            }
            return "[" + string.Join( ",", entries ) + "]";
        }
    }
}
