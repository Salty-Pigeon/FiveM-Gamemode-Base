using System.Collections.Generic;

namespace GamemodeCityClient {

    public class GuideTeamRole {
        public string Name;
        public string Color;
        public string Goal;
        public string[] Tips;
    }

    public class GuideSection {
        public string Overview;
        public string HowToWin;
        public string[] Rules;
        public GuideTeamRole[] TeamRoles;
        public string[] Tips;
    }

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
        public GuideSection Guide = null;

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

        private static string BuildGuideJson( GuideSection guide ) {
            if( guide == null ) return "null";
            var roles = new List<string>();
            if( guide.TeamRoles != null ) {
                foreach( var role in guide.TeamRoles ) {
                    roles.Add(
                        "{\"name\":\"" + EscapeJson( role.Name ) +
                        "\",\"color\":\"" + EscapeJson( role.Color ) +
                        "\",\"goal\":\"" + EscapeJson( role.Goal ) +
                        "\",\"tips\":" + BuildJsonArray( role.Tips ?? new string[0] ) + "}"
                    );
                }
            }
            return "{\"overview\":\"" + EscapeJson( guide.Overview ?? "" ) +
                "\",\"howToWin\":\"" + EscapeJson( guide.HowToWin ?? "" ) +
                "\",\"rules\":" + BuildJsonArray( guide.Rules ?? new string[0] ) +
                ",\"teamRoles\":[" + string.Join( ",", roles ) + "]" +
                ",\"tips\":" + BuildJsonArray( guide.Tips ?? new string[0] ) + "}";
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
                    ",\"features\":" + BuildJsonArray( info.Features ) +
                    ",\"guide\":" + BuildGuideJson( info.Guide ) + "}"
                );
            }
            return "[" + string.Join( ",", entries ) + "]";
        }
    }
}
