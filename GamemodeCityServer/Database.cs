using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using MySql.Data;
using MySql.Data.MySqlClient;
using GamemodeCityShared;


namespace GamemodeCityServer {
    class Database {

        public static MySqlConnection Connection;

        public Database( MapManager mapManager ) {
            MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
            conn_string.Server = "localhost";       // or 127.0.0.1
            conn_string.UserID = "root";            // your MySQL user
            conn_string.Password = "Soccerjjno2";   // your MySQL password
            conn_string.Database = "gamemodecity";    // matches the CREATE DATABASE above

            Connection = new MySqlConnection( conn_string.ToString() );
            Connection.Open();

            mapManager.Maps = Load();
        }

        public List<ServerMap> Load( ) {
            List<ServerMap> Maps = new List<ServerMap>();
            if( Connection.State == System.Data.ConnectionState.Open ) {

                MySqlCommand comm = new MySqlCommand( "", Connection );
                comm.CommandText = "SELECT * FROM maps";
                MySqlDataReader MyDataReader = comm.ExecuteReader();

                while( MyDataReader.Read() ) {

                    int id = MyDataReader.GetInt32( 0 );
                    string name = MyDataReader.GetString( 1 );
                    string gamemode = MyDataReader.GetString( 2 );
                    float posX = MyDataReader.GetFloat( 3 );
                    float posY = MyDataReader.GetFloat( 4 );
                    float posZ = MyDataReader.GetFloat( 5 );
                    float sizeX = MyDataReader.GetFloat( 6 );
                    float sizeY = MyDataReader.GetFloat( 7 );
                    float sizeZ = MyDataReader.GetFloat( 8 );


                    ServerMap map = new ServerMap( id, name, gamemode.Split(',').ToList(), new Vector3(posX, posY, posZ), new Vector3(sizeX, sizeY, sizeZ) );

                    Maps.Add( map );
                }

                MyDataReader.Close();
            }

            foreach( var map in Maps ) {
                map.Spawns = LoadSpawns( map );
            }

            return Maps;
        }

        public List<Spawn> LoadSpawns( ServerMap map ) {
            List<Spawn> spawns = new List<Spawn>();
            if( Connection.State == System.Data.ConnectionState.Open ) {

                MySqlCommand comm = new MySqlCommand( "", Connection );
                comm.CommandText = "SELECT * FROM spawns WHERE map=" + map.ID;
                MySqlDataReader MyDataReader = comm.ExecuteReader();

                while( MyDataReader.Read() ) {

                    
                    int id = MyDataReader.GetInt32( 0 );
                    int mapID = MyDataReader.GetInt32( 1 );
                    int spawntype = MyDataReader.GetInt32( 2 );
                    string spawnitem = MyDataReader.GetString( 3 );
                    int team = MyDataReader.GetInt32( 4 );
                    float posX = MyDataReader.GetFloat( 5 );
                    float posY = MyDataReader.GetFloat( 6 );
                    float posZ = MyDataReader.GetFloat( 7 );

                    spawns.Add( new Spawn( id, new Vector3( posX, posY, posZ ), (SpawnType)spawntype, spawnitem, team ) );
                    
                }

                MyDataReader.Close();
            }
            return spawns;
        }
        

        public static void SaveMap( ServerMap map ) {
            if( Connection.State == System.Data.ConnectionState.Open ) {
                MySqlCommand comm = new MySqlCommand( "", Connection );

                comm.CommandText = "REPLACE INTO maps(name,gamemode,posX,posY,posZ,sizeX,sizeY,sizeZ) VALUES(?name, ?gamemode, ?posX, ?posY, ?posZ, ?sizeX, ?sizeY, ?sizeZ)";

                if( map.ID >= 0 ) {
                    comm.CommandText = "REPLACE INTO maps(id,name,gamemode,posX,posY,posZ,sizeX,sizeY,sizeZ) VALUES(?id, ?name, ?gamemode, ?posX, ?posY, ?posZ, ?sizeX, ?sizeY, ?sizeZ)";
                    comm.Parameters.AddWithValue( "id", map.ID );
                }

                comm.Parameters.AddWithValue( "name", map.Name );
                comm.Parameters.AddWithValue( "gamemode", string.Join(",", map.Gamemodes) );
                comm.Parameters.AddWithValue( "posX", map.Position.X );
                comm.Parameters.AddWithValue( "posY", map.Position.Y );
                comm.Parameters.AddWithValue( "posZ", map.Position.Z );
                comm.Parameters.AddWithValue( "sizeX", map.Size.X );
                comm.Parameters.AddWithValue( "sizeY", map.Size.Y );
                comm.Parameters.AddWithValue( "sizeZ", map.Size.Y );
                comm.ExecuteNonQuery();

                map.ID = (int)comm.LastInsertedId;

            }
            foreach( var spawn in map.Spawns ) {
                EditSpawn( map, spawn );
            }
        }

        public static void EditSpawn( ServerMap map, Spawn spawn ) {
            if( Connection.State == System.Data.ConnectionState.Open ) {
                MySqlCommand comm = new MySqlCommand( "", Connection );

                comm.CommandText = "REPLACE INTO spawns(map,spawntype,spawnitem,team,posX,posY,posZ) VALUES(?map, ?spawntype, ?spawnitem, ?team, ?posX, ?posY, ?posZ)";
                if( spawn.ID >= 0 ) {
                    comm.CommandText = "REPLACE INTO spawns(id,map,spawntype,spawnitem,team,posX,posY,posZ) VALUES(?id, ?map, ?spawntype, ?spawnitem, ?team, ?posX, ?posY, ?posZ)";
                    comm.Parameters.AddWithValue( "id", spawn.ID );
                }
                comm.Parameters.AddWithValue( "map", map.ID );
                comm.Parameters.AddWithValue( "spawntype", spawn.SpawnType );
                comm.Parameters.AddWithValue( "spawnitem", spawn.Entity );
                comm.Parameters.AddWithValue( "team", spawn.Team );
                comm.Parameters.AddWithValue( "posX", spawn.Position.X );
                comm.Parameters.AddWithValue( "posY", spawn.Position.Y );
                comm.Parameters.AddWithValue( "posZ", spawn.Position.Z );
                comm.ExecuteNonQuery();

                spawn.ID = (int)comm.LastInsertedId; 
            }
        }


        public void SaveAll( Dictionary<string, ServerMap> Maps ) {
            if( Connection.State == System.Data.ConnectionState.Open ) {
                foreach( var map in Maps ) {
                    SaveMap( map.Value );
                }
            }
        }

        public void Remove( string name ) {
            MySqlCommand comm = new MySqlCommand( "", Connection );
            comm.CommandText = "DELETE FROM maps WHERE name = ?name";
            comm.Parameters.AddWithValue( "name", name );
            comm.ExecuteNonQuery();
        }
    }
}
