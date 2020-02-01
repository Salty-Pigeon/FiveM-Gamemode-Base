using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using System.Dynamic;
using GamemodeCityShared;

namespace GamemodeCityClient
{
    public class Main : BaseScript {

        BaseGamemode CurrentGame;
        MapMenu MapMenu;

        public Main() {


            EventHandlers["onClientResourceStart"] += new Action<string>( OnClientResourceStart );
            EventHandlers["salty:StartGame"] += new Action<string>(StartGame);
            EventHandlers["salty:CacheMap"] += new Action<int, string, dynamic, Vector3, Vector3, dynamic>( CacheMap );
            EventHandlers["salty:OpenMapGUI"] += new Action( OpenMapGUI );
            EventHandlers["salty:Spawn"] += new Action<Vector3>( Spawn );


            RegisterNUICallback( "salty_nui_loaded", SetNUIReady );
            RegisterNUICallback( "salty_enable", EnableGUI );
            RegisterNUICallback( "salty_disable", DisableGUI );
            RegisterNUICallback( "salty_edit_map", EditMap );

            base.Tick += Tick;
           
        }

        


        void OpenMapGUI() {
            MapMenu = new MapMenu( "Maps", "Modify maps", Globals.Maps );
        }

        void CacheMap( int id, string name, dynamic gamemodes, Vector3 pos, Vector3 size, dynamic spawns ) {
            List<string> gamemode = gamemodes as List<string>;

            ClientMap map = new ClientMap( id, name, gamemode, pos, size, false );

            map.SpawnsFromSendable( spawns );

            Globals.Maps[id] = map;
        }

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            SetNuiFocus( false, false );

            Globals.Init();

            RegisterCommand( "tdm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent("salty:netStartGame", "tdm");
            } ), false );

            RegisterCommand( "ttt", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "ttt" );
            } ), false );

            RegisterCommand("noclip", new Action<int, List<object>, string>(( source, args, raw ) => {
                Globals.SetNoClip(!Globals.isNoclip);
            }), false);

            RegisterCommand( "maps", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netOpenMapGUI" );
            } ), false );


            RegisterCommand( "mapname", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( Globals.LastSelectedMap != null )
                    TriggerServerEvent( "saltyMap:netUpdate", new Dictionary<string, dynamic> { { "create", false }, { "id", Globals.LastSelectedMap.ID }, { "name", string.Join( " ", args ) } } );
                else
                    Globals.WriteChat( "Error", "Select a map with /maps", 255, 20, 20 );
            } ), false );


        }



        public void StartGame( string ID ) {
            Debug.WriteLine( "yala" );
            CurrentGame = (BaseGamemode)Activator.CreateInstance( Globals.Gamemodes[ID.ToLower()].GetType() );
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


        private void SetNUIReady( dynamic data, CallbackDelegate _callback ) {
            Debug.WriteLine( "NUI READY TO BE USED!!!" );
        }

        private void EnableGUI( dynamic data, CallbackDelegate _callback ) {
            SetNuiFocus( true, true );
        }

        private void DisableGUI( dynamic data, CallbackDelegate _callback ) {
            SetNuiFocus( false, false );
        }

        private void EditMap( dynamic data, CallbackDelegate _callback ) {
            if( data.name == "mapName" ) {
                Debug.WriteLine( data.data );
                Globals.LastSelectedMap.Name = data.data;
            }
            else if( data.name == "mapGamemode" ) {
                Globals.LastSelectedMap.Gamemodes = (data.data as string).Split(',').ToList<string>();
            }
        }

        public void SetNuiFocus( bool _focus, bool _cursor ) {
            API.SetNuiFocus( _focus, _cursor );
        }

        public void RegisterNUICallback( string _type, Action<ExpandoObject, CallbackDelegate> _callback ) {
            API.RegisterNuiCallbackType( _type );
            try {
                EventHandlers.Add( $"__cfx_nui:{_type}", _callback );
            }
            catch {

            }
        }


        public void Spawn( Vector3 spawn ) {
            LocalPlayer.Character.Position = spawn;
        }


    }
}
