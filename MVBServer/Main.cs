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

    public enum Teams {
        Trucker,
        Bikie
    }
    public class Main : BaseGamemode
    {
        public Main() : base( "MVB" ) {
            Settings.GameLength = (10 * 1000 * 60);
            Settings.Name = "Monster Trucks vs Motorbikes";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);
        }

        public override void Start() {

            base.Start();


            List<Player> playerList = new PlayerList().ToList();

            var trucker = playerList.OrderBy( x => Guid.NewGuid() ).First();
            playerList.Remove( trucker );
            SetTeam( trucker, (int)Teams.Bikie );
            SpawnPlayer( trucker );

            foreach( var player in playerList ) {
                SetTeam( player, (int)Teams.Bikie );
                SpawnPlayer( player );
            }

        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {
            Teams team = (Teams)GetPlayerDetail( victim, "team" );
            if( team == Teams.Bikie ) {
                SetTeam( victim, (int)Teams.Trucker );
            }
            if( GetTeamPlayers( (int)Teams.Bikie ).Count == 0 ) {
                End();
            }
            base.OnPlayerDied( victim, killerType, deathCoords );
        }
    }
}
