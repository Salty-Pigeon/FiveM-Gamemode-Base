using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class HubNUI : BaseScript {

        private static HubNUI Instance;
        private static string currentControlsGamemode;
        private static bool isOpen = false;

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
                        OpenHub( "" );
                    }
                }
            }
            await Task.FromResult( 0 );
        }

        private static string EscapeJson( string s ) {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
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

        public static void OpenHub( string tab ) {
            if( isOpen ) return;
            if( MenuAPI.MenuController.IsAnyMenuOpen() ) return;

            isOpen = true;
            string gamemodes = GamemodeRegistry.BuildAllGamemodesJson();
            string debugActions = DebugRegistry.BuildDebugActionsJson();
            string tabJson = tab == "" ? "\"\"" : "\"" + EscapeJson( tab ) + "\"";
            string payload = "{\"type\":\"openHub\",\"tab\":" + tabJson + ",\"gamemodes\":" + gamemodes + ",\"debugActions\":" + debugActions + "}";
            SendNuiMessage( payload );
            SetNuiFocus( true, true );
        }

        public static void CloseHub() {
            isOpen = false;
            currentControlsGamemode = null;
            SetNuiFocus( false, false );
            SendNuiMessage( "{\"type\":\"closeHub\"}" );
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
