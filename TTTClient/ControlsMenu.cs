using GamemodeCityClient;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTTClient {
    public class ControlsMenu : BaseScript {

        public Menu controlMenu;
        public ControlsMenu( string name, string subtitle ) {

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.CloseAllMenus();
            // Creating the first menu.
            controlMenu = new Menu( "Controls", "" );
            MenuController.AddMenu( controlMenu );


            var radar = new MenuItem( " [ ` ] to use disguise" );
            var teleport = new MenuItem( "[ INSERT ] to set teleport" );
            var disguise = new MenuItem( "[ HOME ] to use teleport" );
            controlMenu.AddMenuItem( radar );
            controlMenu.AddMenuItem( teleport );
            controlMenu.AddMenuItem( disguise );

        }

    }
}
