using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMClient
{
    public class Main : BaseGamemode
    {
        
        public Main( ) : base ( "TDM" ) {
           
        }

        public override void Start() {
            base.Start();

            Console.WriteLine("TDM Starting");
            Globals.WriteChat("TDM:", "Round begin.", 255, 255, 255 );

            GiveWeaponToPed(PlayerPedId(), 3220176749, (int)Globals.Weapons[3220176749]["DefaultClipSize"], false, true);
            
        }
    }
}
