using GamemodeCityClient;
using GamemodeCityShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMClient
{
    public class Main : BaseGamemode
    {

        Vehicle Truck;
        Vehicle Bike;

        public Main(  ) : base ( "ICM" ) {

        }
        /*
        public override void Update() {
            CantExitVehichles();
            if( ClientGlobals.Team == 1 ) {
                if( !Game.PlayerPed.IsInVehicle() && Truck != null ) {
                    Game.PlayerPed.SetIntoVehicle( Truck, VehicleSeat.Driver );
                }
                uint streetName = 0;
                uint crossingName = 0;
                GetStreetNameAtCoord( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref streetName, ref crossingName );
                if( streetName == 629262578 || crossingName == 629262578 ) {
                    var velocity = Truck.Velocity;
                    var speed = Truck.Speed;
                    Truck.Position = PlayerSpawn;
                    Truck.Heading = 67.7f;
                    Truck.Velocity = velocity * 2;
                    Truck.Speed = speed * 1.5f;
                    SetGameplayCamRelativeHeading( 0 );
                    AddScore( 1 );
                }
            }
            if( ClientGlobals.Team == 2 && !canKill ) {
                Game.PlayerPed.CanBeKnockedOffBike = false;
                if( !Game.PlayerPed.IsInVehicle() && Bike != null ) {
                    Game.PlayerPed.SetIntoVehicle( Bike, VehicleSeat.Driver );
                }
                uint streetName = 0;
                uint crossingName = 0;
                GetStreetNameAtCoord( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref streetName, ref crossingName );
                if( streetName == 3436239235 || crossingName == 3436239235 ) {
                    canKill = true;
                    GiveWeaponToPed( PlayerPedId(), (uint)GetHashKey( "weapon_rpg" ), 100, false, true );
                }
            }

            if( canKill ) {
                SetPedMoveRateOverride( PlayerPedId(), 4f );
                Game.PlayerPed.IsInvincible = true;
                Game.PlayerPed.Weapons.Current.InfiniteAmmo = true;
                Game.PlayerPed.Weapons.Current.InfiniteAmmoClip = true;
            }

            base.Update();

        }
        */

    }

    
}
