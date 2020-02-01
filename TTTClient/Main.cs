using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTTClient
{
    public class Main : BaseGamemode
    {
        public Main() : base( "TTT" ) {
            HUD = new TTTHUD();
        }

        public override void Start() {
            base.Start();

            Globals.WriteChat( "TTT", "Game started", 255, 0, 0 );

        }

        public override void Update() {
            base.Update();
        }
    }
}
