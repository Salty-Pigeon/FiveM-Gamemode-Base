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
            Debug.WriteLine( "Hello world!" );

            base.Tick += Tick;
           
        }

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            Globals.Init();

            RegisterCommand( "tdm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                CurrentGame = Globals.Gamemodes["TDM"];
                CurrentGame.Start();
            } ), false );

            RegisterCommand("noclip", new Action<int, List<object>, string>(( source, args, raw ) => {
                Globals.SetNoClip(!Globals.isNoclip);
            }), false);
        }

        private async Task Tick() {
            if (CurrentGame != null)
                CurrentGame.Update();
        }
    }
}
