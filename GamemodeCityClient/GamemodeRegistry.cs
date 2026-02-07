using System.Collections.Generic;

namespace GamemodeCityClient {

    public class GamemodeInfo {
        public string Id;
        public string Name;
        public string Description;
        public string Color;

        public GamemodeInfo( string id, string name, string description, string color ) {
            Id = id;
            Name = name;
            Description = description;
            Color = color;
        }
    }

    public static class GamemodeRegistry {

        private static Dictionary<string, GamemodeInfo> Registry = new Dictionary<string, GamemodeInfo>();

        public static void Register( string id, string name, string description, string color ) {
            Registry[id] = new GamemodeInfo( id, name, description, color );
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
                    "\",\"hasControls\":" + ( hasControls ? "true" : "false" ) + "}"
                );
            }
            return "[" + string.Join( ",", entries ) + "]";
        }
    }
}
