using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityServer {
    public class BaseGamemode : BaseScript {

        public string Gamemode;

        public ServerMap Map;

        public Settings Settings = new Settings();

        public Dictionary<int, float> TeamScores = new Dictionary<int, float>();

        public float GameTime = 0;

        public Dictionary<Player, Dictionary<string, dynamic>> PlayerDetails = new Dictionary<Player, Dictionary<string, dynamic>>();

        public List<Player> Spectators = new List<Player>();

        public bool PreGame = true;
        private float PreGameTime = 0;

        public BaseGamemode( string gamemode ) {
            Globals.GameCoins = 0;
            Gamemode = gamemode.ToLower();
            if (!ServerGlobals.Gamemodes.ContainsKey(Gamemode))
                ServerGlobals.Gamemodes.Add(Gamemode, this);

        }

        ~BaseGamemode() {
            Debug.WriteLine("gamemode disposed");
        }


        public virtual void Start( ) {
           
            TriggerClientEvent( "salty:StartGame", Gamemode, Settings.GameLength, Settings.Weapons, Map.Position, Map.Size );
        }

        public virtual void OnTimerEnd() {
            End();
        }

        public virtual void Update() {
            if( PreGame && PreGameTime > 0 ) {
                if( PreGameTime < GetGameTimer() ) {
                    PreGame = false;
                    Main.StartGame( ServerGlobals.CurrentGame.Gamemode );
                }
            }
        }

        public virtual void End() {
            TriggerClientEvent( "salty:EndGame" );
            ServerGlobals.CurrentRound++;
            if( ServerGlobals.CurrentRound < Settings.Rounds ) {
                WriteChat( "GamemodeCity", "Next round starting in " + Math.Round( Settings.PreGameTime / 1000 ), 200, 200, 20 );
                PreGameTime = GetGameTimer() + Settings.PreGameTime;
                PreGame = true;
            }
            else {
                ServerGlobals.CurrentRound = 0;
                ServerGlobals.CurrentGame = null;
                Main.BeginGameVote();
            }
        }

        public Player GetPlayer( string src ) {
            return new PlayerList().Where( x => x.Handle == src ).First();
        }

        public virtual void OnPlayerKilled( Player victim, Player attacker, Vector3 deathcords , uint weaponHash) {
            
        }

        public virtual void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {

        }

        public static void WriteChat( string prefix, string str, int r, int g, int b ) {
            TriggerClientEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }

        

        public static void WriteChat( Player ply, string prefix, string str, int r, int g, int b ) {
            ply.TriggerEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }

        public void SpawnPlayer( Player player, int team ) {
            player.TriggerEvent( "salty:Spawn", (int)SpawnType.PLAYER, Map.GetSpawn( SpawnType.PLAYER, team ).Position, 0 );
        }

        public void SpawnPlayers() {
            foreach( var ply in new PlayerList() ) {
                SpawnPlayer( ply );
            }
        }

        public void SpawnPlayers( int team ) {
            foreach( var ply in new PlayerList() ) {
                SpawnPlayer( ply, team );
            }
        }

        public void SpawnPlayer( Player player ) {
            dynamic team = GetPlayerDetail( player, "team" );
            if( team != null ) {
                player.TriggerEvent( "salty:Spawn", (int)SpawnType.PLAYER, Map.GetSpawn( SpawnType.PLAYER, (int)team ).Position, 0 );
            } else {
                SpawnPlayer( player, 0 );
            }
        }

        public void SpawnWeapon( Vector3 pos, uint hash ) {
            TriggerClientEvent( "salty:Spawn", (int)SpawnType.WEAPON, pos, hash );
        }

        public void AddScore( Player ply, float amount ) {
            dynamic score = GetPlayerDetail( ply, "score" );
            if( score != null ) {
                SetPlayerDetail( ply, "score", (float)score + amount );
            } else {
                SetPlayerDetail( ply, "score", amount );
            }
        }

        public void SetTeam( Player ply, int team ) {
            ply.TriggerEvent( "salty:SetTeam", team );
            SetPlayerDetail( ply, "team", team );
        }

        public List<Player> GetTeamPlayers(int team ) {
            List<Player> teamPlayers = new List<Player>();
            foreach( var player in new PlayerList() ) {
                int plyTeam = GetPlayerDetail( player, "team" );
                if( plyTeam == team ) {
                    teamPlayers.Add( player );
                }
            }
            return teamPlayers;
        }


        public virtual void OnDetailUpdate( Player ply, string key, dynamic oldValue, dynamic newValue ) {
            
            TriggerClientEvent( "salty:updatePlayerDetail", Convert.ToInt32( ply.Handle ), key, newValue );
        }

        public void SetPlayerDetail( Player ply, string detail, dynamic data ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<string, dynamic>() );
            }
            if( !PlayerDetails[ply].ContainsKey(detail) ) {
                PlayerDetails[ply].Add( detail, data );
            }
            OnDetailUpdate( ply, detail, PlayerDetails[ply][detail], data );
            PlayerDetails[ply][detail] = data;
        }

        public void AddPlayerDetail( Player ply, string detail, float amount ) {
            SetPlayerDetail( ply, detail, GetPlayerDetail( ply, detail ) + amount );
        }

        public dynamic GetPlayerDetail( Player ply, string detail ) {
            if( !PlayerDetails.ContainsKey(ply) ) {
                PlayerDetails.Add( ply, new Dictionary<string, dynamic>() );
            } else if( PlayerDetails[ply].ContainsKey( detail ) ) {
                return PlayerDetails[ply][detail];
            }
            return null;
        }

    }


}
