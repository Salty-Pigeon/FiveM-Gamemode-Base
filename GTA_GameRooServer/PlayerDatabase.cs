using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using GTA_GameRooShared;

namespace GTA_GameRooServer {

    public class PlayerData {
        public string License;
        public string Name;
        public int XP;
        public int Level;
        public int Tokens;
        public List<string> UnlockedModels;
        public string SelectedModel;
        public string AppearanceJson;
        public List<string> UnlockedItems;
        public int AdminLevel;
        public bool IsNewPlayer;
    }

    public class PlayerDatabase {

        private static MySqlConnection _connection;
        private static bool _hasNewColumns = false;

        private static MySqlConnection GetConnection() {
            if( _connection != null && _connection.State == System.Data.ConnectionState.Open ) {
                return _connection;
            }

            // Dispose broken/closed connection before making a new one
            if( _connection != null ) {
                try { _connection.Dispose(); } catch { }
                _connection = null;
            }

            var connString = new MySqlConnectionStringBuilder();
            connString.Server = "localhost";
            connString.UserID = "root";
            connString.Password = "Soccerjjno2";
            connString.Database = "gta_gameroo";
            connString.CharacterSet = "utf8mb4";
            connString.ConnectionTimeout = 10;
            connString.DefaultCommandTimeout = 15;
            _connection = new MySqlConnection( connString.ToString() );
            _connection.Open();
            return _connection;
        }

        public static void EnsureTable() {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( @"CREATE TABLE IF NOT EXISTS players (
                    license VARCHAR(128) PRIMARY KEY,
                    name VARCHAR(128),
                    xp INT DEFAULT 0,
                    level INT DEFAULT 1,
                    tokens INT DEFAULT 1000,
                    unlocked_models TEXT,
                    selected_model VARCHAR(128)
                )", conn );
                cmd.ExecuteNonQuery();
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error creating players table: " + ex.Message );
            }

            // Add new columns separately — each fully independent so one failure doesn't block the other
            TryAddColumn( "appearance", "TEXT" );
            TryAddColumn( "unlocked_items", "TEXT" );
            TryAddColumn( "admin_level", "INT DEFAULT 0" );

            // Check if columns exist now
            _hasNewColumns = HasColumn( "appearance" );
            CitizenFX.Core.Debug.WriteLine( "[GameRoo] New columns available: " + _hasNewColumns );

            EnsureSettingsTable();
        }

