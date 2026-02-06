using GamemodeCityClient;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using GamemodeCityShared;

namespace TTTClient {
    public class ControlsMenu {

        public Menu controlMenu;
        private string gamemode;

        // Bindable keys: control ID → display name
        private static List<KeyValuePair<int, string>> BindableKeys = new List<KeyValuePair<int, string>>() {
            new KeyValuePair<int, string>( 244, "M" ),
            new KeyValuePair<int, string>( 243, "~ (Tilde)" ),
            new KeyValuePair<int, string>( 38, "E" ),
            new KeyValuePair<int, string>( 23, "F" ),
            new KeyValuePair<int, string>( 47, "G" ),
            new KeyValuePair<int, string>( 74, "H" ),
            new KeyValuePair<int, string>( 44, "Q" ),
            new KeyValuePair<int, string>( 48, "Z" ),
            new KeyValuePair<int, string>( 73, "X" ),
            new KeyValuePair<int, string>( 36, "Ctrl" ),
            new KeyValuePair<int, string>( 37, "Tab" ),
            new KeyValuePair<int, string>( 171, "Caps Lock" ),
            new KeyValuePair<int, string>( 121, "Insert" ),
            new KeyValuePair<int, string>( 212, "Home" ),
            new KeyValuePair<int, string>( 213, "End" ),
            new KeyValuePair<int, string>( 214, "Delete" ),
            new KeyValuePair<int, string>( 156, "F2" ),
            new KeyValuePair<int, string>( 288, "F5" ),
            new KeyValuePair<int, string>( 289, "F6" ),
            new KeyValuePair<int, string>( 170, "F1" ),
            new KeyValuePair<int, string>( 29, "B" ),
            new KeyValuePair<int, string>( 249, "N" ),
            new KeyValuePair<int, string>( 0, "V" ),
        };

        // Map action name → its MenuListItem
        private Dictionary<string, MenuListItem> listItems = new Dictionary<string, MenuListItem>();

        public ControlsMenu( string name, string subtitle ) : this( name, subtitle, "ttt" ) { }

        public ControlsMenu( string name, string subtitle, string gamemodeId ) {
            gamemode = gamemodeId;

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.CloseAllMenus();
            controlMenu = new Menu( name, subtitle );
            MenuController.AddMenu( controlMenu );

            BuildMenu();

            // When left/right changes the selected key, save immediately
            controlMenu.OnListIndexChange += OnListIndexChange;
            controlMenu.OnItemSelect += OnItemSelect;
        }

        private void BuildMenu() {
            controlMenu.ClearMenuItems();
            listItems.Clear();

            List<string> keyNames = BindableKeys.Select( k => k.Value ).ToList();

            var actions = ControlConfig.GetActions( gamemode );
            foreach( var action in actions ) {
                int currentControlId = ControlConfig.GetControl( gamemode, action );
                string actionName = ControlConfig.GetActionName( gamemode, action );

                // Find the index of the current binding in our key list
                int currentIndex = BindableKeys.FindIndex( k => k.Key == currentControlId );
                if( currentIndex < 0 ) currentIndex = 0;

                var listItem = new MenuListItem( actionName, keyNames, currentIndex ) {
                    Description = "Use ~b~LEFT/RIGHT~s~ arrows to change the key binding."
                };
                controlMenu.AddMenuItem( listItem );
                listItems[action] = listItem;
            }

            var resetItem = new MenuItem( "~r~Reset All to Defaults" ) {
                Description = "Reset all controls back to their default bindings."
            };
            controlMenu.AddMenuItem( resetItem );
        }

        private void OnListIndexChange( Menu menu, MenuListItem listItem, int oldIndex, int newIndex, int itemIndex ) {
            // Find which action this list item belongs to
            foreach( var kvp in listItems ) {
                if( kvp.Value == listItem ) {
                    int newControlId = BindableKeys[newIndex].Key;
                    string keyName = BindableKeys[newIndex].Value;
                    ControlConfig.SetControl( gamemode, kvp.Key, newControlId );

                    string actionName = ControlConfig.GetActionName( gamemode, kvp.Key );
                    BaseGamemode.WriteChat( "Controls", actionName + " set to [ " + keyName + " ]", 30, 200, 30 );
                    break;
                }
            }
        }

        private void OnItemSelect( Menu menu, MenuItem menuItem, int itemIndex ) {
            // Check if reset was selected
            if( menuItem.Text.Contains( "Reset" ) ) {
                ControlConfig.ResetDefaults( gamemode );
                BaseGamemode.WriteChat( "Controls", "All controls reset to defaults.", 30, 200, 30 );
                MenuController.CloseAllMenus();
            }
        }
    }
}
