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

    public enum Teams {
        Innocent,
        Traitor,
        Detective
    }

    public class Main : BaseGamemode
    {

        List<Player> Traitors = new List<Player>();
        List<Player> Detectives = new List<Player>();
        List<Player> Innocents = new List<Player>();

        double traitorsPerPlayers = 4;
        double detectivesPerPlayers = 4;

        

        public Main() : base( "TTT" ) {
            Settings.Weapons = new List<uint>() { 2725352035, 453432689, 736523883, 3220176749, 4024951519, 2937143193, 3173288789 };
            Settings.GameLength = (1 * 1000 * 60);
            
        }

        public override void Start() {

            base.Start();

            Map.SpawnGuns();


            List<Player> playerList = new PlayerList().ToList();


            for( var i = 0; i <= Math.Ceiling(playerList.Count / traitorsPerPlayers); i++ ) {
                var player = playerList.OrderBy( x => Guid.NewGuid() ).First();
                playerList.Remove( player );
                Traitors.Add( player );
                SetTeam( player, (int)Teams.Traitor );
            }

            if( playerList.Count > 0 ) {
                for( var i = 0; i <= Math.Floor( playerList.Count / detectivesPerPlayers ); i++ ) {
                    var player = playerList.OrderBy( x => Guid.NewGuid() ).First();
                    playerList.Remove( player );
                    Detectives.Add( player );
                    SetTeam( player, (int)Teams.Detective );
                }
            }

            if( playerList.Count > 0 ) {
                foreach( var player in playerList ) {
                    Innocents.Add( player );
                    SetTeam( player, (int)Teams.Innocent );
                }
            }
            

            SpawnPlayers( 0 );


        }

        public override void OnPlayerKilled( Player attacker, string victimSrc ) {


            base.OnPlayerKilled( attacker, victimSrc );
        }

    }
}
