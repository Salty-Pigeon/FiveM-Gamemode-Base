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

            DrawRectangle( 0.025f, 0.878f, 0.12f, 0.093f, 122, 127, 140, 255 );


            DrawHealth();
            //DrawScore();
            TeamText.Draw();

            if( Globals.Team == 0 ) {
                DrawRectangle( 0.025f, 0.8782425f, 0.073f, 0.025f, 200, 0, 0, 255 );
                DrawRectangle( 0.025f, 0.8782425f, 0.007f, 0.025f, 150, 0, 0, 255 );
            }

            DrawRectangle( 0.098f, 0.87824f, 0.007f, 0.025f, 68, 74, 96, 255 );

            base.Draw();
        }
    }
}
