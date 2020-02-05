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

        public Dictionary<Player, Dictionary<PlayerDetail, dynamic>> PlayerDetails = new Dictionary<Player, Dictionary<PlayerDetail, dynamic>>();

        public List<Player> Spectators = new List<Player>();

        public bool PreGame = false;
        private float PreGameTime = 0;

        public BaseGamemode( string gamemode ) {
            Globals.GameCoins = 0;
            Gamemode = gamemode.ToLower();
            if( !ServerGlobals.Gamemodes.ContainsKey( Gamemode ) )
                ServerGlobals.Gamemodes.Add( Gamemode, this);

        }

        public virtual void Start( ) {
            GameTime = GetGameTimer() + Settings.GameLength;
            TriggerClientEvent( "salty:StartGame", Gamemode, Settings.GameLength, Settings.Weapons );
        }

        public virtual void Update() {
            if( PreGame ) {
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

        public virtual void OnPlayerKilled( Player attacker, Player victim ) {
            
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
            dynamic team = GetPlayerDetail( player, PlayerDetail.TEAM );
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
            dynamic score = GetPlayerDetail( ply, PlayerDetail.SCORE );
            if( score != null ) {
                SetPlayerDetail( ply, PlayerDetail.SCORE, (float)score + amount );
            } else {
                SetPlayerDetail( ply, PlayerDetail.SCORE, amount );
            }
        }

        public void SetTeam( Player ply, int team ) {
            ply.TriggerEvent( "salty:SetTeam", team );
            SetPlayerDetail( ply, PlayerDetail.TEAM, team );
        }

        public void SetPlayerDetail( Player ply, PlayerDetail detail, dynamic data ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<PlayerDetail, dynamic>() );
            }
            PlayerDetails[ply][PlayerDetail.TEAM] = data;
        }

        public dynamic GetPlayerDetail( Player ply, PlayerDetail detail ) {
            if( !PlayerDetails.ContainsKey(ply) ) {
                PlayerDetails.Add( ply, new Dictionary<PlayerDetail, dynamic>() );
            } else if( PlayerDetails[ply].ContainsKey( detail ) ) {
                return PlayerDetails[ply][detail];
            }
            return null;
        }

    }

    public enum PlayerDetail {
        TEAM,
        SCORE
    }

}
