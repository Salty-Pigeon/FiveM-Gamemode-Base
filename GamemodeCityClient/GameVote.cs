using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    class GameVote : BaseScript {
        public Menu VoteMenu;

        private bool voted = false;

        public GameVote( Dictionary<string, string> Gamemodes ) {

            MenuController.CloseAllMenus();

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.DisableBackButton = true;


            // Creating the first menu.
            VoteMenu = new Menu( "Vote Gamemode" );
            MenuController.AddMenu( VoteMenu );


            foreach( var map in Gamemodes ) {
                var mapItem = new MenuItem( map.Value );
                VoteMenu.AddMenuItem( mapItem );

                VoteMenu.OnItemSelect += ( _menu, _item, _index ) => {
                    if( _item == mapItem && !voted ) {
                        voted = true;
                        TriggerServerEvent( "salty:netVote", map.Key );
                        MenuController.DisableBackButton = false;
                        VoteMenu.CloseMenu();
                        MenuController.CloseAllMenus();
                    }
                };
            }



        }
    }
}
