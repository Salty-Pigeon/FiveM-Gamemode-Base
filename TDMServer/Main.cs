using GamemodeCityServer;
using GamemodeCityShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMServer
{
    public class Main : BaseGamemode
    {

       
        
        public Main() : base("TDM") {
            Settings.Weapons =  new List<uint>(){ 2725352035, 453432689, 736523883, 3220176749 };
        }

        public override void Start() {

            Globals.WriteChat( "TDM", "Game started", 255, 0, 0 );
            base.Start();
            
        }

        public override void OnPlayerKilled( Player attacker, string victimSrc ) {

            AddScore( attacker, 1 );

            base.OnPlayerKilled( attacker, victimSrc );
        }
    }
}
