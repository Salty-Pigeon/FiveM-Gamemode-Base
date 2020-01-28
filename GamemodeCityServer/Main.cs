using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityServer
{
    public class Main : BaseScript
    {

        BaseGamemode CurrentGame;

        public Main() {
            Debug.WriteLine( "Hello world!" );

            EventHandlers["salty:netStartGame"] += new Action<Player, int>(StartGame);
        }

        public void StartGame( [FromSource] Player ply, int ID ) {
            TriggerClientEvent("salty:StartGame", ID);
        }
    }
}
