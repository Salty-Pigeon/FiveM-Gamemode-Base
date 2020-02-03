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

        MapMenu MapMenu;

        public Main() {


            EventHandlers["onClientResourceStart"] += new Action<string>( OnClientResourceStart );
            EventHandlers["salty:StartGame"] += new Action<string, float, dynamic>(StartGame);
            EventHandlers["salty:EndGame"] += new Action(EndGame);
            EventHandlers["salty:CacheMap"] += new Action<int, string, string, Vector3, Vector3, dynamic>( CacheMap );
            EventHandlers["salty:OpenMapGUI"] += new Action( OpenMapGUI );
            EventHandlers["salty:Spawn"] += new Action<int, Vector3, uint>( Spawn );
            EventHandlers["salty:SetTeam"] += new Action<int>( SetTeam );
            

            RegisterNUICallback( "salty_nui_loaded", SetNUIReady );
            RegisterNUICallback( "salty_enable", EnableGUI );
            RegisterNUICallback( "salty_disable", DisableGUI );
            RegisterNUICallback( "salty_edit_map", EditMap );

            base.Tick += Tick;
           
        }

        


        void OpenMapGUI() {
            MapMenu = new MapMenu( "Maps", "Modify maps", ClientGlobals.Maps );
        }

        void CacheMap( int id, string name, string gamemodes, Vector3 pos, Vector3 size, dynamic spawns ) {

            ClientMap map = new ClientMap( id, name, gamemodes.Split(',').ToList(), pos, size, false );

            map.SpawnsFromSendable( spawns );

            ClientGlobals.Maps[id] = map;
        }

        private void SetTeam( int team ) {
            if( ClientGlobals.CurrentGame != null )
                ClientGlobals.CurrentGame.SetTeam( team );
        }

        SaltyWeapon testWeapon;

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            SetNuiFocus( false, false );

            ClientGlobals.Init();

            RegisterCommand( "tdm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent("salty:netStartGame", "tdm");
            } ), false );

            RegisterCommand( "ttt", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "ttt" );
            } ), false );

            RegisterCommand("noclip", new Action<int, List<object>, string>(( source, args, raw ) => {
                ClientGlobals.SetNoClip(!ClientGlobals.isNoclip);
            }), false);

            RegisterCommand( "maps", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netOpenMapGUI" );
            } ), false );

            RegisterCommand( "weapon", new Action<int, List<object>, string>( ( source, args, raw ) => {
                //testWeapon = new SaltyWeapon( SaltyEntity.Type.WEAPON, 3220176749, LocalPlayer.Character.Position + new Vector3( 0, 1, 0 ) );
                Spawn( (int)SpawnType.WEAPON, LocalPlayer.Character.Position, 3220176749 );
            } ), false );




        }



        public void StartGame( string ID, float gameLength, dynamic gameWeps ) {

            List<dynamic> weps = gameWeps as List<dynamic>;
       
            foreach( var wep in weps ) {
                if( ClientGlobals.CurrentGame != null && !ClientGlobals.CurrentGame.GameWeapons.Contains(wep) )
                    ClientGlobals.CurrentGame.GameWeapons.Add( wep );
            }

            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.Map.ClearObjects();
            }

            ClientGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ClientGlobals.Gamemodes[ID.ToLower()].GetType() );
            ClientGlobals.CurrentGame.Map = new ClientMap( -1, ID, new List<string>(), new Vector3( 0, 0, 0 ), new Vector3( 0, 0, 0 ), false );
            ClientGlobals.CurrentGame.Start( gameLength );
        }

        public void EndGame() {
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.End();
                ClientGlobals.CurrentGame = null;
            }   
        }
        
        private async Task Tick() {
            if ( ClientGlobals.CurrentGame != null)
                ClientGlobals.CurrentGame.Update();
            if( ClientGlobals.isNoclip )
                ClientGlobals.NoClipUpdate();
            if( MapMenu != null )
                MapMenu.Draw();
            if( testWeapon != null )
                testWeapon.Update();
        }


        private void SetNUIReady( dynamic data, CallbackDelegate _callback ) {
            
        }

        private void EnableGUI( dynamic data, CallbackDelegate _callback ) {
            SetNuiFocus( true, true );
        }

        private void DisableGUI( dynamic data, CallbackDelegate _callback ) {
            SetNuiFocus( false, false );
        }

        private void EditMap( dynamic data, CallbackDelegate _callback ) {
            if( data.name == "mapName" ) {
                ClientGlobals.LastSelectedMap.Name = data.data;
            }
            else if( data.name == "mapGamemode" ) {
                ClientGlobals.LastSelectedMap.Gamemodes = (data.data as string).Split(',').ToList<string>();
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


        public void Spawn( int typ, Vector3 spawn, uint hash ) {
            SpawnType type = (SpawnType)typ;
            if( type == SpawnType.PLAYER ) {
                LocalPlayer.Character.Position = spawn;
            } else if( type == SpawnType.WEAPON ) {
                if( ClientGlobals.CurrentGame != null ) {
                    if( ClientGlobals.CurrentGame.Map == null ) {
                        Debug.WriteLine( "Map null" );
                    } else {
                        SaltyWeapon wep = new SaltyWeapon( SpawnType.WEAPON, hash, spawn );
                        ClientGlobals.CurrentGame.Map.Weapons.Add( wep );
                    }
                } else {
                    Debug.WriteLine( "Game null" );
                }
            }
        }


    }
}
