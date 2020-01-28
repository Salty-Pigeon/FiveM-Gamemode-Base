using GamemodeCityClient;
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
            
        }
    }
}
