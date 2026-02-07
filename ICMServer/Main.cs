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
        IceCreamMan,
        Kiddie      
    }


    public class Main : BaseGamemode
    {

        Player IceCreamMan;

        public Main() : base( "ICM" ) {
            Settings.GameLength = (10 * 1000 * 60);
            Settings.Name = "Ice Cream Man";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);


        }

        

        public override void Start() {

            base.Start();


            List<Player> playerList = new PlayerList().ToList();

            IceCreamMan = playerList.OrderBy( x => Guid.NewGuid() ).First();
            playerList.Remove( IceCreamMan );
            SetTeam( IceCreamMan, (int)Teams.IceCreamMan );
            SpawnPlayer( IceCreamMan );

            foreach( var player in playerList ) {
                SetTeam( player, (int)Teams.Kiddie );
                SpawnPlayer( player );
            }

        }
        

        public override void OnTimerEnd() {
            WriteChat( "Ice Cream Man", "Ice cream man delivered ice cream safely", 255, 0, 0 );
            TriggerClientEvent( "salty::ICMRoundResult", "Ice Cream Man", "#f59e0b", "All ice cream delivered safely!" );
            base.OnTimerEnd();
        }

        public override void OnPlayerKilled( Player victim, Player attacker, Vector3 deathCoords, uint weaponHash ) {
            Teams team = (Teams)GetPlayerDetail( victim, "team" );
            if( team == Teams.IceCreamMan ) {
                WriteChat( "Ice Cream Man", "Ice cream man defeated. Bikers win.", 255, 0, 0 );
                TriggerClientEvent( "salty::ICMRoundResult", "Kids", "#06b6d4", "The Ice Cream Man has been stopped!" );
                End();
            }
            base.OnPlayerKilled( victim, attacker, deathCoords, weaponHash );
        }



        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathcords ) {
            Teams team = (Teams)GetPlayerDetail( victim, "team" );
            if( team == Teams.Kiddie ) {
                AddScore( IceCreamMan, 1 );

                // Check if all kids are dead
                List<Player> kids = GetTeamPlayers( (int)Teams.Kiddie );
                bool allDead = true;
                foreach( var kid in kids ) {
                    if( kid != victim ) {
                        allDead = false;
                        break;
                    }
                }
                if( allDead ) {
                    TriggerClientEvent( "salty::ICMRoundResult", "Ice Cream Man", "#f59e0b", "All kids have been splattered!" );
                    End();
                    return;
                }
            }
            base.OnPlayerDied( victim, killerType, deathcords );
        }



    }
}
