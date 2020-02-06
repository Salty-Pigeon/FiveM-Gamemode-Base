using GamemodeCityServer;
using GamemodeCityShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMServer
{

    public enum Teams {
        Kiddie,
        IceCreamMan
    }


    public class Main : BaseGamemode
    {
        public Main() : base( "ICM" ) {
            Settings.GameLength = (1 * 1000 * 60);
            Settings.Name = "Ice Cream Man";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);
        }

        public override void Start() {

            base.Start();


            List<Player> playerList = new PlayerList().ToList();

            var icecreamman = playerList.OrderBy( x => Guid.NewGuid() ).First();
            playerList.Remove( icecreamman );
            SetTeam( icecreamman, 0 );
            SpawnPlayer( icecreamman );

            foreach( var player in playerList ) {
                SetTeam( player, 1 );
                SpawnPlayer( player );
            }

        }
        

        public override void OnTimerEnd() {
            WriteChat( "Ice Cream Man", "Ice cream man delivered ice cream safely", 255, 0, 0 );
            base.OnTimerEnd();
        }

        public override void OnPlayerKilled( Player player, Player victim ) {
            if( GetPlayerDetail( victim, PlayerDetail.TEAM ) == (int)Teams.IceCreamMan ) {
                WriteChat( "Ice Cream Man", "Ice cream man defeated. Bikers win.", 255, 0, 0 );
                End();
            }
            base.OnPlayerKilled( player, victim );
        }



        public override void OnPlayerDied( Player player, int killerType, Vector3 deathcords ) {
            if( GetPlayerDetail( player, PlayerDetail.TEAM ) == (int)Teams.Kiddie ) {
                AddScore( player, 1 );
            }
            base.OnPlayerDied( player, killerType, deathcords );
        }



    }
}
