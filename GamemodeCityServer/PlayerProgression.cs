using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class PlayerProgression : BaseScript {

        private static Dictionary<string, PlayerData> Cache = new Dictionary<string, PlayerData>();

        public PlayerProgression() {
            PlayerDatabase.EnsureTable();

            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>( OnPlayerConnecting );
            EventHandlers["playerDropped"] += new Action<Player, string>( OnPlayerDropped );
            EventHandlers["salty:requestProgression"] += new Action<Player>( OnRequestProgression );
            EventHandlers["salty:purchaseModel"] += new Action<Player, string>( OnPurchaseModel );
            EventHandlers["salty:selectModel"] += new Action<Player, string>( OnSelectModel );
            EventHandlers["salty:awardXP"] += new Action<Player, int>( OnAwardXP );
            EventHandlers["salty:purchaseItem"] += new Action<Player, string>( OnPurchaseItem );
            EventHandlers["salty:saveAppearance"] += new Action<Player, string>( OnSaveAppearance );
        }

        private static string GetLicense( Player player ) {
            foreach( var id in player.Identifiers ) {
                if( id.StartsWith( "license:" ) ) {
                    return id;
                }
            }
            return "license:unknown_" + player.Handle;
        }

        private void OnPlayerConnecting( [FromSource] Player player, string playerName, dynamic setKickReason, dynamic deferrals ) {
            string license = GetLicense( player );
            var data = PlayerDatabase.LoadPlayer( license, playerName );
            if( data != null ) {
                Cache[license] = data;
            }
        }

        private void OnPlayerDropped( [FromSource] Player player, string reason ) {
            string license = GetLicense( player );
            if( Cache.ContainsKey( license ) ) {
                PlayerDatabase.SavePlayer( Cache[license] );
                Cache.Remove( license );
            }
        }

        private void OnRequestProgression( [FromSource] Player player ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) {
                // Player connected before cache was ready, load now
                var data = PlayerDatabase.LoadPlayer( license, player.Name );
                if( data != null ) {
                    Cache[license] = data;
                }
            }
            if( Cache.ContainsKey( license ) ) {
                SendProgression( player, Cache[license] );
            }
        }

        private void OnAwardXP( [FromSource] Player player, int amount ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) return;

            var data = Cache[license];
            data.XP += amount;

            int oldLevel = data.Level;

            // Check for level ups (flat 200 XP per level)
            while( data.XP >= 200 ) {
                data.XP -= 200;
                data.Level++;
                data.Tokens += 100;
            }

            PlayerDatabase.SavePlayer( data );
            SendProgressionWithLevelUp( player, data, data.Level > oldLevel );
        }

        /// <summary>
        /// Static helper for gamemodes to award XP to a player.
        /// </summary>
        public static void AwardXP( Player player, int amount ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) return;

            var data = Cache[license];
            data.XP += amount;

            int oldLevel = data.Level;

            // Check for level ups (flat 200 XP per level)
            while( data.XP >= 200 ) {
                data.XP -= 200;
                data.Level++;
                data.Tokens += 100;
            }

            PlayerDatabase.SavePlayer( data );
            SendProgressionWithLevelUp( player, data, data.Level > oldLevel );
        }

        private void OnPurchaseModel( [FromSource] Player player, string modelHash ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) return;

            var data = Cache[license];

            // Validate model exists
            if( !PedModels.All.Any( m => m.Hash == modelHash ) ) return;

            // Already owned
            if( data.UnlockedModels.Contains( modelHash ) ) return;

            // Not enough tokens
            if( data.Tokens < AppearanceConstants.ModelCost ) return;

            data.Tokens -= AppearanceConstants.ModelCost;
            data.UnlockedModels.Add( modelHash );
            PlayerDatabase.SavePlayer( data );
            SendProgression( player, data );
        }

        private void OnSelectModel( [FromSource] Player player, string modelHash ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) return;

            var data = Cache[license];

            // Must own the model
            if( !data.UnlockedModels.Contains( modelHash ) ) return;

            data.SelectedModel = modelHash;
            PlayerDatabase.SavePlayer( data );
            SendProgression( player, data );
        }

        private void OnPurchaseItem( [FromSource] Player player, string itemKey ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) return;

            var data = Cache[license];

            // Already owned
            if( data.UnlockedItems.Contains( itemKey ) ) return;

            // Determine cost from item key format
            int cost = 0;
            if( itemKey.StartsWith( "comp_" ) ) {
                cost = AppearanceConstants.ClothingCost;
            } else if( itemKey.StartsWith( "prop_" ) ) {
                cost = AppearanceConstants.PropCost;
            } else if( itemKey.StartsWith( "hair_" ) ) {
                cost = AppearanceConstants.HairStyleCost;
            } else {
                return; // Invalid format
            }

            // Not enough tokens
            if( data.Tokens < cost ) return;

            data.Tokens -= cost;
            data.UnlockedItems.Add( itemKey );
            PlayerDatabase.SavePlayer( data );
            SendProgression( player, data );
        }

        private void OnSaveAppearance( [FromSource] Player player, string appearanceJson ) {
            string license = GetLicense( player );
            if( !Cache.ContainsKey( license ) ) return;

            var data = Cache[license];
            data.AppearanceJson = appearanceJson;
            PlayerDatabase.SavePlayer( data );
        }

        private static string EscapeJson( string s ) {
            if( s == null ) return "";
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ).Replace( "\n", "\\n" ).Replace( "\r", "" );
        }

        private static string BuildProgressionJson( PlayerData data, bool leveledUp ) {
            var unlockedModels = new List<string>();
            foreach( var hash in data.UnlockedModels ) {
                unlockedModels.Add( "\"" + EscapeJson( hash ) + "\"" );
            }

            var unlockedItems = new List<string>();
            foreach( var item in data.UnlockedItems ) {
                unlockedItems.Add( "\"" + EscapeJson( item ) + "\"" );
            }

            return "{\"xp\":" + data.XP +
                ",\"level\":" + data.Level +
                ",\"tokens\":" + data.Tokens +
                ",\"unlockedModels\":[" + string.Join( ",", unlockedModels ) + "]" +
                ",\"selectedModel\":\"" + EscapeJson( data.SelectedModel ) + "\"" +
                ",\"appearanceJson\":" + ( data.AppearanceJson != null ? "\"" + EscapeJson( data.AppearanceJson ) + "\"" : "null" ) +
                ",\"unlockedItems\":[" + string.Join( ",", unlockedItems ) + "]" +
                ",\"leveledUp\":" + ( leveledUp ? "true" : "false" ) + "}";
        }

        private static void SendProgression( Player player, PlayerData data ) {
            string json = BuildProgressionJson( data, false );
            player.TriggerEvent( "salty:receiveProgression", json );
        }

        private static void SendProgressionWithLevelUp( Player player, PlayerData data, bool leveledUp ) {
            string json = BuildProgressionJson( data, leveledUp );
            player.TriggerEvent( "salty:receiveProgression", json );
        }
    }
}
