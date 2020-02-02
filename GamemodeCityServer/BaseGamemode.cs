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

        public Dictionary<string, float> PlayerScores = new Dictionary<string, float>();
        public Dictionary<int, float> TeamScores = new Dictionary<int, float>();

        public BaseGamemode( string gamemode ) {
            Gamemode = gamemode.ToLower();
            if( !ServerGlobals.Gamemodes.ContainsKey( Gamemode ) )
                ServerGlobals.Gamemodes.Add( Gamemode, this);

        }

        public virtual void Start( ) {
            TriggerClientEvent( "salty:StartGame", Gamemode, Settings.GameLength, Settings.Weapons );
        }

        public virtual void Update() {

        }

        public virtual void OnPlayerKilled( Player attacker, string victimSrc ) {
            
        }

        public virtual void OnPlayerDied( Player attacker, int killerType, Vector3 deathCoords ) {

        }

        public void SpawnPlayer( Player player, int team ) {
            player.TriggerEvent( "salty:Spawn", (int)SpawnType.PLAYER, Map.GetSpawn( SpawnType.PLAYER, team ).Position, 0 );
        }

        public void SpawnWeapon( Vector3 pos, uint hash ) {
            Debug.WriteLine( "Spawning weapon " + Globals.Weapons[hash]["Name"] );
            TriggerClientEvent( "salty:Spawn", (int)SpawnType.WEAPON, pos, hash );
        }

        public void AddScore( Player ply, float amount ) {
            if( PlayerScores.ContainsKey(ply.Handle) ) {
                PlayerScores[ply.Handle] += amount;
            } else {
                PlayerScores.Add( ply.Handle, amount );
            }
        }

        public void SetTeam( Player ply, int team ) {
            ply.TriggerEvent( "salty:SetTeam", team );
        }

    }
}
