using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class BaseGamemode : BaseScript {

        string Gamemode;

        public Map Map;
        public HUD HUD;

        public float GameTimerEnd;

        public Settings Settings = new Settings();

        public BaseGamemode( string gamemode ) {
            Gamemode = gamemode.ToLower();
            if( !Globals.Gamemodes.ContainsKey( Gamemode ) )
                Globals.Gamemodes.Add( Gamemode, this);

        }

        public virtual void Start( float gameTime ) {
            GameTimerEnd = GetGameTimer() + gameTime;
            HUD.Start();
        }

        public virtual void Update() {
            if( HUD != null ) {
                HUD.Draw();
            }
        }

        public virtual void Cleanup() {

        }
        

    }
}
