using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GTA_GameRooShared;

namespace GTA_GameRooClient {
    public class HubNUI : BaseScript {

        private static HubNUI Instance;
        private static string currentControlsGamemode;
        private static bool isOpen = false;
        public static bool IsOpen => isOpen;
        private static bool isMapTabActive = false;

        public HubNUI() {
            Instance = this;

            RegisterNuiCallbackType( "setBind" );
            EventHandlers["__cfx_nui:setBind"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnSetBind );

            RegisterNuiCallbackType( "resetDefaults" );
            EventHandlers["__cfx_nui:resetDefaults"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnResetDefaults );

            RegisterNuiCallbackType( "closeHub" );
            EventHandlers["__cfx_nui:closeHub"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnCloseHub );

            RegisterNuiCallbackType( "selectControlsGamemode" );
            EventHandlers["__cfx_nui:selectControlsGamemode"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnSelectControlsGamemode );

            RegisterNuiCallbackType( "debugAction" );
            EventHandlers["__cfx_nui:debugAction"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnDebugAction );

            RegisterNuiCallbackType( "selectDebugGamemode" );
            EventHandlers["__cfx_nui:selectDebugGamemode"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnSelectDebugGamemode );

            // Map editor callbacks
            RegisterNuiCallbackType( "saveMap" );
            EventHandlers["__cfx_nui:saveMap"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnSaveMap );

            RegisterNuiCallbackType( "deleteMap" );
            EventHandlers["__cfx_nui:deleteMap"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnDeleteMap );

            RegisterNuiCallbackType( "createMap" );
            EventHandlers["__cfx_nui:createMap"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnCreateMap );

            RegisterNuiCallbackType( "teleportToMap" );
            EventHandlers["__cfx_nui:teleportToMap"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnTeleportToMap );

            RegisterNuiCallbackType( "toggleBoundaries" );
            EventHandlers["__cfx_nui:toggleBoundaries"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnToggleBoundaries );

            RegisterNuiCallbackType( "teleportToSpawn" );
            EventHandlers["__cfx_nui:teleportToSpawn"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnTeleportToSpawn );

            RegisterNuiCallbackType( "getPlayerPosition" );
            EventHandlers["__cfx_nui:getPlayerPosition"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnGetPlayerPosition );

            RegisterNuiCallbackType( "selectMapForEdit" );
            EventHandlers["__cfx_nui:selectMapForEdit"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnSelectMapForEdit );

            RegisterNuiCallbackType( "mapsTabClosed" );
            EventHandlers["__cfx_nui:mapsTabClosed"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnMapsTabClosed );

            RegisterNuiCallbackType( "requestMaps" );
            EventHandlers["__cfx_nui:requestMaps"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnRequestMaps );

            RegisterNuiCallbackType( "minimizeHub" );
            EventHandlers["__cfx_nui:minimizeHub"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnMinimizeHub );

            RegisterCommand( "hub", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( isOpen ) {
                    CloseHub();
                } else {
                    OpenHub( "" );
                }
            } ), false );

