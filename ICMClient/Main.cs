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
        Random rand;


        bool CanKill = false;

        List<string> Bikes = new List<string>() {
            "BMX",
            "Cruiser",
            "Fixter",
            "Scorcher",
            "TriBike"
        };

        public Main(  ) : base ( "ICM" ) {
            HUD = new HUD();
            rand = new Random( GetGameTimer() );
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );

        }

        public override void End() {

            if( Truck != null )
                Truck.Delete();
            if( Bike != null )
                Bike.Delete();
            base.End();

        }

        public override void PlayerSpawn() {           
            base.PlayerSpawn();

            if( ClientGlobals.Team == 0 ) {
                SpawnTruck();
            } else if ( ClientGlobals.Team == 1 ) {
                SpawnBike();
            } 

        }

        public override void Update() {
            CantExitVehichles();
            if( ClientGlobals.Team == 0 ) {
                if( !Game.PlayerPed.IsInVehicle() && Truck != null ) {
                    Game.PlayerPed.SetIntoVehicle( Truck, VehicleSeat.Driver );
                }
                uint streetName = 0;
                uint crossingName = 0;
                GetStreetNameAtCoord( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref streetName, ref crossingName );
                if( streetName == 629262578 || crossingName == 629262578 ) {
                    var velocity = Truck.Velocity;
                    var speed = Truck.Speed;
                    Truck.Position = ClientGlobals.LastSpawn;
                    Truck.Heading = 67.7f;
                    Truck.Velocity = velocity * 2;
                    Truck.Speed = speed * 1.5f;
                    SetGameplayCamRelativeHeading( 0 );
                    //AddScore( 1 );
                }
            }
            if( ClientGlobals.Team == 1 && !CanKill ) {
                Game.PlayerPed.CanBeKnockedOffBike = false;
                if( !Game.PlayerPed.IsInVehicle() && Bike != null ) {
                    Game.PlayerPed.SetIntoVehicle( Bike, VehicleSeat.Driver );
                }
                uint streetName = 0;
                uint crossingName = 0;
                GetStreetNameAtCoord( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, ref streetName, ref crossingName );
                if( streetName == 3436239235 || crossingName == 3436239235 ) {
                    CanKill = true;
                    GiveWeaponToPed( PlayerPedId(), (uint)GetHashKey( "weapon_rpg" ), 100, false, true );
                }
            }

            if( CanKill ) {
                SetPedMoveRateOverride( PlayerPedId(), 4f );
                Game.PlayerPed.IsInvincible = true;
                Game.PlayerPed.Weapons.Current.InfiniteAmmo = true;
                Game.PlayerPed.Weapons.Current.InfiniteAmmoClip = true;
            }

            base.Update();

        }

        public async Task SpawnBike() {
            if( Bike != null )
                Bike.Delete();
            Game.PlayerPed.Position = ClientGlobals.LastSpawn;
            Bike = await World.CreateVehicle( Bikes[rand.Next( 0, Bikes.Count )], Game.PlayerPed.Position, 266.6f );
            Bike.MaxSpeed = 12;
            Game.PlayerPed.SetIntoVehicle( Bike, VehicleSeat.Driver );
            SetGameplayCamRelativeHeading( 0 );
        }

        public async Task SpawnTruck() {
            if( Truck != null )
                Truck.Delete();
            Game.PlayerPed.Position = ClientGlobals.LastSpawn;
            Truck = await World.CreateVehicle( "cutter", Game.PlayerPed.Position, 67.7f );
            Truck.CanBeVisiblyDamaged = false;
            Truck.CanEngineDegrade = false;
            Truck.CanTiresBurst = false;
            Truck.CanWheelsBreak = false;
            Truck.EngineHealth = 999999;
            Truck.MaxHealth = 999999;
            Truck.Health = 999999;
            Truck.EnginePowerMultiplier = 100;
            Truck.Gravity = 50;
            Truck.IsInvincible = true;
            Truck.IsFireProof = true;

            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fCamberStiffnesss", 0.1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fMass", 10000f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "FTRACTIONSPRINGDELTAMAX", 100f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSteeringLock", 40f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fDriveInertia", 1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fDriveBiasFront", 0.5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionCurveLateral", 25f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionCurveMax", 5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionCurveMin", 5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionBiasFront", 0.5f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fTractionLossMult", 0.1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSuspensionReboundDamp", 2f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSuspensionCompDamp", 2f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fSuspensionForce", 3f );
            SetVehicleHasStrongAxles( NetworkGetEntityFromNetworkId( Truck.NetworkId ), true );
            SetVehicleHighGear( NetworkGetEntityFromNetworkId( Truck.NetworkId ), 1 );

            Game.PlayerPed.SetIntoVehicle( Truck, VehicleSeat.Driver );

            SetGameplayCamRelativeHeading( 0 );

        }


    }


}
