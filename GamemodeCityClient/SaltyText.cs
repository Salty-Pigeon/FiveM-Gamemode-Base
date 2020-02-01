using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class SaltyText : BaseScript {

        public int Font;
        public bool Proportional;
        public float Scale;
        public System.Drawing.Color Colour;
        public bool Centre = false;
        public bool Outline = false;
        public Vector2 Position;
        public Vector2 Size;
        public string Caption;

        public SaltyText( float x, float y, float width, float height, float scale, string text, int r, int g, int b, int a, bool outline, bool centre, int font, bool proportional ) {
            Position = new Vector2( x, y );
            Size = new Vector2( width, height );
            Scale = scale;
            Caption = text;
            Colour = System.Drawing.Color.FromArgb( a, r, g, b);
            Outline = outline;
            Centre = centre;
            Font = font;
            Proportional = proportional;
        }

        public void Draw() {
            SetTextFont( Font );
            SetTextProportional( true );
            SetTextScale( Scale, Scale );
            SetTextColour( Colour.R, Colour.G, Colour.B, Colour.A );
            //SetTextDropShadow();
            //SetTextEdge( 1, 0, 0, 0, 255 );
            SetTextCentre( Centre );
            if( Outline )
                SetTextOutline();
            SetTextEntry( "STRING" );
            AddTextComponentString( Caption );
            DrawText( Position.X - Size.X / 2, Position.Y - Size.Y / 2 );
        }
    }
}
