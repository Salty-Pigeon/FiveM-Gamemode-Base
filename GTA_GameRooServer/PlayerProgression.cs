using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CitizenFX.Core;
using GTA_GameRooShared;

namespace GTA_GameRooServer {
    public class PlayerProgression : BaseScript {

        private static Dictionary<string, PlayerData> Cache = new Dictionary<string, PlayerData>();

        private static float _previewPedX = 0;
        private static float _previewPedY = 0;
        private static float _previewPedZ = 0;
        private static float _previewPedH = 10f;
        private static bool _previewPedPosLoaded = false;

        public PlayerProgression() {
            PlayerDatabase.EnsureTable();
            LoadPreviewPedPos();

            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>( OnPlayerConnecting );
            EventHandlers["playerDropped"] += new Action<Player, string>( OnPlayerDropped );
            EventHandlers["salty:requestProgression"] += new Action<Player>( OnRequestProgression );
            EventHandlers["salty:purchaseModel"] += new Action<Player, string>( OnPurchaseModel );
            EventHandlers["salty:selectModel"] += new Action<Player, string>( OnSelectModel );
            EventHandlers["salty:awardXP"] += new Action<Player, int>( OnAwardXP );
            EventHandlers["salty:purchaseItem"] += new Action<Player, string>( OnPurchaseItem );
            EventHandlers["salty:saveAppearance"] += new Action<Player, string>( OnSaveAppearance );
            EventHandlers["salty:setAdminLevel"] += new Action<Player, string, int>( OnSetAdminLevel );
            EventHandlers["salty:getOnlinePlayers"] += new Action<Player>( OnGetOnlinePlayers );
            EventHandlers["salty:lookupPlayer"] += new Action<Player, string>( OnLookupPlayer );
            EventHandlers["salty:setPreviewPedPos"] += new Action<Player, string>( OnSetPreviewPedPos );
            EventHandlers["salty:getPreviewPedPos"] += new Action<Player>( OnGetPreviewPedPos );
        }

        public static int GetAdminLevel( Player player ) {
            string license = GetLicense( player );
            if( Cache.ContainsKey( license ) ) return Cache[license].AdminLevel;
            return 0;
        }

        public static string GetLicense( Player player ) {
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
                bool isNew = Cache[license].IsNewPlayer;
                SendProgression( player, Cache[license], isNew );
                Cache[license].IsNewPlayer = false;
            }

