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

        string Gamemode;

        public ServerMap Map;

        public Settings Settings = new Settings();

        public Dictionary<int, float> TeamScores = new Dictionary<int, float>();

        public float GameTime = 0;

        public Dictionary<Player, Dictionary<PlayerDetail, dynamic>> PlayerDetails = new Dictionary<Player, Dictionary<PlayerDetail, dynamic>>();

        public List<Player> Spectators = new List<Player>();

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

        }

        public virtual void End() {
            TriggerClientEvent( "salty:EndGame" );
        }

        public virtual void OnPlayerKilled( Player attacker, string victimSrc ) {
            
        }

        public virtual void OnPlayerDied( Player attacker, int killerType, Vector3 deathCoords ) {

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
