﻿using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class BaseGamemode : BaseScript {

        string Gamemode;

        public Settings Settings = new Settings();

        public BaseGamemode( string gamemode ) {
            Gamemode = gamemode;
            if( !Globals.Gamemodes.ContainsKey(gamemode) )
                Globals.Gamemodes.Add(gamemode, this);

            Console.WriteLine("Registered gamemode " + Gamemode);

        }

        public virtual void Start() {

        }

        public virtual void Update() {
            
        }

        public virtual void Cleanup() {

        }
        

    }
}
