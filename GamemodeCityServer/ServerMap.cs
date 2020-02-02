using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class ServerMap : Map {

        
        public ServerMap( int id, string name, List<string> gamemode, Vector3 position, Vector3 size ) : base ( id, name, gamemode, position, size ) {
            
        }

        public void SpawnGuns() {

            foreach( var spawn in GetSpawns(SpawnType.WEAPON) ) {
                ServerGlobals.CurrentGame.SpawnWeapon( spawn.Position, GrabWeapon( false ) );
            }
        }

        public uint GrabWeapon( bool melee ) {
            uint wep = 0;
            if( !melee ) {
                wep = ServerGlobals.CurrentGame.Settings.Weapons.OrderBy( x => Guid.NewGuid() ).Where( x => Globals.Weapons[x]["Group"] != "GROUP_MELEE" && Globals.Weapons[x]["Group"] != "GROUP_UNARMED" ).First();
            }
            return wep;
        }

    }
}
