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

            var gmInfo = GamemodeRegistry.Register( "tdm", "Team Deathmatch",
                "Two teams battle it out. Eliminate the opposing team to win.", "#4a90d9" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 32;
            gmInfo.Tags = new string[] { "Shooter", "Team" };
            gmInfo.Teams = new string[] { "Team A", "Team B" };
            gmInfo.Features = new string[] { "Loadouts" };
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

            GiveWeaponToPed(PlayerPedId(), 3220176749, 100, false, true);
            
        }

    }
}
