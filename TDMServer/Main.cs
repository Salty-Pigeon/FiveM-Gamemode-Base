using GTA_GameRooServer;
using GTA_GameRooShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMServer
{
    public class Main : BaseGamemode
    {

       
        
        public Main() : base("TDM") {
            Settings.Weapons =  new List<uint>(){ 2725352035, 453432689, 736523883, 3220176749 };
            Settings.GameLength = (1 * 1000 * 60);
            Settings.Name = "Team Deathmatch";

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

        public override void OnPlayerKilled( Player attacker, Player victim, Vector3 deathCoords, uint weaponHash ) {

            AddScore( attacker, 1 );

            base.OnPlayerKilled( attacker, victim, deathCoords, weaponHash );
        }
    }
}