        private static void EnsureSettingsTable() {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( @"CREATE TABLE IF NOT EXISTS settings (
                    setting_key VARCHAR(64) PRIMARY KEY,
                    setting_value TEXT
                )", conn );
                cmd.ExecuteNonQuery();
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error creating settings table: " + ex.Message );
            }
        }

        public static string GetSetting( string key ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "SELECT setting_value FROM settings WHERE setting_key = ?key", conn );
                cmd.Parameters.AddWithValue( "key", key );
                var reader = cmd.ExecuteReader();
                if( reader.Read() ) {
                    string val = reader.IsDBNull( 0 ) ? null : reader.GetString( 0 );
                    reader.Close();
                    return val;
                }
                reader.Close();
                return null;
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error getting setting: " + ex.Message );
                return null;
            }
        }

        public static void SetSetting( string key, string value ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "INSERT INTO settings (setting_key, setting_value) VALUES (?key, ?value) ON DUPLICATE KEY UPDATE setting_value = ?value2", conn );
                cmd.Parameters.AddWithValue( "key", key );
                cmd.Parameters.AddWithValue( "value", value );
                cmd.Parameters.AddWithValue( "value2", value );
                cmd.ExecuteNonQuery();
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error saving setting: " + ex.Message );
            }
        }

        private static void TryAddColumn( string columnName, string columnType ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "ALTER TABLE players ADD COLUMN " + columnName + " " + columnType, conn );
                cmd.ExecuteNonQuery();
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Added column: " + columnName );
            } catch( Exception ) {
                // Column already exists or other error — that's fine
            }
        }

        private static bool HasColumn( string columnName ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "SELECT " + columnName + " FROM players LIMIT 0", conn );
                var reader = cmd.ExecuteReader();
                reader.Close();
                return true;
            } catch( Exception ) {
                return false;
            }
        }

        public static PlayerData LoadPlayer( string license, string name ) {
            try {
                var conn = GetConnection();

                // Use SELECT * to avoid issues if new columns don't exist yet
                var cmd = new MySqlCommand( "SELECT * FROM players WHERE license = ?license", conn );
                cmd.Parameters.AddWithValue( "license", license );
                var reader = cmd.ExecuteReader();

                if( reader.Read() ) {
                    var data = new PlayerData {
                        License = reader.GetString( 0 ),
                        Name = reader.GetString( 1 ),
                        XP = reader.GetInt32( 2 ),
                        Level = reader.GetInt32( 3 ),
                        Tokens = reader.GetInt32( 4 ),
                        UnlockedModels = reader.IsDBNull( 5 ) ? new List<string>() : reader.GetString( 5 ).Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList(),
                        SelectedModel = reader.IsDBNull( 6 ) ? "" : reader.GetString( 6 ),
                        AppearanceJson = null,
                        UnlockedItems = new List<string>()
                    };

                    // Read new columns if they exist
                    if( _hasNewColumns && reader.FieldCount > 7 ) {
                        data.AppearanceJson = reader.IsDBNull( 7 ) ? null : reader.GetString( 7 );
                        if( reader.FieldCount > 8 ) {
                            data.UnlockedItems = reader.IsDBNull( 8 ) ? new List<string>() : reader.GetString( 8 ).Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                        }
                        if( reader.FieldCount > 9 ) {
                            data.AdminLevel = reader.IsDBNull( 9 ) ? 0 : reader.GetInt32( 9 );
                        }
                    }

                    reader.Close();

                    // Update name if changed
                    if( data.Name != name ) {
                        data.Name = name;
                        SavePlayer( data );
                    }

                    return data;
                }
                reader.Close();

                // First join — pick a random model
                var rng = new Random();
                var randomModel = PedModels.All[rng.Next( PedModels.All.Count )];

                var newPlayer = new PlayerData {
                    License = license,
                    Name = name,
                    XP = 0,
                    Level = 1,
                    Tokens = 1000,
                    UnlockedModels = new List<string> { randomModel.Hash },
                    SelectedModel = randomModel.Hash,
                    AppearanceJson = null,
                    UnlockedItems = new List<string>(),
                    IsNewPlayer = true
                };

                if( _hasNewColumns ) {
                    var insert = new MySqlCommand( "INSERT INTO players (license, name, xp, level, tokens, unlocked_models, selected_model, appearance, unlocked_items) VALUES (?license, ?name, ?xp, ?level, ?tokens, ?unlocked_models, ?selected_model, ?appearance, ?unlocked_items)", conn );
                    insert.Parameters.AddWithValue( "license", newPlayer.License );
                    insert.Parameters.AddWithValue( "name", newPlayer.Name );
                    insert.Parameters.AddWithValue( "xp", newPlayer.XP );
                    insert.Parameters.AddWithValue( "level", newPlayer.Level );
                    insert.Parameters.AddWithValue( "tokens", newPlayer.Tokens );
                    insert.Parameters.AddWithValue( "unlocked_models", string.Join( ",", newPlayer.UnlockedModels ) );
                    insert.Parameters.AddWithValue( "selected_model", newPlayer.SelectedModel );
                    insert.Parameters.AddWithValue( "appearance", DBNull.Value );
                    insert.Parameters.AddWithValue( "unlocked_items", "" );
                    insert.ExecuteNonQuery();
                } else {
                    var insert = new MySqlCommand( "INSERT INTO players (license, name, xp, level, tokens, unlocked_models, selected_model) VALUES (?license, ?name, ?xp, ?level, ?tokens, ?unlocked_models, ?selected_model)", conn );
                    insert.Parameters.AddWithValue( "license", newPlayer.License );
                    insert.Parameters.AddWithValue( "name", newPlayer.Name );
                    insert.Parameters.AddWithValue( "xp", newPlayer.XP );
                    insert.Parameters.AddWithValue( "level", newPlayer.Level );
                    insert.Parameters.AddWithValue( "tokens", newPlayer.Tokens );
                    insert.Parameters.AddWithValue( "unlocked_models", string.Join( ",", newPlayer.UnlockedModels ) );
                    insert.Parameters.AddWithValue( "selected_model", newPlayer.SelectedModel );
                    insert.ExecuteNonQuery();
                }

                return newPlayer;
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error loading player: " + ex.Message );
                return null;
            }
        }

        public static void SavePlayer( PlayerData data ) {
            try {
                var conn = GetConnection();

                if( _hasNewColumns ) {
                    var cmd = new MySqlCommand( "UPDATE players SET name=?name, xp=?xp, level=?level, tokens=?tokens, unlocked_models=?unlocked_models, selected_model=?selected_model, appearance=?appearance, unlocked_items=?unlocked_items WHERE license=?license", conn );
                    cmd.Parameters.AddWithValue( "license", data.License );
                    cmd.Parameters.AddWithValue( "name", data.Name );
                    cmd.Parameters.AddWithValue( "xp", data.XP );
                    cmd.Parameters.AddWithValue( "level", data.Level );
                    cmd.Parameters.AddWithValue( "tokens", data.Tokens );
                    cmd.Parameters.AddWithValue( "unlocked_models", string.Join( ",", data.UnlockedModels ) );
                    cmd.Parameters.AddWithValue( "selected_model", data.SelectedModel );
                    cmd.Parameters.AddWithValue( "appearance", data.AppearanceJson != null ? (object)data.AppearanceJson : DBNull.Value );
                    cmd.Parameters.AddWithValue( "unlocked_items", data.UnlockedItems.Count > 0 ? string.Join( ",", data.UnlockedItems ) : "" );
                    cmd.ExecuteNonQuery();
                } else {
                    var cmd = new MySqlCommand( "UPDATE players SET name=?name, xp=?xp, level=?level, tokens=?tokens, unlocked_models=?unlocked_models, selected_model=?selected_model WHERE license=?license", conn );
                    cmd.Parameters.AddWithValue( "license", data.License );
                    cmd.Parameters.AddWithValue( "name", data.Name );
                    cmd.Parameters.AddWithValue( "xp", data.XP );
                    cmd.Parameters.AddWithValue( "level", data.Level );
                    cmd.Parameters.AddWithValue( "tokens", data.Tokens );
                    cmd.Parameters.AddWithValue( "unlocked_models", string.Join( ",", data.UnlockedModels ) );
                    cmd.Parameters.AddWithValue( "selected_model", data.SelectedModel );
                    cmd.ExecuteNonQuery();
                }
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error saving player: " + ex.Message );
            }
        }

        public static PlayerData GetPlayer( string license ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "SELECT * FROM players WHERE license = ?license", conn );
                cmd.Parameters.AddWithValue( "license", license );
                var reader = cmd.ExecuteReader();

                if( reader.Read() ) {
                    var data = new PlayerData {
                        License = reader.GetString( 0 ),
                        Name = reader.GetString( 1 ),
                        XP = reader.GetInt32( 2 ),
                        Level = reader.GetInt32( 3 ),
                        Tokens = reader.GetInt32( 4 ),
                        UnlockedModels = reader.IsDBNull( 5 ) ? new List<string>() : reader.GetString( 5 ).Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList(),
                        SelectedModel = reader.IsDBNull( 6 ) ? "" : reader.GetString( 6 ),
                        AppearanceJson = null,
                        UnlockedItems = new List<string>()
                    };

                    if( _hasNewColumns && reader.FieldCount > 7 ) {
                        data.AppearanceJson = reader.IsDBNull( 7 ) ? null : reader.GetString( 7 );
                        if( reader.FieldCount > 8 ) {
                            data.UnlockedItems = reader.IsDBNull( 8 ) ? new List<string>() : reader.GetString( 8 ).Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                        }
                        if( reader.FieldCount > 9 ) {
                            data.AdminLevel = reader.IsDBNull( 9 ) ? 0 : reader.GetInt32( 9 );
                        }
                    }

                    reader.Close();
                    return data;
                }
                reader.Close();
                return null;
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error getting player: " + ex.Message );
                return null;
            }
        }

        public static void SetAdminLevel( string license, int level ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "UPDATE players SET admin_level=?level WHERE license=?license", conn );
                cmd.Parameters.AddWithValue( "license", license );
                cmd.Parameters.AddWithValue( "level", level );
                cmd.ExecuteNonQuery();
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error setting admin level: " + ex.Message );
            }
        }

        public static PlayerData LookupPlayer( string license ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "SELECT name, admin_level FROM players WHERE license = ?license", conn );
                cmd.Parameters.AddWithValue( "license", license );
                var reader = cmd.ExecuteReader();

                if( reader.Read() ) {
                    var data = new PlayerData {
                        License = license,
                        Name = reader.IsDBNull( 0 ) ? "" : reader.GetString( 0 ),
                        AdminLevel = reader.IsDBNull( 1 ) ? 0 : reader.GetInt32( 1 )
                    };
                    reader.Close();
                    return data;
                }
                reader.Close();
                return null;
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GameRoo] Error looking up player: " + ex.Message );
                return null;
            }
        }
    }
}
