using System;
using System.Collections.Generic;
using System.Linq;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class MapMenu : SaltyMenu {

        Dictionary<MenuItem, ClientMap> mapIndex = new Dictionary<MenuItem, ClientMap>();
        private static readonly string[] AvailableGamemodes = { "tdm", "ttt", "icm", "mvb" };

        public void EditMapMenu( Menu parent, ClientMap map ) {

            // -- Map Name --
            MenuItem nameItem = new MenuItem( "Name: " + map.Name, "Use /mapname <name> in chat to change" );
            parent.AddMenuItem( nameItem );

            // -- Author --
            MenuItem authorItem = new MenuItem( "Author: " + map.Author, "Use /mapauthor <name> in chat to change" );
            parent.AddMenuItem( authorItem );

            // -- Enabled Toggle --
            MenuCheckboxItem enabledToggle = new MenuCheckboxItem( "Enabled", "Is this map available for gameplay?", map.Enabled );
            parent.AddMenuItem( enabledToggle );
            parent.OnCheckboxChange += ( _menu, _item, _index, _checked ) => {
                if( _item == enabledToggle ) {
                    map.Enabled = _checked;
                }
            };

            // -- Gamemodes (checkboxes) --
            Menu gamemodeMenu = AddSubMenu( parent, "Gamemodes" );
            MenuItem gamemodeItem = AddMenuItem( parent, gamemodeMenu, "Gamemodes", "Select which gamemodes this map supports", "->", true );

            Dictionary<MenuCheckboxItem, string> gamemodeChecks = new Dictionary<MenuCheckboxItem, string>();
            foreach( string gm in AvailableGamemodes ) {
                bool isChecked = map.Gamemodes.Contains( gm );
                MenuCheckboxItem gmCheck = new MenuCheckboxItem( gm.ToUpper(), "", isChecked );
                gamemodeMenu.AddMenuItem( gmCheck );
                gamemodeChecks[gmCheck] = gm;
            }

            gamemodeMenu.OnCheckboxChange += ( _menu, _item, _index, _checked ) => {
                if( gamemodeChecks.ContainsKey( _item ) ) {
                    string gm = gamemodeChecks[_item];
                    if( _checked && !map.Gamemodes.Contains( gm ) ) {
                        map.Gamemodes.Add( gm );
                    } else if( !_checked && map.Gamemodes.Contains( gm ) ) {
                        map.Gamemodes.Remove( gm );
                    }
                }
            };

            // -- Teleport to Map --
            parent.AddMenuItem( new MenuItem( "Teleport to Map Center", "Teleport to the center of this map" ) );

            // -- Show/Hide Boundaries --
            parent.AddMenuItem( new MenuItem( "Show/Hide Boundaries", "Toggle boundary visualization" ) );

            // -- Position Controls --
            MenuSliderItem sliderOffset = new MenuSliderItem( "Offset: 1", 0, 50, 1, false ) { Description = "Adjustment step size" };
            MenuSliderItem sliderX = new MenuSliderItem( "Centre X: " + (int)map.Position.X, -999999, 999999, 0, false );
            MenuSliderItem sliderY = new MenuSliderItem( "Centre Y: " + (int)map.Position.Y, -999999, 999999, 0, false );
            MenuSliderItem sliderZ = new MenuSliderItem( "Centre Z: " + (int)map.Position.Z, -999999, 999999, 0, false );

            parent.AddMenuItem( sliderOffset );
            parent.AddMenuItem( sliderX );
            parent.AddMenuItem( sliderY );
            parent.AddMenuItem( sliderZ );

            // -- Size Controls --
            MenuSliderItem sliderWidth = new MenuSliderItem( "Width (X): " + (int)map.Size.X, -9999, 9999, 0, false );
            MenuSliderItem sliderLength = new MenuSliderItem( "Length (Y): " + (int)map.Size.Y, -9999, 9999, 0, false );
            MenuSliderItem sliderHeight = new MenuSliderItem( "Height (Z): " + (int)map.Size.Z, -9999, 9999, 0, false ) { Description = "0 = no height limit" };

            parent.AddMenuItem( sliderWidth );
            parent.AddMenuItem( sliderLength );
            parent.AddMenuItem( sliderHeight );

            // -- Player Count --
            MenuSliderItem sliderMinPlayers = new MenuSliderItem( "Min Players: " + map.MinPlayers, 1, 32, map.MinPlayers, false );
            MenuSliderItem sliderMaxPlayers = new MenuSliderItem( "Max Players: " + map.MaxPlayers, 1, 32, map.MaxPlayers, false );

            parent.AddMenuItem( sliderMinPlayers );
            parent.AddMenuItem( sliderMaxPlayers );

            // -- Slider Events --
            parent.OnSliderPositionChange += ( _menu, _sliderItem, _oldPosition, _newPosition, _itemIndex ) => {
                int offset = Math.Max( 1, sliderOffset.Position );

                if( _sliderItem == sliderOffset ) {
                    _sliderItem.Text = "Offset: " + _newPosition;
                }
                if( _sliderItem == sliderX ) {
                    map.Position = new Vector3( map.Position.X + (_newPosition - _oldPosition) * offset, map.Position.Y, map.Position.Z );
                    _sliderItem.Text = "Centre X: " + (int)map.Position.X;
                }
                if( _sliderItem == sliderY ) {
                    map.Position = new Vector3( map.Position.X, map.Position.Y + (_newPosition - _oldPosition) * offset, map.Position.Z );
                    _sliderItem.Text = "Centre Y: " + (int)map.Position.Y;
                }
                if( _sliderItem == sliderZ ) {
                    map.Position = new Vector3( map.Position.X, map.Position.Y, map.Position.Z + (_newPosition - _oldPosition) * offset );
                    _sliderItem.Text = "Centre Z: " + (int)map.Position.Z;
                }
                if( _sliderItem == sliderWidth ) {
                    map.Size = new Vector3( map.Size.X + (_newPosition - _oldPosition) * offset, map.Size.Y, map.Size.Z );
                    _sliderItem.Text = "Width (X): " + (int)map.Size.X;
                }
                if( _sliderItem == sliderLength ) {
                    map.Size = new Vector3( map.Size.X, map.Size.Y + (_newPosition - _oldPosition) * offset, map.Size.Z );
                    _sliderItem.Text = "Length (Y): " + (int)map.Size.Y;
                }
                if( _sliderItem == sliderHeight ) {
                    map.Size = new Vector3( map.Size.X, map.Size.Y, map.Size.Z + (_newPosition - _oldPosition) * offset );
                    _sliderItem.Text = "Height (Z): " + (int)map.Size.Z;
                }
                if( _sliderItem == sliderMinPlayers ) {
                    map.MinPlayers = _newPosition;
                    _sliderItem.Text = "Min Players: " + _newPosition;
                }
                if( _sliderItem == sliderMaxPlayers ) {
                    map.MaxPlayers = _newPosition;
                    _sliderItem.Text = "Max Players: " + _newPosition;
                }
            };

            // -- Spawns Submenu --
            Menu spawnMenu = AddSubMenu( parent, "Spawns - " + map.Name );
            MenuItem spawnMenuItem = AddMenuItem( parent, spawnMenu, "Spawns (" + map.Spawns.Count + ")", "Add and edit spawn points", "->", true );

            // Create spawn option
            Menu createSpawnMenu = AddSubMenu( spawnMenu, "Create New Spawn" );
            MenuItem createSpawnItem = AddMenuItem( spawnMenu, createSpawnMenu, "Create Spawn", "Create spawn at your current position", "->", true );
            BuildSpawnEditor( createSpawnMenu, map, null );

            // Existing spawns
            foreach( var spawn in map.Spawns ) {
                string spawnLabel = spawn.SpawnType.ToString() + " T" + spawn.Team + " #" + spawn.ID;
                Menu editSpawnMenu = AddSubMenu( spawnMenu, "Edit: " + spawnLabel );
                MenuItem editSpawnItem = AddMenuItem( spawnMenu, editSpawnMenu, spawnLabel, "Edit this spawn point", "->", true );
                BuildSpawnEditor( editSpawnMenu, map, spawn );
            }

            // -- Save --
            parent.AddMenuItem( new MenuItem( "~g~Save Map", "Save all changes to this map" ) );

            // -- Delete Map --
            Menu deleteMenu = AddSubMenu( parent, "Delete " + map.Name + "?" );
            MenuItem deleteItem = AddMenuItem( parent, deleteMenu, "~r~Delete Map", "Permanently delete this map", "->", true );
            deleteMenu.AddMenuItem( new MenuItem( "~r~Yes, Delete", "This cannot be undone!" ) );
            deleteMenu.AddMenuItem( new MenuItem( "No, Go Back", "" ) );
            deleteMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "~r~Yes, Delete" && map.ID > 0 ) {
                    ClientGlobals.DeleteMap( map.ID );
                    ClientGlobals.Maps.Remove( map.ID );
                    BaseGamemode.WriteChat( "Maps", "Map '" + map.Name + "' deleted. Use /maps to reopen editor.", 200, 30, 30 );
                    MenuAPI.MenuController.CloseAllMenus();
                }
                if( _item.Text == "No, Go Back" ) {
                    deleteMenu.GoBack();
                }
            };

            // -- Menu item select events --
            parent.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Teleport to Map Center" ) {
                    Game.PlayerPed.Position = map.Position;
                }
                if( _item.Text == "Show/Hide Boundaries" ) {
                    map.Draw = !map.Draw;
                    if( map.Draw ) {
                        map.CreateBlip();
                    } else {
                        map.RemoveBlip();
                    }
                }
                if( _item.Text == "~g~Save Map" ) {
                    ClientGlobals.SendMap( map );
                    BaseGamemode.WriteChat( "Maps", "Map '" + map.Name + "' saved! Use /maps to reopen editor.", 30, 200, 30 );
                    MenuAPI.MenuController.CloseAllMenus();
                }
            };

            // -- Menu open/close events --
            parent.OnMenuOpen += ( _ ) => {
                ClientGlobals.LastSelectedMap = map;
                ClientGlobals.IsEditingMap = true;

                if( map.JustCreated ) {
                    map.Position = Game.PlayerPed.Position;
                    map.Size = new Vector3( 100, 100, 0 );
                    map.Name = "unnamed_" + Game.GameTime;
                    map.Author = Game.Player.Name;
                }

                nameItem.Text = "Name: " + map.Name;
                authorItem.Text = "Author: " + map.Author;
                sliderX.Text = "Centre X: " + (int)map.Position.X;
                sliderY.Text = "Centre Y: " + (int)map.Position.Y;
                sliderZ.Text = "Centre Z: " + (int)map.Position.Z;
                sliderWidth.Text = "Width (X): " + (int)map.Size.X;
                sliderLength.Text = "Length (Y): " + (int)map.Size.Y;
                sliderHeight.Text = "Height (Z): " + (int)map.Size.Z;
                spawnMenuItem.Text = "Spawns (" + map.Spawns.Count + ")";
            };

            parent.OnMenuClose += ( _ ) => {
                ClientGlobals.IsEditingMap = false;
            };
        }

        private void BuildSpawnEditor( Menu spawnEditMenu, Map map, Spawn existingSpawn ) {
            bool isNew = existingSpawn == null;

            MenuListItem spawnTypeList = new MenuListItem( "Type", Enum.GetNames( typeof( SpawnType ) ).ToList(), isNew ? 0 : (int)existingSpawn.SpawnType );
            spawnEditMenu.AddMenuItem( spawnTypeList );

            MenuSliderItem teamSlider = new MenuSliderItem( "Team: " + (isNew ? 0 : existingSpawn.Team), 0, 3, isNew ? 0 : existingSpawn.Team, false );
            spawnEditMenu.AddMenuItem( teamSlider );

            if( !isNew ) {
                spawnEditMenu.AddMenuItem( new MenuItem( "Teleport Here", "Teleport to this spawn point" ) );
                spawnEditMenu.AddMenuItem( new MenuItem( "Move to My Position", "Move this spawn to where you are standing" ) );
            }

            spawnEditMenu.AddMenuItem( new MenuItem( isNew ? "~g~Create Spawn" : "~g~Save Spawn", "" ) );

            if( !isNew ) {
                spawnEditMenu.AddMenuItem( new MenuItem( "~r~Delete Spawn", "Remove this spawn point" ) );
            }

            spawnEditMenu.OnSliderPositionChange += ( _menu, _sliderItem, _oldPosition, _newPosition, _itemIndex ) => {
                if( _sliderItem == teamSlider ) {
                    _sliderItem.Text = "Team: " + _newPosition;
                }
            };

            spawnEditMenu.OnItemSelect += ( _menu, _item, _index ) => {
                if( _item.Text == "Teleport Here" && existingSpawn != null ) {
                    Game.PlayerPed.Position = existingSpawn.Position;
                }
                if( _item.Text == "Move to My Position" && existingSpawn != null ) {
                    existingSpawn.Position = Game.PlayerPed.Position;
                    BaseGamemode.WriteChat( "Maps", "Spawn moved to your position.", 30, 200, 200 );
                }
                if( _item.Text == "~g~Create Spawn" ) {
                    var newSpawn = new Spawn( -1, Game.PlayerPed.Position, (SpawnType)spawnTypeList.ListIndex, "player", teamSlider.Position );
                    map.Spawns.Add( newSpawn );
                    BaseGamemode.WriteChat( "Maps", "Spawn created at your position. Type: " + newSpawn.SpawnType + " Team: " + newSpawn.Team, 30, 200, 200 );
                    spawnEditMenu.GoBack();
                }
                if( _item.Text == "~g~Save Spawn" && existingSpawn != null ) {
                    existingSpawn.SpawnType = (SpawnType)spawnTypeList.ListIndex;
                    existingSpawn.Team = teamSlider.Position;
                    BaseGamemode.WriteChat( "Maps", "Spawn updated.", 30, 200, 200 );
                    spawnEditMenu.GoBack();
                }
                if( _item.Text == "~r~Delete Spawn" && existingSpawn != null ) {
                    map.Spawns.Remove( existingSpawn );
                    BaseGamemode.WriteChat( "Maps", "Spawn deleted.", 200, 30, 30 );
                    spawnEditMenu.GoBack();
                }
            };
        }

        public MapMenu( string name, string subtitle, Dictionary<int, ClientMap> Maps ) {

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            Menu mapMenu = new Menu( name, subtitle ) { Visible = true };
            MenuController.AddMenu( mapMenu );

            // -- Create Map --
            Menu createMapSubMenu = AddSubMenu( mapMenu, "Create New Map" );
            MenuItem createMapItem = AddMenuItem( mapMenu, createMapSubMenu, "~g~Create New Map", "Create a map at your current position", "->", true );
            EditMapMenu( createMapSubMenu, new ClientMap( -1, "unnamed_" + Game.GameTime, new List<string>() { "tdm" }, Game.PlayerPed.Position, new Vector3( 100, 100, 0 ), true ) );

            // -- Existing Maps --
            foreach( var map in Maps ) {
                string mapLabel = map.Value.Name;
                if( !map.Value.Enabled ) mapLabel = "~s~" + mapLabel + " (Disabled)";

                Menu mapSubMenu = AddSubMenu( mapMenu, "Edit: " + map.Value.Name );
                MenuItem mapItem = AddMenuItem( mapMenu, mapSubMenu, mapLabel, "Gamemodes: " + string.Join( ", ", map.Value.Gamemodes ) + " | Spawns: " + map.Value.Spawns.Count, "->", true );
                EditMapMenu( mapSubMenu, map.Value );
                mapIndex.Add( mapItem, map.Value );
            }

            // -- Map info display on selection --
            mapMenu.OnIndexChange += ( _menu, _oldItem, _newItem, _oldIndex, _newIndex ) => {
                if( mapIndex.ContainsKey( _newItem ) ) {
                    ClientGlobals.LastSelectedMap = mapIndex[_newItem];
                } else {
                    ClientGlobals.LastSelectedMap = null;
                }
            };

            mapMenu.OnMenuClose += ( _ ) => {
                ClientGlobals.LastSelectedMap = null;
                ClientGlobals.IsEditingMap = false;
                // Clean up any blips
                foreach( var map in Maps.Values ) {
                    map.RemoveBlip();
                }
            };
        }

        public void Draw() {
            if( ClientGlobals.LastSelectedMap != null ) {
                ClientGlobals.LastSelectedMap.DrawBoundaries();
                ClientGlobals.LastSelectedMap.DrawSpawns();
            }
        }
    }
}
