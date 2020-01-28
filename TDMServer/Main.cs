using GamemodeCityServer;
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

        }

        public override void Start() {
            base.Start();

            Console.WriteLine("TDM Starting");
        }
    }
}
