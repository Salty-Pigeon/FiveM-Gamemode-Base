using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace GamemodeCityServer {
    class MapManager {

        public List<ServerMap> Maps = new List<ServerMap>();


        public ServerMap FindMap( Vector3 pos ) {
            foreach( ServerMap map in Maps ) {
                if( map.IsInZone(pos) )
                    return map;
            }
            return null;
        }

        
        public ServerMap FindMap( string gamemode ) {
            return Maps.Where( x => x.Gamemodes.Contains( gamemode ) ).Select( x => x ).First();
        }

        public void Update( [FromSource] Player ply, ExpandoObject expandoObject ) {


            Dictionary<string, dynamic> updateDetails = expandoObject.ToDictionary( x => x.Key, x => x.Value );

            if( !(updateDetails["create"] )  ) {

                ServerMap map = Maps.Find( x => x.ID == updateDetails["id"] );

                if( map != null ) {
                    foreach( var detail in updateDetails ) {
                        switch( detail.Key ) {

                            case "name":
                                map.Name = detail.Value;
                                break;

                            case "gamemode":
                                map.Gamemodes = (detail.Value as string).Split(',').ToList<string>();
                                break;

                            case "position":
                                map.Position = detail.Value;
                                break;

                            case "size":
                                map.Size = detail.Value;
                                break;

                            case "spawns":                
                                map.SpawnsFromSendable( detail.Value );
                                break;
                        }
                    }

                    Database.SaveMap( map );
                }

            } else {
                ServerMap map = new ServerMap( 0, updateDetails["name"], new List<string>(), updateDetails["position"], updateDetails["size"] );
                Maps.Add( map );
                Database.SaveMap( map );
            }

            
        }


    }
}
