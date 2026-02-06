using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamemodeCityClient {
    public class ControlsMenuNUI : BaseScript {

        private static ControlsMenuNUI Instance;
        private static string currentGamemode;

        // Bindable keys: control ID -> display name (moved from TTTClient/ControlsMenu.cs)
        public static List<KeyValuePair<int, string>> BindableKeys = new List<KeyValuePair<int, string>>() {
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

        public ControlsMenuNUI() {
            Instance = this;

            RegisterNuiCallbackType( "setBind" );
            EventHandlers["__cfx_nui:setBind"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnSetBind );

            RegisterNuiCallbackType( "resetDefaults" );
            EventHandlers["__cfx_nui:resetDefaults"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnResetDefaults );

            RegisterNuiCallbackType( "closeMenu" );
            EventHandlers["__cfx_nui:closeMenu"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnCloseMenu );
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

        public static void OpenMenu( string gamemodeId ) {
            currentGamemode = gamemodeId;

            string bindings = BuildBindingsJson( gamemodeId );
            string payload = "{\"type\":\"openControls\",\"bindings\":" + bindings + "}";

            SendNuiMessage( payload );
            SetNuiFocus( true, true );
        }

        private void OnSetBind( IDictionary<string, object> data, CallbackDelegate cb ) {
            string action = data["action"].ToString();
            int controlId = Convert.ToInt32( data["controlId"] );

            ControlConfig.SetControl( currentGamemode, action, controlId );

            string actionName = ControlConfig.GetActionName( currentGamemode, action );
            string keyName = ControlConfig.GetControlName( controlId );
            BaseGamemode.WriteChat( "Controls", actionName + " set to [ " + keyName + " ]", 30, 200, 30 );

            cb( "{\"status\":\"ok\"}" );
        }

        private void OnResetDefaults( IDictionary<string, object> data, CallbackDelegate cb ) {
            ControlConfig.ResetDefaults( currentGamemode );
            BaseGamemode.WriteChat( "Controls", "All controls reset to defaults.", 30, 200, 30 );

            string bindings = BuildBindingsJson( currentGamemode );
            string payload = "{\"type\":\"updateControls\",\"bindings\":" + bindings + "}";
            SendNuiMessage( payload );

            cb( "{\"status\":\"ok\"}" );
        }

        private void OnCloseMenu( IDictionary<string, object> data, CallbackDelegate cb ) {
            SetNuiFocus( false, false );
            cb( "{\"status\":\"ok\"}" );
        }
    }
}