            RegisterCommand( "controls", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( isOpen ) {
                    CloseHub();
                } else {
                    OpenHub( "controls" );
                }
            } ), false );

            RegisterCommand( "debug", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( PlayerProgression.AdminLevel < 1 ) return;
                if( isOpen ) {
                    CloseHub();
                } else {
                    OpenHub( "debug" );
                }
            } ), false );

            Tick += OnTick;
        }

        private async Task OnTick() {
            if( IsControlJustReleased( 0, 288 ) ) {
                if( isOpen ) {
                    CloseHub();
                } else {
                    if( !MenuAPI.MenuController.IsAnyMenuOpen() ) {
                        // If we were editing a map (hub minimized for boundary preview), reopen to maps tab
                        OpenHub( ClientGlobals.IsEditingMap ? "maps" : "" );
                    }
                }
            }

            // Draw map boundaries/spawns and player coords while editing
            if( ClientGlobals.IsEditingMap && ClientGlobals.LastSelectedMap != null ) {
                ClientGlobals.LastSelectedMap.DrawBoundaries();
                ClientGlobals.LastSelectedMap.DrawSpawns();

                // Draw player position on screen
                Vector3 pos = Game.PlayerPed.Position;
                string coordText = "X: " + F( pos.X ) + "  Y: " + F( pos.Y ) + "  Z: " + F( pos.Z );
                SetTextFont( 4 );
                SetTextScale( 0.0f, 0.4f );
                SetTextColour( 255, 255, 255, 230 );
                SetTextDropshadow( 2, 0, 0, 0, 255 );
                SetTextOutline();
                BeginTextCommandDisplayText( "STRING" );
                AddTextComponentSubstringPlayerName( coordText );
                EndTextCommandDisplayText( 0.5f, 0.02f );
            }

            await Task.FromResult( 0 );
        }

        private static string EscapeJson( string s ) {
            if( s == null ) return "";
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ).Replace( "\n", "\\n" ).Replace( "\r", "" );
        }

        private static string F( float v ) {
            return v.ToString( CultureInfo.InvariantCulture );
        }

        private static string BuildBindingsJson( string gamemodeId ) {
            var actions = ControlConfig.GetActions( gamemodeId );
            var entries = new List<string>();
            foreach( var action in actions ) {
                int controlId = ControlConfig.GetControl( gamemodeId, action );
                string actionName = ControlConfig.GetActionName( gamemodeId, action );
                entries.Add( "\"" + EscapeJson( action ) + "\":{\"controlId\":" + controlId + ",\"actionName\":\"" + EscapeJson( actionName ) + "\"}" );
            }
            return "{" + string.Join( ",", entries ) + "}";
        }

        private static string BuildMapsJson() {
            var entries = new List<string>();
            foreach( var kvp in ClientGlobals.Maps ) {
                entries.Add( BuildMapJson( kvp.Value ) );
            }
            return "[" + string.Join( ",", entries ) + "]";
        }

        private static string BuildMapJson( ClientMap map ) {
            var spawns = new List<string>();
            foreach( var spawn in map.Spawns ) {
                spawns.Add( "{\"id\":" + spawn.ID
                    + ",\"posX\":" + F( spawn.Position.X )
                    + ",\"posY\":" + F( spawn.Position.Y )
                    + ",\"posZ\":" + F( spawn.Position.Z )
                    + ",\"heading\":" + F( spawn.Heading )
                    + ",\"spawnType\":" + (int)spawn.SpawnType
                    + ",\"entity\":\"" + EscapeJson( spawn.Entity ) + "\""
                    + ",\"team\":" + spawn.Team + "}" );
            }

            var gms = new List<string>();
            foreach( var gm in map.Gamemodes ) {
                gms.Add( "\"" + EscapeJson( gm ) + "\"" );
            }

            var verts = new List<string>();
            foreach( var v in map.Vertices ) {
                verts.Add( "{\"x\":" + F( v.X ) + ",\"y\":" + F( v.Y ) + "}" );
            }

            return "{\"id\":" + map.ID
                + ",\"name\":\"" + EscapeJson( map.Name ) + "\""
                + ",\"author\":\"" + EscapeJson( map.Author ) + "\""
                + ",\"description\":\"" + EscapeJson( map.Description ) + "\""
                + ",\"enabled\":" + ( map.Enabled ? "true" : "false" )
                + ",\"gamemodes\":[" + string.Join( ",", gms ) + "]"
                + ",\"rotation\":" + F( map.Rotation )
                + ",\"minPlayers\":" + map.MinPlayers
                + ",\"maxPlayers\":" + map.MaxPlayers
                + ",\"posX\":" + F( map.Position.X )
                + ",\"posY\":" + F( map.Position.Y )
                + ",\"posZ\":" + F( map.Position.Z )
                + ",\"sizeX\":" + F( map.Size.X )
                + ",\"sizeY\":" + F( map.Size.Y )
                + ",\"sizeZ\":" + F( map.Size.Z )
                + ",\"spawns\":[" + string.Join( ",", spawns ) + "]"
                + ",\"vertices\":[" + string.Join( ",", verts ) + "]}";
        }

        public static void OpenHub( string tab, bool isNewPlayer = false ) {
            if( isOpen ) return;
            if( MenuAPI.MenuController.IsAnyMenuOpen() ) return;

            isOpen = true;
            isMapTabActive = ( tab == "maps" );
            string gamemodes = GamemodeRegistry.BuildAllGamemodesJson();
            string debugActions = DebugRegistry.BuildDebugActionsJson();
            string mapsJson = BuildMapsJson();
            string tabJson = tab == "" ? "\"\"" : "\"" + EscapeJson( tab ) + "\"";
            string progressionJson = PlayerProgression.BuildProgressionJson();
            string pedModelsJson = PlayerProgression.BuildPedModelsJson();
            string payload = "{\"type\":\"openHub\",\"tab\":" + tabJson + ",\"gamemodes\":" + gamemodes + ",\"debugActions\":" + debugActions + ",\"maps\":" + mapsJson + ",\"progression\":" + progressionJson + ",\"pedModels\":" + pedModelsJson + ",\"isNewPlayer\":" + ( isNewPlayer ? "true" : "false" ) + "}";
            SendNuiMessage( payload );
            SetNuiFocus( true, true );
        }

        public static void CloseHub() {
            isOpen = false;
            isMapTabActive = false;
            currentControlsGamemode = null;
            ClientGlobals.IsEditingMap = false;
            // Clean up blips
            foreach( var map in ClientGlobals.Maps.Values ) {
                if( map.Draw ) {
                    map.Draw = false;
                    map.RemoveBlip();
                }
            }
            PlayerProgression.StopCustomization( true );
            SetNuiFocus( false, false );
            SendNuiMessage( "{\"type\":\"closeHub\"}" );
        }

        /// <summary>
        /// Hides the hub UI and releases focus, but keeps map editing state active
        /// so boundaries continue drawing. F5 will reopen the hub to maps tab.
        /// </summary>
        public static void MinimizeHub() {
            isOpen = false;
            isMapTabActive = false;
            currentControlsGamemode = null;
            // Keep IsEditingMap and LastSelectedMap — boundaries continue drawing
            PlayerProgression.StopCustomization( true );
            SetNuiFocus( false, false );
            SendNuiMessage( "{\"type\":\"closeHub\"}" );
        }

        private void OnMinimizeHub( IDictionary<string, object> data, CallbackDelegate cb ) {
            MinimizeHub();
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnSelectControlsGamemode( IDictionary<string, object> data, CallbackDelegate cb ) {
            string gamemodeId = data["gamemodeId"].ToString();
            currentControlsGamemode = gamemodeId;

            string bindings = BuildBindingsJson( gamemodeId );
            string payload = "{\"type\":\"showControls\",\"gamemodeId\":\"" + EscapeJson( gamemodeId ) + "\",\"bindings\":" + bindings + "}";
            SendNuiMessage( payload );

            cb( "{\"status\":\"ok\"}" );
        }

        private void OnSelectDebugGamemode( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( PlayerProgression.AdminLevel < 1 ) { cb( "{\"status\":\"ok\"}" ); return; }
            string gamemodeId = data["gamemodeId"].ToString();
            string entities = DebugRegistry.BuildEntitiesJson( gamemodeId );
            string payload = "{\"type\":\"showDebugEntities\",\"gamemodeId\":\"" + EscapeJson( gamemodeId ) + "\",\"entities\":" + entities + "}";
            SendNuiMessage( payload );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnSetBind( IDictionary<string, object> data, CallbackDelegate cb ) {
            string action = data["action"].ToString();
            int controlId = Convert.ToInt32( data["controlId"] );

            ControlConfig.SetControl( currentControlsGamemode, action, controlId );

            string actionName = ControlConfig.GetActionName( currentControlsGamemode, action );
            string keyName = ControlConfig.GetControlName( controlId );
            BaseGamemode.WriteChat( "Controls", actionName + " set to [ " + keyName + " ]", 30, 200, 30 );

            cb( "{\"status\":\"ok\"}" );
        }

        private void OnResetDefaults( IDictionary<string, object> data, CallbackDelegate cb ) {
            ControlConfig.ResetDefaults( currentControlsGamemode );
            BaseGamemode.WriteChat( "Controls", "All controls reset to defaults.", 30, 200, 30 );

            string bindings = BuildBindingsJson( currentControlsGamemode );
            string payload = "{\"type\":\"updateControls\",\"bindings\":" + bindings + "}";
            SendNuiMessage( payload );

            cb( "{\"status\":\"ok\"}" );
        }

        private void OnDebugAction( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( PlayerProgression.AdminLevel < 1 ) { cb( "{\"status\":\"ok\"}" ); return; }
            string gamemodeId = data["gamemodeId"].ToString();
            string actionId = data["actionId"].ToString();

            var action = DebugRegistry.GetAction( gamemodeId, actionId );
            if( action != null ) {
                if( action.NeedsTarget && data.ContainsKey( "targetId" ) ) {
                    int targetId = Convert.ToInt32( data["targetId"] );
                    action.TargetCallback.Invoke( targetId );
                } else if( !action.NeedsTarget ) {
                    action.Callback.Invoke();
                }
            }

            cb( "{\"status\":\"ok\"}" );
        }

        public static void SendDebugEntityUpdate( string gamemodeId ) {
            if( !isOpen ) return;
            string entities = DebugRegistry.BuildEntitiesJson( gamemodeId );
            string payload = "{\"type\":\"updateDebugEntities\",\"entities\":" + entities + "}";
            SendNuiMessage( payload );
        }

        private void OnCloseHub( IDictionary<string, object> data, CallbackDelegate cb ) {
            CloseHub();
            cb( "{\"status\":\"ok\"}" );
        }

        // ==================== Map Editor Callbacks ====================

        private void OnSaveMap( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( PlayerProgression.AdminLevel < 2 ) { cb( "{\"status\":\"ok\"}" ); return; }
            try {
                var mapData = data["map"] as IDictionary<string, object>;
                if( mapData == null ) { cb( "{\"status\":\"error\"}" ); return; }

                int id = Convert.ToInt32( mapData["id"] );
                ClientMap map;

                if( id == -1 || !ClientGlobals.Maps.ContainsKey( id ) ) {
                    // New map — create it
                    map = new ClientMap( -1, "unnamed", new List<string>() { "tdm" }, Vector3.Zero, new Vector3( 100, 100, 0 ), true );
                } else {
                    map = ClientGlobals.Maps[id];
                }

                // Apply form data
                map.Name = mapData.ContainsKey( "name" ) ? mapData["name"].ToString() : map.Name;
                map.Author = mapData.ContainsKey( "author" ) ? mapData["author"].ToString() : map.Author;
                map.Description = mapData.ContainsKey( "description" ) ? mapData["description"].ToString() : map.Description;
                map.Enabled = mapData.ContainsKey( "enabled" ) && Convert.ToBoolean( mapData["enabled"] );
                map.MinPlayers = mapData.ContainsKey( "minPlayers" ) ? Convert.ToInt32( mapData["minPlayers"] ) : map.MinPlayers;
                map.MaxPlayers = mapData.ContainsKey( "maxPlayers" ) ? Convert.ToInt32( mapData["maxPlayers"] ) : map.MaxPlayers;
                map.Position = new Vector3(
                    mapData.ContainsKey( "posX" ) ? Convert.ToSingle( mapData["posX"] ) : map.Position.X,
                    mapData.ContainsKey( "posY" ) ? Convert.ToSingle( mapData["posY"] ) : map.Position.Y,
                    mapData.ContainsKey( "posZ" ) ? Convert.ToSingle( mapData["posZ"] ) : map.Position.Z
                );
                map.Size = new Vector3(
                    mapData.ContainsKey( "sizeX" ) ? Convert.ToSingle( mapData["sizeX"] ) : map.Size.X,
                    mapData.ContainsKey( "sizeY" ) ? Convert.ToSingle( mapData["sizeY"] ) : map.Size.Y,
                    mapData.ContainsKey( "sizeZ" ) ? Convert.ToSingle( mapData["sizeZ"] ) : map.Size.Z
                );
                map.Rotation = mapData.ContainsKey( "rotation" ) ? Convert.ToSingle( mapData["rotation"] ) : map.Rotation;

                // Gamemodes
                if( mapData.ContainsKey( "gamemodes" ) && mapData["gamemodes"] is IList<object> gmList ) {
                    map.Gamemodes = new List<string>();
                    foreach( var gm in gmList ) {
                        map.Gamemodes.Add( gm.ToString() );
                    }
                }

                // Spawns
                if( mapData.ContainsKey( "spawns" ) && mapData["spawns"] is IList<object> spawnList ) {
                    map.Spawns = new List<Spawn>();
                    foreach( var spawnObj in spawnList ) {
                        if( spawnObj is IDictionary<string, object> s ) {
                            var spawn = new Spawn(
                                s.ContainsKey( "id" ) ? Convert.ToInt32( s["id"] ) : -1,
                                new Vector3(
                                    s.ContainsKey( "posX" ) ? Convert.ToSingle( s["posX"] ) : 0,
                                    s.ContainsKey( "posY" ) ? Convert.ToSingle( s["posY"] ) : 0,
                                    s.ContainsKey( "posZ" ) ? Convert.ToSingle( s["posZ"] ) : 0
                                ),
                                (SpawnType)( s.ContainsKey( "spawnType" ) ? Convert.ToInt32( s["spawnType"] ) : 0 ),
                                s.ContainsKey( "entity" ) ? s["entity"].ToString() : "player",
                                s.ContainsKey( "team" ) ? Convert.ToInt32( s["team"] ) : 0,
                                s.ContainsKey( "heading" ) ? Convert.ToSingle( s["heading"] ) : 0f
                            );
                            map.Spawns.Add( spawn );
                        }
                    }
                }

                // Vertices
                if( mapData.ContainsKey( "vertices" ) && mapData["vertices"] is IList<object> vertexList ) {
                    map.Vertices = new List<Vector2>();
                    foreach( var vertObj in vertexList ) {
                        if( vertObj is IDictionary<string, object> v ) {
                            map.Vertices.Add( new Vector2(
                                v.ContainsKey( "x" ) ? Convert.ToSingle( v["x"] ) : 0,
                                v.ContainsKey( "y" ) ? Convert.ToSingle( v["y"] ) : 0
                            ) );
                        }
                    }
                    if( map.Vertices.Count >= 3 ) {
                        map.RecalculateCentroid();
                    }
                }

                ClientGlobals.SendMap( map );

                // Remove the -1 entry so it doesn't duplicate when server sends back the real ID via CacheMap
                if( id == -1 && ClientGlobals.Maps.ContainsKey( -1 ) ) {
                    ClientGlobals.Maps.Remove( -1 );
                }

                BaseGamemode.WriteChat( "Maps", "Map '" + map.Name + "' saved.", 30, 200, 30 );
                cb( "{\"status\":\"ok\"}" );

                // Send updated map data back to NUI via message
                SendNuiMessage( "{\"type\":\"mapSaved\",\"oldId\":" + id + ",\"map\":" + BuildMapJson( map ) + "}" );
            } catch( Exception ex ) {
                Debug.WriteLine( "[GameRoo] Error saving map: " + ex.Message );
                cb( "{\"status\":\"error\"}" );
            }
        }

        private void OnDeleteMap( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( PlayerProgression.AdminLevel < 2 ) { cb( "{\"status\":\"ok\"}" ); return; }
            int mapId = Convert.ToInt32( data["mapId"] );
            if( mapId > 0 ) {
                ClientGlobals.DeleteMap( mapId );
                if( ClientGlobals.Maps.ContainsKey( mapId ) ) {
                    var map = ClientGlobals.Maps[mapId];
                    map.Draw = false;
                    map.RemoveBlip();
                    ClientGlobals.Maps.Remove( mapId );
                }
                ClientGlobals.LastSelectedMap = null;
                ClientGlobals.IsEditingMap = false;
                BaseGamemode.WriteChat( "Maps", "Map deleted.", 200, 30, 30 );
            } else if( mapId == -1 ) {
                // Remove unsaved map
                if( ClientGlobals.Maps.ContainsKey( -1 ) ) {
                    var map = ClientGlobals.Maps[-1];
                    map.Draw = false;
                    map.RemoveBlip();
                    ClientGlobals.Maps.Remove( -1 );
                }
                ClientGlobals.LastSelectedMap = null;
                ClientGlobals.IsEditingMap = false;
            }
            cb( "{\"status\":\"ok\"}" );

            // Send updated maps list to NUI
            SendMapsUpdate();
        }

        private void OnCreateMap( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( PlayerProgression.AdminLevel < 2 ) { cb( "{\"status\":\"ok\"}" ); return; }
            Vector3 pos = Game.PlayerPed.Position;
            var map = new ClientMap( -1, "unnamed_" + Game.GameTime, new List<string>() { "tdm" }, pos, new Vector3( 100, 100, 0 ), true );
            map.Author = Game.Player.Name;
            ClientGlobals.Maps[-1] = map;
            ClientGlobals.LastSelectedMap = map;
            ClientGlobals.IsEditingMap = true;

            cb( "{\"status\":\"ok\"}" );

            // Send the new map to NUI via message (more reliable than fetch response in FiveM)
            string mapJson = BuildMapJson( map );
            SendNuiMessage( "{\"type\":\"mapCreated\",\"map\":" + mapJson + "}" );
        }

        private void OnTeleportToMap( IDictionary<string, object> data, CallbackDelegate cb ) {
            float x = Convert.ToSingle( data["posX"] );
            float y = Convert.ToSingle( data["posY"] );
            float z = Convert.ToSingle( data["posZ"] );
            Game.PlayerPed.Position = new Vector3( x, y, z );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnToggleBoundaries( IDictionary<string, object> data, CallbackDelegate cb ) {
            // Use LastSelectedMap directly — it's the same reference as Maps[id]
            var map = ClientGlobals.LastSelectedMap;
            if( map != null ) {
                // Update position/size from NUI form
                map.Position = new Vector3(
                    Convert.ToSingle( data["posX"] ),
                    Convert.ToSingle( data["posY"] ),
                    Convert.ToSingle( data["posZ"] )
                );
                map.Size = new Vector3(
                    Convert.ToSingle( data["sizeX"] ),
                    Convert.ToSingle( data["sizeY"] ),
                    Convert.ToSingle( data["sizeZ"] )
                );
                map.Rotation = data.ContainsKey( "rotation" ) ? Convert.ToSingle( data["rotation"] ) : map.Rotation;

                // Update vertices from NUI
                if( data.ContainsKey( "vertices" ) && data["vertices"] is IList<object> vertexList ) {
                    map.Vertices = new List<Vector2>();
                    foreach( var vertObj in vertexList ) {
                        if( vertObj is IDictionary<string, object> v ) {
                            map.Vertices.Add( new Vector2(
                                v.ContainsKey( "x" ) ? Convert.ToSingle( v["x"] ) : 0,
                                v.ContainsKey( "y" ) ? Convert.ToSingle( v["y"] ) : 0
                            ) );
                        }
                    }
                    if( map.Vertices.Count >= 3 ) {
                        map.RecalculateCentroid();
                    }
                }

                map.Draw = true;
                map.CreateBlip();
            }
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnTeleportToSpawn( IDictionary<string, object> data, CallbackDelegate cb ) {
            float x = Convert.ToSingle( data["posX"] );
            float y = Convert.ToSingle( data["posY"] );
            float z = Convert.ToSingle( data["posZ"] );
            Game.PlayerPed.Position = new Vector3( x, y, z );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnGetPlayerPosition( IDictionary<string, object> data, CallbackDelegate cb ) {
            Vector3 pos = Game.PlayerPed.Position;
            float heading = Game.PlayerPed.Heading;
            string context = data.ContainsKey( "context" ) ? data["context"].ToString() : "";
            int spawnIndex = data.ContainsKey( "spawnIndex" ) ? Convert.ToInt32( data["spawnIndex"] ) : -1;
            cb( "{\"status\":\"ok\"}" );
            SendNuiMessage( "{\"type\":\"playerPosition\",\"x\":" + F( pos.X ) + ",\"y\":" + F( pos.Y ) + ",\"z\":" + F( pos.Z )
                + ",\"heading\":" + F( heading )
                + ",\"context\":\"" + EscapeJson( context ) + "\",\"spawnIndex\":" + spawnIndex + "}" );
        }

        private void OnSelectMapForEdit( IDictionary<string, object> data, CallbackDelegate cb ) {
            int mapId = Convert.ToInt32( data["mapId"] );
            isMapTabActive = true;
            if( ClientGlobals.Maps.ContainsKey( mapId ) ) {
                ClientGlobals.LastSelectedMap = ClientGlobals.Maps[mapId];
                ClientGlobals.IsEditingMap = true;
            }
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnRequestMaps( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( PlayerProgression.AdminLevel < 2 ) { cb( "{\"status\":\"ok\"}" ); return; }
            // Trigger the server event to fetch maps — server will send salty:CacheMap for each map
            // then salty:OpenMapGUI which will call SendMapsUpdate() since hub is already open
            TriggerServerEvent( "salty:netOpenMapGUI" );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnMapsTabClosed( IDictionary<string, object> data, CallbackDelegate cb ) {
            isMapTabActive = false;
            ClientGlobals.IsEditingMap = false;
            ClientGlobals.LastSelectedMap = null;
            // Clean up blips
            foreach( var map in ClientGlobals.Maps.Values ) {
                if( map.Draw ) {
                    map.Draw = false;
                    map.RemoveBlip();
                }
            }
            cb( "{\"status\":\"ok\"}" );
        }

        public static void SendMapsUpdate() {
            if( !isOpen ) return;
            string mapsJson = BuildMapsJson();
            string payload = "{\"type\":\"updateMaps\",\"maps\":" + mapsJson + "}";
            SendNuiMessage( payload );
        }

        public static void ShowRoleReveal( string team, string color ) {
            string payload = "{\"type\":\"tttRoleReveal\",\"team\":\"" + EscapeJson( team ) + "\",\"color\":\"" + EscapeJson( color ) + "\"}";
            SendNuiMessage( payload );
        }

        public static void ShowCountdown( int count ) {
            string payload = "{\"type\":\"tttCountdown\",\"count\":" + count + "}";
            SendNuiMessage( payload );
        }

        public static void ShowRoundEnd( string winner, string color, string reason ) {
            string payload = "{\"type\":\"tttRoundEnd\",\"winner\":\"" + EscapeJson( winner ) + "\",\"color\":\"" + EscapeJson( color ) + "\",\"reason\":\"" + EscapeJson( reason ) + "\"}";
            SendNuiMessage( payload );
        }

        public static void ShowBodyInspect( string name, string team, string teamColor, string weapon, string deathTime ) {
            string payload = "{\"type\":\"tttBodyInspect\",\"name\":\"" + EscapeJson( name ) + "\",\"team\":\"" + EscapeJson( team ) + "\",\"teamColor\":\"" + EscapeJson( teamColor ) + "\",\"weapon\":\"" + EscapeJson( weapon ) + "\",\"deathTime\":\"" + EscapeJson( deathTime ) + "\"}";
            SendNuiMessage( payload );
        }
    }
}
