using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTA_GameRooServer {
    public class Vote : BaseScript {

        Action<object> winner;
        bool ended = false;

        Dictionary<string, object> PlayerVotes = new Dictionary<string, object>();
        Dictionary<string, string> PlayerNames = new Dictionary<string, string>();

        int durationMs;
        long voteEndTime;

        public Vote( Action<object> win, int durationMs = 30000 ) {
            winner = win;
            this.durationMs = durationMs;
            voteEndTime = (long)GetGameTimer() + durationMs;
        }

        public void MakeVote( Player player, object ID ) {
            string handle = player.Handle;
            PlayerVotes[handle] = ID;
            PlayerNames[handle] = player.Name;
            BroadcastVoteState();
        }

        public void Update() {
            if( !ended && GetGameTimer() >= voteEndTime ) {
                EndVote();
            }
        }

        public void BroadcastVoteState() {
            var grouped = new Dictionary<string, List<string>>();
            foreach( var kvp in PlayerVotes ) {
                string key = kvp.Value.ToString();
                if( !grouped.ContainsKey( key ) )
                    grouped[key] = new List<string>();
                string name = PlayerNames.ContainsKey( kvp.Key ) ? PlayerNames[kvp.Key] : kvp.Key;
                grouped[key].Add( name );
            }

            // Build JSON manually
            var entries = new List<string>();
            foreach( var kvp in grouped ) {
                var names = new List<string>();
                foreach( var n in kvp.Value ) {
                    names.Add( "\"" + EscapeJson( n ) + "\"" );
                }
                entries.Add( "\"" + EscapeJson( kvp.Key ) + "\":[" + string.Join( ",", names ) + "]" );
            }
            string json = "{" + string.Join( ",", entries ) + "}";
            TriggerClientEvent( "salty:VoteUpdate", json );
        }

        public void EndVote() {
            if( ended ) return;
            ended = true;
            object result = GetWinner();
            TriggerClientEvent( "salty:VoteEnd", result != null ? result.ToString() : "" );
            winner( result );
        }

        public object GetWinner() {
            if( PlayerVotes.Count == 0 ) return null;

            var tally = new Dictionary<string, int>();
            foreach( var kvp in PlayerVotes ) {
                string key = kvp.Value.ToString();
                if( !tally.ContainsKey( key ) )
                    tally[key] = 0;
                tally[key]++;
            }
            return tally.Aggregate( ( l, r ) => l.Value > r.Value ? l : r ).Key;
        }

        private static string EscapeJson( string s ) {
            if( s == null ) return "";
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ).Replace( "\n", "\\n" ).Replace( "\r", "" );
        }

    }
}
