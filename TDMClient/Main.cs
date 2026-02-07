using GamemodeCityClient;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace TDMClient
{
    public class Main : BaseGamemode
    {
        
        public Main( ) : base ( "TDM" ) {
            HUD = new HUD();

            GamemodeRegistry.Register( "tdm", "Team Deathmatch",
                "Two teams battle it out. Eliminate the opposing team to win.", "#4a90d9" );
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

            GiveWeaponToPed(PlayerPedId(), 3220176749, 100, false, true);
            
        }

    }
}
