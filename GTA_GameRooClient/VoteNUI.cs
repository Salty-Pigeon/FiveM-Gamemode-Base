using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GTA_GameRooClient {
    public class VoteNUI : BaseScript {

        private static VoteNUI Instance;
        private static bool isOpen = false;
        public static bool IsOpen => isOpen;

        public VoteNUI() {
            Instance = this;
        }

        private static string EscapeJson( string s ) {
            if( s == null ) return "";
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ).Replace( "\n", "\\n" ).Replace( "\r", "" );
        }

        private static string F( float v ) {
            return v.ToString( CultureInfo.InvariantCulture );
        }

        public static void OpenVote( float durationSeconds ) {
            isOpen = true;

            // Build gamemodes JSON array from registry
            var allGamemodes = GamemodeRegistry.GetAll();
            var entries = new List<string>();
            foreach( var kvp in allGamemodes ) {
                var gm = kvp.Value;
                var tags = new List<string>();
                foreach( var tag in gm.Tags ) {
                    tags.Add( "\"" + EscapeJson( tag ) + "\"" );
                }
                entries.Add( "{\"id\":\"" + EscapeJson( kvp.Key ) + "\""
                    + ",\"name\":\"" + EscapeJson( gm.Name ) + "\""
                    + ",\"description\":\"" + EscapeJson( gm.Description ) + "\""
                    + ",\"color\":\"" + EscapeJson( gm.Color ) + "\""
                    + ",\"minPlayers\":" + gm.MinPlayers
                    + ",\"maxPlayers\":" + gm.MaxPlayers
                    + ",\"tags\":[" + string.Join( ",", tags ) + "]}" );
            }
            string gamemodesJson = "[" + string.Join( ",", entries ) + "]";

            string payload = "{\"type\":\"openVote\",\"gamemodes\":" + gamemodesJson + ",\"duration\":" + F( durationSeconds ) + "}";
            SendNuiMessage( payload );

            // Open the hub to the vote tab
            HubNUI.OpenHubToVote();
        }

        public static void CloseVote() {
            isOpen = false;
            SendNuiMessage( "{\"type\":\"closeVote\"}" );
        }

        public static void UpdateVotes( string votesJson ) {
            SendNuiMessage( "{\"type\":\"updateVotes\",\"votes\":" + votesJson + "}" );
        }

        public static void ShowWinner( string winnerId ) {
            isOpen = false;
            SendNuiMessage( "{\"type\":\"voteWinner\",\"winnerId\":\"" + EscapeJson( winnerId ) + "\"}" );
        }
    }
}
