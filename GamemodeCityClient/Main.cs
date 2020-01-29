using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient
{
    public class Main : BaseScript {

        BaseGamemode CurrentGame;

        public Main() {

            EventHandlers["onClientResourceStart"] += new Action<string>( OnClientResourceStart );
            EventHandlers["salty:StartGame"] += new Action<int>(StartGame);

            base.Tick += Tick;
           
        }

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            Globals.Init();

            RegisterCommand( "tdm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent("salty:netStartGame", 0);
            } ), false );

            RegisterCommand("noclip", new Action<int, List<object>, string>(( source, args, raw ) => {
                Globals.SetNoClip(!Globals.isNoclip);
            }), false);

            RegisterCommand( "maps", new Action<int, List<object>, string>( ( source, args, raw ) => {
                MapMenu menu = new MapMenu( "Maps", "Modify maps", new Dictionary<string, Map>() );
            } ), false );

            RegisterCommand( "mapname", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "saltyMap:netUpdate", new Dictionary<string, dynamic> { { "playerPos", LocalPlayer.Character.Position },  { "name", string.Join( " ", args ) } } );
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
        }
    }
}
