using GamemodeCityServer;
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
            base.Start();

            Console.WriteLine("TDM Starting");

            
        }
    }
}
