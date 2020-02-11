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
        float timeAddedOnDeath = 1 * 1000 * 45;

        public Dictionary<int, bool> DeadBodies = new Dictionary<int, bool>();



        public Main() : base( "TTT" ) {
            Settings.Weapons = new List<uint>() { 2725352035, 453432689, 736523883, 3220176749, 4024951519, 2937143193, 3173288789 };
            Settings.Name = "Trouble in Terrorist Town";
            Settings.Rounds = 1;
            Settings.GameLength = (10 * 1000 * 60);
            Settings.PreGameTime = (1 * 1000 * 15);

            EventHandlers["salty::netBodyDiscovered"] += new Action<Player, int>( BodyDiscovered );
        }

        public override void Start() {

            base.Start();

            Map.SpawnGuns();


            List<Player> playerList = new PlayerList().ToList();

            for( var i = 0; i < Math.Ceiling(playerList.Count / traitorsPerPlayers); i++ ) {
                var player = playerList.OrderBy( x => Guid.NewGuid() ).First();
                Debug.WriteLine( playerList.Count.ToString() );

                playerList.Remove( player );

                Traitors.Add( player );
                SetTeam( player, (int)Teams.Traitor );
                SetPlayerDetail( player, "coins", 1 );
            }


            if( playerList.Count > 0 ) {
                for( var i = 0; i < Math.Floor( playerList.Count / detectivesPerPlayers ); i++ ) {

                    var player = playerList.OrderBy( x => Guid.NewGuid() ).First();
                    playerList.Remove( player );
                    Detectives.Add( player );
                    SetTeam( player, (int)Teams.Detective );
                    SetPlayerDetail( player, "coins", 1 );
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

        public override void OnPlayerKilled( Player victim, Player attacker, Vector3 deathCoords, uint weaponHash ) {
            TriggerClientEvent( "salty::SpawnDeadBody", deathCoords, Convert.ToInt32( victim.Handle ), Convert.ToInt32( attacker.Handle ), weaponHash );
            if( attacker != null && Traitors.Contains( attacker )  ) {
                AddPlayerDetail( attacker, "coins", 1 );
            }
                
            base.OnPlayerKilled( attacker, victim, deathCoords, weaponHash );
        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {

            Teams team = (Teams)GetPlayerDetail( victim, "team" );
            if( Traitors.Contains( victim ) ) {
                Traitors.Remove( victim );
                if( Traitors.Count == 0 ) {
                    WriteChat( "TTT", "Innocents win", 20, 200, 20 );
                    End();
                    return;
                }
            }

            if( !Traitors.Contains( victim ) ) {
                GameTime += timeAddedOnDeath;
                foreach( var ply in Traitors ) {
                    ply.TriggerEvent( "salty:UpdateTime", GameTime );
                }
            }

            if( Detectives.Contains( victim ) ) {
                Detectives.Remove( victim );
            }
            if( Innocents.Contains( victim ) ) {
                Innocents.Remove( victim );
            }

            if( Innocents.Count + Detectives.Count == 0 ) {
                WriteChat( "TTT", "Traitors win", 200, 20, 20 );
                End();
                return;
            }

            base.OnPlayerDied( victim, killerType, deathCoords );
        }

        public override void OnDetailUpdate( Player ply, string key, dynamic oldValue, dynamic newValue ) {
            if( key == "disguise" ) {

            }
            base.OnDetailUpdate( ply, key, (object)oldValue, (object)newValue );
        }

        public void BodyDiscovered( [FromSource] Player ply, int body ) {
            DeadBodies[body] = true;
            TriggerClientEvent( "salty::UpdateDeadBody", body );
        }


        public override void End( ) {
            if( GameTime < GetGameTimer() ) {
                WriteChat( "TTT", "Time over! Innocents win", 200, 20, 20 );
            }
            base.End();
        }

    }
}
