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
            Settings.Weapons = new List<uint>() { 2725352035, 453432689, 736523883, 3220176749, 4024951519, 2937143193, 3173288789 };
            Settings.GameLength = 5 * 1000 * 60;
            
        }

        public override void Start() {

            base.Start();

            Map.SpawnGuns();

            PlayerList playerList = new PlayerList();


            foreach( var player in playerList ) {
                SpawnPlayer( player, 0 );
                SetTeam( player, 0 );
            }


        }

        public override void OnPlayerKilled( Player attacker, string victimSrc ) {


            base.OnPlayerKilled( attacker, victimSrc );
        }

    }
}
