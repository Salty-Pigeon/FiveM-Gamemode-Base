using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using System.Dynamic;


namespace GamemodeCityClient
{
    public class Main : BaseScript {

        BaseGamemode CurrentGame;
        MapMenu MapMenu;

        public Main() {


            EventHandlers["onClientResourceStart"] += new Action<string>( OnClientResourceStart );
            EventHandlers["salty:StartGame"] += new Action<int>(StartGame);
            EventHandlers["salty:CacheMap"] += new Action<int, string, dynamic, Vector3, Vector3>( CacheMap );
            EventHandlers["salty:OpenMapGUI"] += new Action( OpenMapGUI );

            RegisterNUICallback( "salty_nui_loaded", SetNUIReady );
            RegisterNUICallback( "salty_enable", EnableGUI );
            RegisterNUICallback( "salty_disable", DisableGUI );
            RegisterNUICallback( "salty_map_name", MapName );

            base.Tick += Tick;
           
        }


        void OpenMapGUI() {
            MapMenu = new MapMenu( "Maps", "Modify maps", Globals.Maps );
        }

        void CacheMap( int id, string name, dynamic gamemodes, Vector3 pos, Vector3 size ) {
            List<string> gamemode = gamemodes as List<string>;
            Map map = new Map( id, name, gamemode, pos, size );
            Globals.Maps[id] = map;
        }

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            SetNuiFocus( false, false );

            Globals.Init();

            RegisterCommand( "tdm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent("salty:netStartGame", 0);
            } ), false );

            RegisterCommand("noclip", new Action<int, List<object>, string>(( source, args, raw ) => {
                Globals.SetNoClip(!Globals.isNoclip);
            }), false);

            RegisterCommand( "maps", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netOpenMapGUI" );
            } ), false );

            RegisterCommand( "text", new Action<int, List<object>, string>( ( source, args, raw ) => {
                SendNUIMessage( "enable", "" );
            } ), false );

            RegisterCommand( "mapname", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( Globals.LastSelectedMap != null )
                    TriggerServerEvent( "saltyMap:netUpdate", new Dictionary<string, dynamic> { { "create", false }, { "id", Globals.LastSelectedMap.ID }, { "name", string.Join( " ", args ) } } );
                else
                    Globals.WriteChat( "Error", "Select a map with /maps", 255, 20, 20 );
            } ), false );


        }



        public void StartGame( int ID ) {
            CurrentGame = Globals.Gamemodes["TDM"];
            CurrentGame.Start();
        }
        
        private async Task Tick() {
            if (CurrentGame != null)
                CurrentGame.Update();
            if( Globals.isNoclip )
                Globals.NoClipUpdate();
            if( MapMenu != null )
                MapMenu.Draw();
        }


        public void RegisterEventHandler( string _event, Delegate _action ) {
            try {
                EventHandlers.Add( _event, _action );
            } catch {

            }
        }

        private void SetNUIReady( dynamic data, CallbackDelegate _callback ) {
            Debug.WriteLine( "NUI READY TO BE USED!!!" );
        }

        private void EnableGUI( dynamic data, CallbackDelegate _callback ) {
            SetNuiFocus( true, true );
        }

        private void DisableGUI( dynamic data, CallbackDelegate _callback ) {
            SetNuiFocus( false, false );
        }

        private void MapName( dynamic data, CallbackDelegate _callback ) {
            Debug.WriteLine( "Map name" );
            Debug.WriteLine( data );
        }

        public void SetNuiFocus( bool _focus, bool _cursor ) {
            API.SetNuiFocus( _focus, _cursor );
        }


        private void SendNUIMessage( string name, string message ) {
            API.SendNuiMessage( "{\"type\":\"salty\",\"name\":\"" + name + "\",\"data\":\"" + message + "\"}" );
        }


        public void RegisterNUICallback( string _type, Action<ExpandoObject, CallbackDelegate> _callback ) {
            API.RegisterNuiCallbackType( _type );
            RegisterEventHandler( $"__cfx_nui:{_type}", _callback );
        }


    }
}
