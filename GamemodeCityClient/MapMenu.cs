using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System.Drawing;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class MapMenu : SaltyMenu {

        //public static Map currentMap;
        Dictionary<MenuItem, ClientMap> mapIndex = new Dictionary<MenuItem, ClientMap>();


        private double DegreeToRadian( double angle ) {
            return Math.PI * angle / 180.0;
        }


        public void EditMapMenu( Menu parent, ClientMap map ) {


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
            parent.AddMenuItem( new MenuItem( "Set Map Name" ) );
            parent.AddMenuItem( new MenuItem( "Set Gamemodes" ) );


            parent.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Show/Hide" ) {

                }
                if( _item.Text == "Teleport to" ) {

                }
                if( _item.Text == "Set Map Name" ) {
                    ClientGlobals.SendNUIMessage( "enable", "mapName" );
                }
                if( _item.Text == "Set Gamemodes" ) {
                    ClientGlobals.SendNUIMessage( "enable", "mapGamemode" );
                }
                if( _item.Text == "Save" ) {
                    ClientGlobals.SendMap( map );
                    parent.CloseMenu();
                    TriggerServerEvent( "salty:netOpenMapGUI" );
                }
            };



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

            MenuItem deleteMapItem = AddMenuItem( parent, deleteMapMenu, "Delete Map", "Delete entire map", "", true );



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
                ClientGlobals.LastSelectedMap = map;
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



            Menu playerSpawnMenu = AddSubMenu( parent, "Edit " + map.Name + " player spawns" );
            playerSpawnMenu.AddMenuItem( new MenuItem( "Save" ) );
            MenuItem playerSpawnItem = AddMenuItem( parent, playerSpawnMenu, "Player Spawns", "Modify player spawn points", "", true );



            Menu addSpawnMenu = EditSpawnMenu( playerSpawnMenu, map, new Spawn( -3, LocalPlayer.Character.Position, 0, "player", 0 ) );


            foreach( var spawn in map.Spawns ) {
                Menu editSpawnMenu = EditSpawnMenu( playerSpawnMenu, map, spawn );
            }

            playerSpawnMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Save" ) {
                    ClientGlobals.SendMap( map );
                    playerSpawnMenu.CloseMenu();
                    TriggerServerEvent( "salty:netOpenMapGUI" );
                }
            };

            parent.AddMenuItem( new MenuItem( "Save", "Saves new position and size" ) );


        }

        public Menu EditSpawnMenu( Menu parent, Map map, Spawn spawn ) {

            MenuListItem spawnTypes = new MenuListItem( "Set spawn type", Enum.GetNames( typeof( SpawnType ) ).ToList<string>(), 0 );


            if( spawn.ID == -3 ) {
                spawn = new Spawn( -3, LocalPlayer.Character.Position, 0, "player", 0 );
            }

            Menu spawnEditMenu = AddSubMenu( parent, "Edit spawn" );

            string name = spawn.ID == -3 ? "Create spawn" : spawn.SpawnType.ToString();

            MenuItem spawnEditItem = AddMenuItem( parent, spawnEditMenu, name, "", "", true );
            spawnEditMenu.AddMenuItem( spawnTypes );
            spawnEditMenu.AddMenuItem( new MenuItem( "Save" ) );

            spawnEditMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Save" ) {
                    if( spawn.ID == -3 ) {
                        map.Spawns.Add( new Spawn( -1, LocalPlayer.Character.Position, (SpawnType)spawnTypes.ListIndex, "player", 0 ) );
                        EditSpawnMenu( parent, map, new Spawn( -1, LocalPlayer.Character.Position, (SpawnType)spawnTypes.ListIndex, "player", 0 ) );
                        spawnEditMenu.GoBack();
                    } else {
                        spawn.SpawnType = (SpawnType)spawnTypes.ListIndex;
                        parent.GetCurrentMenuItem().Text = spawn.SpawnType.ToString();
                        spawnEditMenu.GoBack();
                    }
                }
            };

            return spawnEditMenu;
        }

        public MapMenu( string name, string subtitle, Dictionary<int, ClientMap> Maps ) {


            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            Menu mapMenu = new Menu( name, subtitle ) { Visible = true };
            MenuController.AddMenu( mapMenu );

            Menu createMapSubMenu = AddSubMenu( mapMenu, "Create map" );
            MenuItem createMapItem = AddMenuItem( mapMenu, createMapSubMenu, "Create Map", "Modify Map", "", true );
            EditMapMenu( createMapSubMenu, new ClientMap( -1, "unnamed" + Game.GameTime, new List<string>() { "tdm" }, LocalPlayer.Character.Position, new Vector3( 0, 0, 0 ), true ) );

            foreach( var map in Maps ) {
                Menu mapSubMenu = AddSubMenu( mapMenu, "Edit " + map.Value.Name );
                MenuItem mapItem = AddMenuItem( mapMenu, mapSubMenu, map.Value.Name, "Modify Map", "", true );
                EditMapMenu( mapSubMenu, map.Value );
                mapIndex.Add( mapItem, map.Value );
            }

            mapMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( mapIndex.ContainsKey( _item ) ) {
                    ClientGlobals.LastSelectedMap = mapIndex[_item];
                }
            };

            mapMenu.OnIndexChange += ( _menu, _oldItem, _newItem, _oldIndex, _newIndex ) => {
                if( mapIndex.ContainsKey( _newItem ) ) {
                    ClientGlobals.LastSelectedMap = mapIndex[_newItem];
                }
                else {
                    ClientGlobals.LastSelectedMap = null;
                }
            };

            mapMenu.OnMenuClose += ( _ ) => {
                ClientGlobals.LastSelectedMap = null;
            };

        }

        public void Draw() {
            if( ClientGlobals.LastSelectedMap != null ) {
                ClientGlobals.LastSelectedMap.DrawBoundarys();
                ClientGlobals.LastSelectedMap.DrawSpawns();
            }
        }

        


    }
}