            // Also send current preview ped position
            if( _previewPedPosLoaded ) {
                player.TriggerEvent( "salty:previewPedPos", BuildPedPosString() );
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
            int cost = AppearanceConstants.GetModelCost( modelHash );
            if( data.Tokens < cost ) return;

            data.Tokens -= cost;
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

            // Deduct tokens if cost > 0
            if( cost > 0 ) {
                if( data.Tokens < cost ) return;
                data.Tokens -= cost;
            }

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

        private static string BuildProgressionJson( PlayerData data, bool leveledUp, bool isNewPlayer = false ) {
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
                ",\"adminLevel\":" + data.AdminLevel +
                ",\"leveledUp\":" + ( leveledUp ? "true" : "false" ) +
                ",\"isNewPlayer\":" + ( isNewPlayer ? "true" : "false" ) + "}";
        }

        private static void SendProgression( Player player, PlayerData data, bool isNewPlayer = false ) {
            string json = BuildProgressionJson( data, false, isNewPlayer );
            player.TriggerEvent( "salty:receiveProgression", json );
        }

        private static void SendProgressionWithLevelUp( Player player, PlayerData data, bool leveledUp ) {
            string json = BuildProgressionJson( data, leveledUp );
            player.TriggerEvent( "salty:receiveProgression", json );
        }

        // ==================== Admin Events ====================

        private void OnSetAdminLevel( [FromSource] Player source, string targetLicense, int level ) {
            string sourceLicense = GetLicense( source );
            if( !Cache.ContainsKey( sourceLicense ) || Cache[sourceLicense].AdminLevel < 3 ) {
                source.TriggerEvent( "salty:adminResult", "{\"success\":false,\"message\":\"Access denied\"}" );
                return;
            }
            if( level < 0 || level > 2 ) {
                source.TriggerEvent( "salty:adminResult", "{\"success\":false,\"message\":\"Invalid level (0-2)\"}" );
                return;
            }

            // Update DB
            PlayerDatabase.SetAdminLevel( targetLicense, level );

            // Update cache if target is online
            if( Cache.ContainsKey( targetLicense ) ) {
                Cache[targetLicense].AdminLevel = level;
                // Find online player and send updated progression
                foreach( var p in Players ) {
                    if( GetLicense( p ) == targetLicense ) {
                        SendProgression( p, Cache[targetLicense] );
                        break;
                    }
                }
            }

            source.TriggerEvent( "salty:adminResult", "{\"success\":true,\"message\":\"Admin level set to " + level + "\"}" );
        }

        private void OnGetOnlinePlayers( [FromSource] Player source ) {
            string sourceLicense = GetLicense( source );
            if( !Cache.ContainsKey( sourceLicense ) || Cache[sourceLicense].AdminLevel < 3 ) return;

            var entries = new List<string>();
            foreach( var p in Players ) {
                string lic = GetLicense( p );
                int adminLvl = Cache.ContainsKey( lic ) ? Cache[lic].AdminLevel : 0;
                entries.Add( "{\"handle\":\"" + EscapeJson( p.Handle ) + "\",\"name\":\"" + EscapeJson( p.Name ) + "\",\"license\":\"" + EscapeJson( lic ) + "\",\"adminLevel\":" + adminLvl + "}" );
            }
            string json = "[" + string.Join( ",", entries ) + "]";
            source.TriggerEvent( "salty:onlinePlayers", json );
        }

        private void OnLookupPlayer( [FromSource] Player source, string license ) {
            string sourceLicense = GetLicense( source );
            if( !Cache.ContainsKey( sourceLicense ) || Cache[sourceLicense].AdminLevel < 3 ) return;

            var data = PlayerDatabase.LookupPlayer( license );
            if( data != null ) {
                source.TriggerEvent( "salty:lookupResult", "{\"found\":true,\"name\":\"" + EscapeJson( data.Name ) + "\",\"adminLevel\":" + data.AdminLevel + "}" );
            } else {
                source.TriggerEvent( "salty:lookupResult", "{\"found\":false,\"name\":\"\",\"adminLevel\":0}" );
            }
        }

        // ==================== Preview PED Position ====================

        private static string F( float v ) {
            return v.ToString( CultureInfo.InvariantCulture );
        }

        private void LoadPreviewPedPos() {
            string val = PlayerDatabase.GetSetting( "preview_ped_pos" );
            if( val != null ) {
                var parts = val.Split( ',' );
                if( parts.Length >= 3 ) {
                    float.TryParse( parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _previewPedX );
                    float.TryParse( parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _previewPedY );
                    float.TryParse( parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _previewPedZ );
                    if( parts.Length >= 4 ) float.TryParse( parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _previewPedH );
                    _previewPedPosLoaded = true;
                    Debug.WriteLine( "[GameRoo] Loaded preview PED pos: " + val );
                }
            }
        }

        private string BuildPedPosString() {
            return F( _previewPedX ) + "," + F( _previewPedY ) + "," + F( _previewPedZ ) + "," + F( _previewPedH );
        }

        private void OnSetPreviewPedPos( [FromSource] Player source, string posStr ) {
            if( GetAdminLevel( source ) < 2 ) return;

            var parts = posStr.Split( ',' );
            if( parts.Length < 3 ) return;

            float x, y, z, h = 180f;
            if( !float.TryParse( parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out x ) ) return;
            if( !float.TryParse( parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out y ) ) return;
            if( !float.TryParse( parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out z ) ) return;
            if( parts.Length >= 4 ) float.TryParse( parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out h );

            _previewPedX = x;
            _previewPedY = y;
            _previewPedZ = z;
            _previewPedH = h;
            _previewPedPosLoaded = true;

            PlayerDatabase.SetSetting( "preview_ped_pos", BuildPedPosString() );

            // Broadcast to all clients
            TriggerClientEvent( "salty:previewPedPos", BuildPedPosString() );
        }

        private void OnGetPreviewPedPos( [FromSource] Player source ) {
            if( _previewPedPosLoaded ) {
                source.TriggerEvent( "salty:previewPedPos", BuildPedPosString() );
            }
        }
    }
}
