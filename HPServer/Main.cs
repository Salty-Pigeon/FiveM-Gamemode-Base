using GamemodeCityServer;
using GamemodeCityShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPServer
{

    public enum Teams {
        Safe,
        It
    }

    public class Main : BaseGamemode
    {
        Player ItPlayer;
        float PotatoEndTime = 0;

        public Main() : base( "HP" ) {
            Settings.GameLength = (5 * 1000 * 60);
            Settings.Name = "Hot Potato";
            Settings.Rounds = 1;
            Settings.PreGameTime = (1 * 1000 * 15);

            EventHandlers["salty:netHPPass"] += new Action<Player, int>( OnPotatoPass );
        }

        public override void Start() {
            base.Start();

            List<Player> playerList = new PlayerList().ToList();

            foreach( var player in playerList ) {
                SetTeam( player, (int)Teams.Safe );
                SpawnPlayer( player );
            }

            // Pick random player to be "it"
            Player firstIt = playerList.OrderBy( x => Guid.NewGuid() ).First();
            AssignPotato( firstIt );
        }

        private void AssignPotato( Player player ) {
            if( ItPlayer != null ) {
                SetTeam( ItPlayer, (int)Teams.Safe );
            }
            ItPlayer = player;
            SetTeam( player, (int)Teams.It );
            PotatoEndTime = GetGameTimer() + 30000;
            WriteChat( "Hot Potato", player.Name + " has the potato!", 255, 68, 68 );
        }

        private void OnPotatoPass( [FromSource] Player source, int targetServerId ) {
            if( ItPlayer == null ) return;
            if( source.Handle != ItPlayer.Handle ) return;

            Player target = null;
            foreach( var ply in new PlayerList() ) {
                if( Convert.ToInt32( ply.Handle ) == targetServerId ) {
                    target = ply;
                    break;
                }
            }

            if( target == null ) return;

            object targetTeam = GetPlayerDetail( target, "team" );
            if( targetTeam == null || Convert.ToInt32( targetTeam ) != (int)Teams.Safe ) return;

            WriteChat( "Hot Potato", ItPlayer.Name + " passed the potato to " + target.Name + "!", 255, 200, 50 );
            AssignPotato( target );
        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {
            object teamObj = GetPlayerDetail( victim, "team" );
            int victimTeam = teamObj != null ? Convert.ToInt32( teamObj ) : -1;

            // Eliminate victim (spectator)
            SetTeam( victim, -1 );

            bool victimWasIt = ( victimTeam == (int)Teams.It );
            if( victimWasIt ) {
                ItPlayer = null;
            }

            // Count alive players
            List<Player> safePlayers = GetTeamPlayers( (int)Teams.Safe );
            int aliveCount = safePlayers.Count + ( ItPlayer != null ? 1 : 0 );

            if( aliveCount <= 1 ) {
                // Find the winner
                Player winner = ItPlayer;
                if( winner == null && safePlayers.Count > 0 ) {
                    winner = safePlayers[0];
                }
                string winnerName = winner != null ? winner.Name : "Nobody";
                WriteChat( "Hot Potato", winnerName + " wins! Last one standing!", 50, 200, 50 );
                TriggerClientEvent( "salty::HPRoundResult", winnerName, "#22c55e", "Last player standing!" );
                End();
            }
            else if( victimWasIt && aliveCount >= 2 ) {
                // Pick new random "it" from safe players
                Player newIt = safePlayers.OrderBy( x => Guid.NewGuid() ).First();
                AssignPotato( newIt );
            }

            base.OnPlayerDied( victim, killerType, deathCoords );
        }

        public override void OnTimerEnd() {
            WriteChat( "Hot Potato", "Time's up! It's a draw!", 255, 200, 50 );
            TriggerClientEvent( "salty::HPRoundResult", "Draw", "#ff4444", "Time ran out!" );
            base.OnTimerEnd();
        }
    }
}
