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

        public List<Map> Maps = new List<Map>();


        public Map FindMap( Vector3 pos ) {
            foreach( Map map in Maps ) {
                if( map.IsInZone(pos) )
                    return map;
            }
            return null;
        }

        
        public void Update( [FromSource] Player ply, ExpandoObject expandoObject ) {

            Dictionary<string, dynamic> updateDetails = expandoObject.ToDictionary( x => x.Key, x => x.Value );
            if( updateDetails.ContainsKey( "playerPos" ) ) {
                Map map = FindMap( updateDetails["playerPos"] );
                if( map != null ) {
                    foreach( var detail in updateDetails ) {
                        switch( detail.Key ) {

                            case "name":
                                map.Name = detail.Value;
                                break;

                            case "gamemode":
                                map.Gamemodes = detail.Value;
                                break;

                            case "position":
                                map.Position = detail.Value;
                                break;

                            case "size":
                                map.Size = detail.Value;
                                break;

                        }
                    }
                    Database.SaveMap( map );
                }
            } else {
                Map map = new Map( 0, updateDetails["name"], "", updateDetails["position"], updateDetails["size"] );
                Maps.Add( map );
                Database.CreateMap( map );
            }

            
        }


    }
}
