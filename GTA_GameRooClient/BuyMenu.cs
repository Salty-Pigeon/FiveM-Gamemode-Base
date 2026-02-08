using GTA_GameRooClient;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA_GameRooClient {
    public class BuyMenu : BaseScript {

        public Menu buyMenu;

        public BuyMenu( string title, string subtitle ) {
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.CloseAllMenus();
            buyMenu = new Menu( title, subtitle );
            MenuController.AddMenu( buyMenu );
        }

        public void AddItem( string name, int cost, Action callback ) {
            var item = new MenuItem( "(" + cost + ") " + name );
            buyMenu.AddMenuItem( item );
            buyMenu.OnItemSelect += ( _menu, _item, _index ) => {

                if( _item == item ) {
                    if( ClientGlobals.CurrentGame.BuyItem( cost ) ) {
                        callback();                       
                    }
                }

                buyMenu.CloseMenu();

            };
        }
    }
}
