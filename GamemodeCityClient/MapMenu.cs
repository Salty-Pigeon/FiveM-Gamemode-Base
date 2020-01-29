using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System.Drawing;

namespace GamemodeCityClient {
    public class MapMenu : BaseScript {

        Map currentMap;

        private double DegreeToRadian( double angle ) {
            return Math.PI * angle / 180.0;
        }

        public MapMenu( string name, string subtitle, Dictionary<string, Map> Maps ) {

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            Menu mapMenu = new Menu( name, subtitle ) { Visible = true };
            MenuController.AddMenu( mapMenu );



            //Vector3 selectedVector = map.Value.SpawnPoints.ElementAt( 0 ).Value[0];
            //int selectedTeam = map.Value.SpawnPoints.ElementAt( 0 ).Key;


            Menu mapEditor = AddSubMenu( mapMenu, "Edit Map" );


            // Add each map

            //MenuItem mapItem = AddMenuItem( mapMenu, mapEditor, "Map", "Modify Map", "", true );
            //MenuItem mapItem1 = AddMenuItem( mapMenu, mapEditor, "Map1", "Modify Map", "", true );


            // base options code
            Menu playerSpawnMenu = AddSubMenu( mapEditor, "Edit " + "Map" + " player spawns" );
            Menu deleteMapMenu = AddSubMenu( mapEditor, "Delete " + "Map" + "?" );
            deleteMapMenu.AddMenuItem( new MenuItem( "Yes", "" ) );
            deleteMapMenu.AddMenuItem( new MenuItem( "No", "" ) );
            deleteMapMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Yes" ) {

                }
                if( _item.Text == "No" ) {
                    deleteMapMenu.CloseMenu();
                }
            };
            mapEditor.AddMenuItem( new MenuItem( "Show/Hide" ) );

            mapEditor.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Show/Hide" ) {

                }
            };

            Menu modifyPosMenu = AddSubMenu( playerSpawnMenu, "Edit position" );

            Vector2 dimensions = new Vector2( 100, 100 );

            MenuSliderItem sliderOffset = new MenuSliderItem( "Offset: 1", -25, 25, 1, false );
            MenuSliderItem sliderX = new MenuSliderItem( "Centre X: ", -999999, 999999, (int)dimensions.X, false );
            MenuSliderItem sliderY = new MenuSliderItem( "Centre Y", -999999, 999999, (int)dimensions.Y, false );
            MenuSliderItem sliderWidth = new MenuSliderItem( "Width", -9999, 9999, (int)dimensions.X, false );
            MenuSliderItem sliderLength = new MenuSliderItem( "Length", -9999, 9999, (int)dimensions.Y, false );

            mapEditor.AddMenuItem( sliderOffset );
            mapEditor.AddMenuItem( sliderX );
            mapEditor.AddMenuItem( sliderY );
            mapEditor.AddMenuItem( sliderWidth );
            mapEditor.AddMenuItem( sliderLength );

            MenuItem playerSpawnItem = AddMenuItem( mapEditor, playerSpawnMenu, "Player Spawns", "Modify player spawn points", "", true );
            MenuItem deleteMapItem = AddMenuItem( mapEditor, deleteMapMenu, "Delete Map", "Delete entire map", "", true );

            modifyPosMenu.AddMenuItem( new MenuItem( "Delete", "Deletes the selected position" ) );
            mapEditor.AddMenuItem( new MenuItem( "Save", "Saves new position and size" ) );




            mapEditor.OnSliderPositionChange += ( _menu, _sliderItem, _oldPosition, _newPosition, _itemIndex ) => {
                if( _sliderItem == sliderOffset ) {
                    _sliderItem.Text = "Offset: " + _newPosition;
                }
                if( _sliderItem == sliderX ) {
                    currentMap.Position.X += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Centre X: " + currentMap.Position.X;
                }
                if( _sliderItem == sliderY ) {
                    currentMap.Position.Y += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Centre Y: " + currentMap.Position.Y;
                }
                if( _sliderItem == sliderWidth ) {
                    currentMap.Size.X += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Width: " + currentMap.Size.X;
                }
                if( _sliderItem == sliderLength ) {
                    currentMap.Size.Y += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Height: " + currentMap.Size.Y;
                }


            };

            mapEditor.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Save" ) {
                    Globals.SendMap( currentMap );
                    mapEditor.CloseMenu();
                }
            };

            modifyPosMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Delete" ) {

                };
            };


            // Create Map

            MenuItem createMap = AddMenuItem( mapMenu, mapEditor, "Create Map", "Modify Map", "", true );

            mapMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item == createMap ) {
                    currentMap = new Map( "unnamed" + Game.GameTime, LocalPlayer.Character.Position, new Vector3( 0, 0, 0 ) );
                    Debug.WriteLine( "Yay map created" );
                }
                sliderOffset.Position = 1;
                sliderX.Text = "Centre X: " + currentMap.Position.X;
                sliderX.Position = (int)dimensions.X;
                sliderY.Text = "Centre Y: " + currentMap.Position.Y;
                sliderY.Position = (int)dimensions.Y;
                sliderWidth.Text = "Width: " + currentMap.Size.X;
                sliderWidth.Position = (int)dimensions.X;
                sliderLength.Text = "Height: " + currentMap.Size.Y;
                sliderLength.Position = (int)dimensions.Y;
            };

            mapMenu.OnMenuClose += ( _menu ) => {

            };

            mapMenu.OnIndexChange += ( _menu, _oldItem, _newItem, _oldIndex, _newIndex ) => {

            };

        }

        public void MapOptionsMenu( Menu menu ) {
            

        }

        public MenuItem AddMenuItem( Menu parent, Menu child, string name, string description, string label, bool bindMenu ) {
            MenuItem menuItem = new MenuItem( name, description ) { Label = label };
            parent.AddMenuItem( menuItem );
            if( bindMenu ) {
                MenuController.BindMenuItem( parent, child, menuItem );
            }
            return menuItem;
        }

        public Menu AddSubMenu( Menu parent, string name ) {
            Menu subMenu = new Menu( null, name );
            MenuController.AddSubmenu( parent, subMenu );
            return subMenu;
        }
    }
}
