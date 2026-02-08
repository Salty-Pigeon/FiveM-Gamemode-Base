using System;
using System.Collections.Generic;

namespace GTA_GameRooClient {

    public class DebugAction {
        public string Id;
        public string Label;
        public string Category;
        public Action Callback;
        public Action<int> TargetCallback;
        public bool NeedsTarget;

        public DebugAction( string id, string label, string category, Action callback ) {
            Id = id;
            Label = label;
            Category = category;
            Callback = callback;
            NeedsTarget = false;
        }

        public DebugAction( string id, string label, string category, Action<int> targetCallback ) {
            Id = id;
            Label = label;
            Category = category;
            TargetCallback = targetCallback;
            NeedsTarget = true;
        }
    }

    public static class DebugRegistry {

        private static Dictionary<string, List<DebugAction>> Registry = new Dictionary<string, List<DebugAction>>();
        private static Dictionary<string, Func<string>> EntityProviders = new Dictionary<string, Func<string>>();

        public static void Register( string gamemodeId, string actionId, string label, string category, Action callback ) {
            if( !Registry.ContainsKey( gamemodeId ) ) {
                Registry[gamemodeId] = new List<DebugAction>();
            }
            Registry[gamemodeId].RemoveAll( a => a.Id == actionId );
            Registry[gamemodeId].Add( new DebugAction( actionId, label, category, callback ) );
        }

        public static void RegisterTargetAction( string gamemodeId, string actionId, string label, string category, Action<int> callback ) {
            if( !Registry.ContainsKey( gamemodeId ) ) {
                Registry[gamemodeId] = new List<DebugAction>();
            }
            Registry[gamemodeId].RemoveAll( a => a.Id == actionId );
            Registry[gamemodeId].Add( new DebugAction( actionId, label, category, callback ) );
        }

        public static void RegisterEntityProvider( string gamemodeId, Func<string> provider ) {
            EntityProviders[gamemodeId] = provider;
        }

        public static string BuildEntitiesJson( string gamemodeId ) {
            if( EntityProviders.ContainsKey( gamemodeId ) ) {
                return EntityProviders[gamemodeId]();
            }
            return "[]";
        }

        public static DebugAction GetAction( string gamemodeId, string actionId ) {
            if( !Registry.ContainsKey( gamemodeId ) ) return null;
            foreach( var action in Registry[gamemodeId] ) {
                if( action.Id == actionId ) return action;
            }
            return null;
        }

        private static string EscapeJson( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
        }

        public static string BuildDebugActionsJson() {
            var gamemodeEntries = new List<string>();
            foreach( var kvp in Registry ) {
                string gmId = kvp.Key;
                var actions = kvp.Value;

                // Group actions by category
                var categories = new Dictionary<string, List<DebugAction>>();
                foreach( var action in actions ) {
                    if( !categories.ContainsKey( action.Category ) ) {
                        categories[action.Category] = new List<DebugAction>();
                    }
                    categories[action.Category].Add( action );
                }

                // Build category entries
                var categoryEntries = new List<string>();
                foreach( var cat in categories ) {
                    var actionEntries = new List<string>();
                    foreach( var action in cat.Value ) {
                        actionEntries.Add( "{\"id\":\"" + EscapeJson( action.Id ) + "\",\"label\":\"" + EscapeJson( action.Label ) + "\",\"needsTarget\":" + ( action.NeedsTarget ? "true" : "false" ) + "}" );
                    }
                    categoryEntries.Add( "\"" + EscapeJson( cat.Key ) + "\":[" + string.Join( ",", actionEntries ) + "]" );
                }

                gamemodeEntries.Add( "\"" + EscapeJson( gmId ) + "\":{" + string.Join( ",", categoryEntries ) + "}" );
            }
            return "{" + string.Join( ",", gamemodeEntries ) + "}";
        }
    }
}
