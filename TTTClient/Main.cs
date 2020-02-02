using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace TTTClient
{
    public class Main : BaseGamemode
    {

        public Main() : base( "TTT" ) {
            HUD = new TTTHUD();
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

            Globals.WriteChat( "TTT", "Game started", 255, 0, 0 );

        }

        public override void Update() {
            base.Update();
        }

        public override void SetTeam( int team ) {
            base.SetTeam( team );
            switch( team ) {
                case 0:
                    HUD.TeamText.Caption = "Innocent";
                    break;
                case 1:
                    HUD.TeamText.Caption = "Traitor";
                    break;
                case 2:
                    HUD.TeamText.Caption = "Detective";
                    break;
                default:
                    HUD.TeamText.Caption = "Spectator";
                    break;
            }
        }
    }
}
