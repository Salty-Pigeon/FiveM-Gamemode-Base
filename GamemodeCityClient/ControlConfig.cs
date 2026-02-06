using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public static class ControlConfig {

        // Default controls per gamemode
        // Key: gamemode ID, Value: dictionary of action name → control ID
        private static Dictionary<string, Dictionary<string, int>> Defaults = new Dictionary<string, Dictionary<string, int>>() {
            { "ttt", new Dictionary<string, int>() {
                { "BuyMenu", 244 },          // M - Interaction Menu
                { "SetTeleport", 121 },       // Insert
                { "UseTeleport", 212 },       // Home
                { "Interact", 38 },           // E
                { "Disguise", 243 },          // Tilde ~
                { "DropWeapon", 23 },         // F - Enter
            }},
        };

        // Friendly display names for actions
        private static Dictionary<string, Dictionary<string, string>> ActionNames = new Dictionary<string, Dictionary<string, string>>() {
            { "ttt", new Dictionary<string, string>() {
                { "BuyMenu", "Buy Menu" },
                { "SetTeleport", "Set Teleport" },
                { "UseTeleport", "Use Teleport" },
                { "Interact", "Interact / Scan Body" },
                { "Disguise", "Toggle Disguise" },
                { "DropWeapon", "Drop Weapon" },
            }},
        };

        /// <summary>
        /// Get the control ID for a gamemode action. Checks saved KVP first, falls back to defaults.
        /// </summary>
        public static int GetControl( string gamemode, string action ) {
            string key = "controls_" + gamemode + "_" + action;
            string val = GetResourceKvpString( key );
            if( !string.IsNullOrEmpty( val ) ) {
                int result;
                if( int.TryParse( val, out result ) ) return result;
            }
            if( Defaults.ContainsKey( gamemode ) && Defaults[gamemode].ContainsKey( action ) ) {
                return Defaults[gamemode][action];
            }
            return -1;
        }

        /// <summary>
        /// Save a custom control binding for a gamemode action.
        /// </summary>
        public static void SetControl( string gamemode, string action, int controlId ) {
            string key = "controls_" + gamemode + "_" + action;
            SetResourceKvp( key, controlId.ToString() );
        }

        /// <summary>
        /// Get all controls for a gamemode (merged: saved overrides + defaults).
        /// </summary>
        public static Dictionary<string, int> GetGamemodeControls( string gamemode ) {
            var controls = new Dictionary<string, int>();
            if( !Defaults.ContainsKey( gamemode ) ) return controls;

            foreach( var kvp in Defaults[gamemode] ) {
                controls[kvp.Key] = GetControl( gamemode, kvp.Key );
            }
            return controls;
        }

        /// <summary>
        /// Get the display name for a gamemode action.
        /// </summary>
        public static string GetActionName( string gamemode, string action ) {
            if( ActionNames.ContainsKey( gamemode ) && ActionNames[gamemode].ContainsKey( action ) ) {
                return ActionNames[gamemode][action];
            }
            return action;
        }

        /// <summary>
        /// Get all action names for a gamemode.
        /// </summary>
        public static List<string> GetActions( string gamemode ) {
            if( Defaults.ContainsKey( gamemode ) ) {
                return Defaults[gamemode].Keys.ToList();
            }
            return new List<string>();
        }

        /// <summary>
        /// Reset all controls for a gamemode back to defaults.
        /// </summary>
        public static void ResetDefaults( string gamemode ) {
            if( !Defaults.ContainsKey( gamemode ) ) return;
            foreach( var action in Defaults[gamemode].Keys ) {
                string key = "controls_" + gamemode + "_" + action;
                DeleteResourceKvp( key );
            }
        }

        /// <summary>
        /// Get a human-readable name for a control ID.
        /// </summary>
        public static string GetControlName( int controlId ) {
            // Map common control IDs to readable names
            if( ControlNames.ContainsKey( controlId ) ) return ControlNames[controlId];
            return "Control " + controlId;
        }

        /// <summary>
        /// Register default controls for a gamemode. Gamemodes call this to add their defaults.
        /// </summary>
        public static void RegisterDefaults( string gamemode, Dictionary<string, int> defaults, Dictionary<string, string> names = null ) {
            Defaults[gamemode] = defaults;
            if( names != null ) ActionNames[gamemode] = names;
        }

        // Common control ID → name mappings
        private static Dictionary<int, string> ControlNames = new Dictionary<int, string>() {
            { 0, "V (Camera)" },
            { 21, "Shift (Sprint)" },
            { 22, "Space (Jump)" },
            { 23, "F (Enter)" },
            { 24, "Left Click (Attack)" },
            { 25, "Right Click (Aim)" },
            { 27, "Up Arrow (Phone)" },
            { 29, "B (Ability)" },
            { 36, "Ctrl (Duck)" },
            { 37, "Tab (Weapon)" },
            { 38, "E (Pickup)" },
            { 44, "Q (Cover)" },
            { 45, "R (Reload)" },
            { 46, "E (Talk)" },
            { 47, "G (Detonate)" },
            { 48, "Z (HUD)" },
            { 51, "E (Context)" },
            { 56, "F9 (Drop Weapon)" },
            { 73, "X (Duck Vehicle)" },
            { 74, "H (Headlights)" },
            { 75, "F (Exit Vehicle)" },
            { 86, "E (Horn)" },
            { 121, "Insert" },
            { 156, "F2 (Map)" },
            { 170, "F1 (Replay)" },
            { 171, "Caps Lock" },
            { 199, "P (Pause)" },
            { 212, "Home" },
            { 213, "End" },
            { 214, "Delete" },
            { 243, "~ (Tilde)" },
            { 244, "M (Interaction)" },
            { 245, "T (Chat)" },
            { 246, "Y (Team Chat)" },
            { 249, "N (Push to Talk)" },
            { 288, "F5" },
            { 289, "F6" },
        };

        /// <summary>
        /// Get all registered gamemodes.
        /// </summary>
        public static List<string> GetRegisteredGamemodes() {
            return Defaults.Keys.ToList();
        }
    }
}
