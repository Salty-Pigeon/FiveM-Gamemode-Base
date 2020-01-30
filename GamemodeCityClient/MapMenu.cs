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
        Dictionary<MenuItem, Map> mapIndex = new Dictionary<MenuItem, Map>();


        private double DegreeToRadian( double angle ) {
            return Math.PI * angle / 180.0;
        }


        public void EditMapMenu( Menu parent, Map map ) {

            Menu playerSpawnMenu = AddSubMenu( parent, "Edit " + map.Name + " player spawns" );
            Menu deleteMapMenu = AddSubMenu( parent, "Delete " + map.Name + "?" );
            deleteMapMenu.AddMenuItem( new MenuItem( "Yes", "" ) );
            deleteMapMenu.AddMenuItem( new MenuItem( "No", "" ) );
            deleteMapMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Yes" ) {

                }
                if( _item.Text == "No" ) {
                    deleteMapMenu.CloseMenu();
                }
            };
            parent.AddMenuItem( new MenuItem( "Show/Hide" ) );
            parent.AddMenuItem( new MenuItem( "Teleport to" ) );


            parent.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Show/Hide" ) {

                }
                if( _item.Text == "Teleport to" ) {

                }
            };

            Menu modifyPosMenu = AddSubMenu( playerSpawnMenu, "Edit position" );


            Vector2 dimensions = new Vector2( 100, 100 );

            MenuSliderItem sliderOffset = new MenuSliderItem( "Offset: 1", -25, 25, 1, false );
            MenuSliderItem sliderX = new MenuSliderItem( "Centre X: ", -999999, 999999, (int)dimensions.X, false );
            MenuSliderItem sliderY = new MenuSliderItem( "Centre Y: ", -999999, 999999, (int)dimensions.Y, false );
            MenuSliderItem sliderWidth = new MenuSliderItem( "Width: ", -9999, 9999, (int)dimensions.X, false );
            MenuSliderItem sliderLength = new MenuSliderItem( "Length: ", -9999, 9999, (int)dimensions.Y, false );

            parent.AddMenuItem( sliderOffset );
            parent.AddMenuItem( sliderX );
            parent.AddMenuItem( sliderY );
            parent.AddMenuItem( sliderWidth );
            parent.AddMenuItem( sliderLength );

            MenuItem playerSpawnItem = AddMenuItem( parent, playerSpawnMenu, "Player Spawns", "Modify player spawn points", "", true );
            MenuItem deleteMapItem = AddMenuItem( parent, deleteMapMenu, "Delete Map", "Delete entire map", "", true );

            modifyPosMenu.AddMenuItem( new MenuItem( "Delete", "Deletes the selected position" ) );
            parent.AddMenuItem( new MenuItem( "Save", "Saves new position and size" ) );

            parent.OnSliderPositionChange += ( _menu, _sliderItem, _oldPosition, _newPosition, _itemIndex ) => {
                if( _sliderItem == sliderOffset ) {
                    _sliderItem.Text = "Offset: " + _newPosition;
                }
                if( _sliderItem == sliderX ) {
                    map.Position.X += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Centre X: " + map.Position.X;
                }
                if( _sliderItem == sliderY ) {
                    map.Position.Y += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Centre Y: " + map.Position.Y;
                }
                if( _sliderItem == sliderWidth ) {
                    map.Size.X += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Width: " + map.Size.X;
                }
                if( _sliderItem == sliderLength ) {
                    map.Size.Y += (_newPosition - _oldPosition) * sliderOffset.Position;
                    _sliderItem.Text = "Length: " + map.Size.Y;
                }


            };

            parent.OnMenuOpen += ( _ ) => {
                currentMap = map;
                if( map.JustCreated ) {
                    map.Position = LocalPlayer.Character.Position;
                    map.Size = new Vector3( 0, 0, 0 );
                    map.Name = "unnamed" + Game.GameTime;
                }
                sliderX.Text = "Centre X: " + map.Position.X;
                sliderY.Text = "Centre Y: " + map.Position.Y;
                sliderWidth.Text = "Width: " + map.Size.X;
                sliderLength.Text = "Length: " + map.Size.Y;
            };

            parent.OnMenuClose += ( _ ) => {

            };

            parent.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Save" ) {
                    Globals.SendMap( map );
                    parent.CloseMenu();
                    TriggerServerEvent( "salty:netOpenMapGUI" );
                }
            };

        }

        public MapMenu( string name, string subtitle, Dictionary<int, Map> Maps ) {


            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            Menu mapMenu = new Menu( name, subtitle ) { Visible = true };
            MenuController.AddMenu( mapMenu );

            Menu createMapSubMenu = AddSubMenu( mapMenu, "Create map" );
            MenuItem createMapItem = AddMenuItem( mapMenu, createMapSubMenu, "Create Map", "Modify Map", "", true );
            EditMapMenu( createMapSubMenu, new Map( "unnamed" + Game.GameTime, LocalPlayer.Character.Position, new Vector3( 0, 0, 0 ) ) );

            foreach( var map in Maps ) {
                Menu mapSubMenu = AddSubMenu( mapMenu, "Edit " + map.Value.Name );
                MenuItem mapItem = AddMenuItem( mapMenu, mapSubMenu, map.Value.Name, "Modify Map", "", true );
                EditMapMenu( mapSubMenu, map.Value );
                mapIndex.Add( mapItem, map.Value );
            }

            mapMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( mapIndex.ContainsKey( _item ) ) {
                    Globals.LastSelectedMap = mapIndex[_item];
                    currentMap = mapIndex[_item];
                }
            };

            mapMenu.OnIndexChange += ( _menu, _oldItem, _newItem, _oldIndex, _newIndex ) => {
                if( mapIndex.ContainsKey( _newItem ) ) {
                    currentMap = mapIndex[_newItem];
                    Globals.LastSelectedMap = mapIndex[_newItem];
                }
                else {
                    currentMap = null;
                }
            };

            mapMenu.OnMenuClose += ( _ ) => {
                currentMap = null;
                Globals.LastSelectedMap = null;
            };

        }

        public void Draw() {
            currentMap.DrawBoundarys();
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
