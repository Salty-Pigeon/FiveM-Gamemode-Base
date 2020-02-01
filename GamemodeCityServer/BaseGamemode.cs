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

        public Map Map;

        public Settings Settings = new Settings();

        public Dictionary<string, float> PlayerScores = new Dictionary<string, float>();
        public Dictionary<int, float> TeamScores = new Dictionary<int, float>();

        public BaseGamemode( string gamemode ) {
            Gamemode = gamemode.ToLower();
            if( !Globals.Gamemodes.ContainsKey( Gamemode ) )
                Globals.Gamemodes.Add( Gamemode, this);

        }

        public virtual void Start() {
            TriggerEvent( "salty:StartGame", Gamemode );

        }

        public virtual void Update() {

        }

        public virtual void OnPlayerKilled( Player attacker, string victimSrc ) {
            
        }

        public virtual void OnPlayerDied( Player attacker, int killerType, Vector3 deathCoords ) {

        }

        public void Spawn( Player player, int team ) {
            player.TriggerEvent( "salty:Spawn", Map.GetSpawn( SpawnType.PLAYER, team ).Position );
        }

        public void AddScore( Player ply, float amount ) {
            if( PlayerScores.ContainsKey(ply.Handle) ) {
                PlayerScores[ply.Handle] += amount;
            } else {
                PlayerScores.Add( ply.Handle, amount );
            }
        }

    }
}
