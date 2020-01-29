using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace GamemodeCityServer {
    class Database {

        public static MySqlConnection Connection;

        public Database( MapManager mapManager ) {
            MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
            conn_string.Server = "127.0.0.1";
            conn_string.UserID = "root";
            conn_string.Password = "";
            conn_string.Database = "gamemodecity";

            Connection = new MySqlConnection( conn_string.ToString() );
            Connection.Open();

            mapManager.Maps = Load();
        }

        public List<Map> Load( ) {
            List<Map> Maps = new List<Map>();
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


                    Map map = new Map( id, name, gamemode, posX, posY, posZ, sizeX, sizeY, sizeZ );

                    Maps.Add( map );
                }

                MyDataReader.Close();
            }

            return Maps;
        }


        

        public static void SaveMap( Map map ) {
            if( Connection.State == System.Data.ConnectionState.Open ) {
                MySqlCommand comm = new MySqlCommand( "", Connection );
                Debug.WriteLine( "Saving map ID " + map.ID );
                comm.CommandText = "REPLACE INTO maps(id,name,gamemode,posX,posY,posZ,sizeX,sizeY,sizeZ) VALUES(?id, ?name, ?gamemode, ?posX, ?posY, ?posZ, ?sizeX, ?sizeY, ?sizeZ)";
                comm.Parameters.AddWithValue( "id", map.ID );
                comm.Parameters.AddWithValue( "name", map.Name );
                comm.Parameters.AddWithValue( "gamemode", string.Join(",", map.Gamemodes) );
                comm.Parameters.AddWithValue( "posX", map.Position.X );
                comm.Parameters.AddWithValue( "posY", map.Position.Y );
                comm.Parameters.AddWithValue( "posZ", map.Position.Z );
                comm.Parameters.AddWithValue( "sizeX", map.Size.X );
                comm.Parameters.AddWithValue( "sizeY", map.Size.Y );
                comm.Parameters.AddWithValue( "sizeZ", map.Size.Y );
                comm.ExecuteNonQuery();
            }
        }

        public static void CreateMap( Map map ) {
            if( Connection.State == System.Data.ConnectionState.Open ) {
                MySqlCommand comm = new MySqlCommand( "", Connection );
                comm.CommandText = "INSERT INTO maps(name,gamemode,posX,posY,posZ,sizeX,sizeY,sizeZ) VALUES(?name, ?gamemode, ?posX, ?posY, ?posZ, ?sizeX, ?sizeY, ?sizeZ);";
                comm.Parameters.AddWithValue( "name", map.Name );
                comm.Parameters.AddWithValue( "gamemode", string.Join( ",", map.Gamemodes ) );
                comm.Parameters.AddWithValue( "posX", map.Position.X );
                comm.Parameters.AddWithValue( "posY", map.Position.Y );
                comm.Parameters.AddWithValue( "posZ", map.Position.Z );
                comm.Parameters.AddWithValue( "sizeX", map.Size.X );
                comm.Parameters.AddWithValue( "sizeY", map.Size.Y );
                comm.Parameters.AddWithValue( "sizeZ", map.Size.Y );
                comm.ExecuteNonQuery();

                map.ID = (int)comm.LastInsertedId;
            }
        }

        public void SaveAll( Dictionary<string, Map> Maps ) {
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
