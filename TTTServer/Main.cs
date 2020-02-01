using GamemodeCityServer;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTTServer
{
    public class Main : BaseGamemode
    {

        public Main() : base( "TTT" ) {
            Settings.Weapons = new List<uint>() { 2725352035, 453432689, 736523883, 3220176749 };
        }

        public override void Start() {

            base.Start();

            PlayerList playerList = new PlayerList();


            foreach( var player in playerList ) {
                Spawn( player, 0 );
            }


        }

        public override void OnPlayerKilled( Player attacker, string victimSrc ) {


            base.OnPlayerKilled( attacker, victimSrc );
        }

    }
}
