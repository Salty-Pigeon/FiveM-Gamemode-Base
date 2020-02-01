using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTTClient {
    class TTTHUD : HUD {
        public override void Draw() {
            DrawRectangle( 0.025f, 0.86f, 0.07f, 0.03f, 200, 0, 0, 200 );       
            DrawHealth();
            DrawScore();
            TeamText.Draw();
            base.Draw();
        }
    }
}
