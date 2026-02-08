using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using GamemodeCityShared;

namespace GamemodeCityServer {

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
            connString.Database = "gamemodecity";
            connString.ConnectionTimeout = 10;
            connString.DefaultCommandTimeout = 15;
            connString.CharacterSet = "utf8mb4";
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
                CitizenFX.Core.Debug.WriteLine( "[GamemodeCity] Error creating players table: " + ex.Message );
            }

            // Add new columns separately — each fully independent so one failure doesn't block the other
            TryAddColumn( "appearance", "TEXT" );
            TryAddColumn( "unlocked_items", "TEXT" );

            // Check if columns exist now
            _hasNewColumns = HasColumn( "appearance" );
            CitizenFX.Core.Debug.WriteLine( "[GamemodeCity] New columns available: " + _hasNewColumns );
        }

        private static void TryAddColumn( string columnName, string columnType ) {
            try {
                var conn = GetConnection();
                var cmd = new MySqlCommand( "ALTER TABLE players ADD COLUMN " + columnName + " " + columnType, conn );
                cmd.ExecuteNonQuery();
                CitizenFX.Core.Debug.WriteLine( "[GamemodeCity] Added column: " + columnName );
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
                    UnlockedItems = new List<string>()
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
                CitizenFX.Core.Debug.WriteLine( "[GamemodeCity] Error loading player: " + ex.Message );
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
                CitizenFX.Core.Debug.WriteLine( "[GamemodeCity] Error saving player: " + ex.Message );
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
                    }

                    reader.Close();
                    return data;
                }
                reader.Close();
                return null;
            } catch( Exception ex ) {
                CitizenFX.Core.Debug.WriteLine( "[GamemodeCity] Error getting player: " + ex.Message );
                return null;
            }
        }
    }
}
