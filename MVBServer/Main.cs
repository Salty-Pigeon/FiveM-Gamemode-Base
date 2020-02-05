using GamemodeCityServer;
using GamemodeCityShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVBServer
{
    public class Main : BaseGamemode
    {
        public Main() : base( "MVB" ) {
            Settings.GameLength = (1 * 1000 * 60);
            Settings.Name = "Monster Trucks vs Motorbikes";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);
        }

        public override void Start() {

            base.Start();


            PlayerList playerList = new PlayerList();


            foreach( var player in playerList ) {
                SpawnPlayer( player, 0 );
                SetTeam( player, 0 );
            }

        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {

            base.OnPlayerDied( victim, killerType, deathCoords );
        }
    }
}
