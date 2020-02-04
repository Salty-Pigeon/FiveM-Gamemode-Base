using GamemodeCityClient;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTTClient {
    class TraitorMenu : BaseScript {

        private Menu buyMenu;

        public TraitorMenu( string name, string subtitle ) {

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;

            // Creating the first menu.
            buyMenu = new Menu( "Buy Menu", "Traitor" );
            MenuController.AddMenu( buyMenu );


            var radar = new MenuItem( "(1) Radar" );
            var teleport = new MenuItem( "(1) Teleport" );
            buyMenu.AddMenuItem( radar );
            buyMenu.AddMenuItem( teleport );

            buyMenu.OnItemSelect += ( _menu, _item, _index ) => {

                if( _item == radar ) {
                    if( ClientGlobals.BuyItem( 1 ) ) {
                        ((TTTHUD)(ClientGlobals.CurrentGame.HUD)).SetRadarActive( true );
                    }
                }

                if( _item == teleport ) {
                    if( ClientGlobals.BuyItem( 1 ) ) {
                        ((Main)(ClientGlobals.CurrentGame)).CanTeleport = true;
                    }
                }
            };

        }

    }
}
