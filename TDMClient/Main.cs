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
            gmInfo.Guide = new GuideSection {
                Overview = "Classic team-based shooter. Two teams fight for the highest kill count within the time limit.",
                HowToWin = "The team with the most kills when the timer expires wins.",
                Rules = new string[] {
                    "Pick up weapons from spawn points around the map.",
                    "Each kill scores +1 for your team."
                },
                TeamRoles = new GuideTeamRole[] {
                    new GuideTeamRole {
                        Name = "Team A", Color = "#4a90d9",
                        Goal = "Eliminate Team B players and control weapon spawns.",
                        Tips = new string[] { "Stick with teammates for crossfire advantages.", "Learn the weapon spawn locations." }
                    },
                    new GuideTeamRole {
                        Name = "Team B", Color = "#e94560",
                        Goal = "Eliminate Team A players and control weapon spawns.",
                        Tips = new string[] { "Stick with teammates for crossfire advantages.", "Learn the weapon spawn locations." }
                    }
                },
                Tips = new string[] {
                    "Learn the weapon spawn locations.",
                    "Stick with teammates for crossfire advantages.",
                    "Control the map center for better positioning."
                }
            };
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

            GiveWeaponToPed(PlayerPedId(), 3220176749, 100, false, true);
            
        }

    }
}
