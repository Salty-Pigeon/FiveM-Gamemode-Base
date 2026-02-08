using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityClient;

namespace MVBClient
{

    public enum Teams {
        Trucker,
        Bikie
    }

    public class Main : BaseGamemode
    {
        float justLeftMap = 0;
        Vehicle Truck;
        Vehicle Bike;

        float BoostSpeed = 10;

        Player targetPlayer;
        List<Player> playerList;


        List<string> Bikes = new List<string>() {
            "Akuma",
            "Avarus",
            "Bagger",
            "Bati",
            "BF400",
            "Blazer4",
            "Chimera",
            "Thrust",
            "Sanchez2",
            "Fcr"
        };

        public Main() : base( "MVB" ) {
            HUD = new HUD();

            LocalPlayer.Character.IsInvincible = true;
            playerList = new PlayerList().ToList();
            playerList.Remove( LocalPlayer );
            HUD.SetGameTimePosition( 0, 0, false );

            var gmInfo = GamemodeRegistry.Register( "mvb", "Monster Trucks vs Motorbikes",
                "Monster trucks try to crush motorbike riders in an arena showdown.", "#ff8c00" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 16;
            gmInfo.Tags = new string[] { "Vehicle Combat", "Arena" };
            gmInfo.Teams = new string[] { "Monster Trucks", "Motorbikes" };
            gmInfo.Features = new string[] { "Boost", "Vehicle Physics", "Markers" };
            gmInfo.Guide = new GuideSection {
                Overview = "Arena vehicle combat. One player drives a monster truck trying to crush motorbike riders. Bikes are fast but fragile \u2014 the truck is slow but devastating.",
                HowToWin = "The monster truck wins by crushing all bikers. Eliminated bikers rotate into the truck role.",
                Rules = new string[] {
                    "Driving into water destroys bikes.",
                    "Out of bounds pushes you back.",
                    "Driving on grass gives a speed boost."
                },
                TeamRoles = new GuideTeamRole[] {
                    new GuideTeamRole {
                        Name = "Monster Truck", Color = "#ff8c00",
                        Goal = "Crush the bikers! Indestructible and heavy.",
                        Tips = new string[] { "Use grass boosts to gain speed.", "Use arena edges to trap bikes.", "Be patient and cut off escape routes." }
                    },
                    new GuideTeamRole {
                        Name = "Motorbikes", Color = "#60a5fa",
                        Goal = "Dodge the truck and survive. Fast and nimble but one hit kills.",
                        Tips = new string[] { "Stay near obstacles the truck can't navigate.", "Use grass for speed boosts too.", "Keep moving \u2014 a stationary bike is a dead bike." }
                    }
                },
                Tips = new string[] {
                    "Bikers \u2014 stay near obstacles the truck can't navigate.",
                    "Truck \u2014 use arena edges to trap bikes.",
                    "Grass gives both teams a speed boost."
                }
            };
        }

        public override void Start( float gameTime ) {

            base.Start( gameTime );

            

        }

        Random rand = new Random();
        public override void Update() {
            CantExitVehichles();
            if( Bike != null ) {
                if( Bike.IsInWater && !Bike.IsEngineRunning ) {
                    ExplodeVehicle( Bike );
                    Bike = null;
                }
                if( Bike.HasCollided ) {
                    string mat = Bike.MaterialCollidingWith.ToString();
                    if( mat == "CarMetal" || mat == "Unk" ) {
                        ExplodeVehicle( Bike );
                        Bike = null;
                    }
                    if( mat.Contains( "Grass" ) ) {
                        Bike.Velocity += GetEntityForwardVector( Truck.Handle ) * (BoostSpeed/2);
                    }
                }
            }
            if( Truck != null ) {
                if( Truck.IsInWater && !Truck.IsEngineRunning ) {
                    ExplodeVehicle( Truck );
                    Truck = null;
                }
                if( Truck.HasCollided ) {
                    string mat = Truck.MaterialCollidingWith.ToString();
                    if( mat.Contains("Grass") ) {
                        Truck.Velocity += GetEntityForwardVector( Truck.Handle ) * BoostSpeed;
                    }
                }
                
            }

            if( !LocalPlayer.Character.IsInVehicle() ) {
                if( Team == (int)Teams.Bikie && Bike != null )
                    Game.PlayerPed.SetIntoVehicle( Bike, VehicleSeat.Driver );
                if( Team == (int)Teams.Trucker && Truck != null )
                    Game.PlayerPed.SetIntoVehicle( Truck, VehicleSeat.Driver );

            }

            if( !Map.IsInZone( LocalPlayer.Character.Position ) ) {
                justLeftMap = GetGameTimer();
            } else {
                justLeftMap = 0;
            }

            if( justLeftMap + 1000 > GetGameTimer() ) {
                if( Game.PlayerPed.IsInVehicle() ) {

                    Vector3 direction;
                    if( playerList.Count == 0 ) {
                        direction = Game.PlayerPed.CurrentVehicle.Position - Map.Position;
                    }
                    else {
                        targetPlayer = targetPlayer == null ? playerList[rand.Next( playerList.Count )] : targetPlayer;
                        direction = Game.PlayerPed.CurrentVehicle.Position - targetPlayer.Character.Position;
                    }

                    direction.Z = rand.Next( 300, 500 );
                    direction.Y = -direction.Y;
                    direction.X = -direction.X;
                    Game.PlayerPed.CurrentVehicle.ApplyForce( direction, default, ForceType.MaxForceRot );

                }
            }
            if( justLeftMap + 1100 < GetGameTimer() && targetPlayer != null ) {
                targetPlayer = null;
            }

            if( Team == (int)Teams.Trucker ) {
                foreach( var ply in new PlayerList() ) {
                    object teamObj = GetPlayerDetail( ply.ServerId, "team" );
                    if( teamObj != null && Convert.ToInt32( teamObj ) == (int)Teams.Bikie ) {
                        DrawMarker( 2, ply.Character.Position.X, ply.Character.Position.Y, ply.Character.Position.Z + 2, 0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 2.0f, 2.0f, 2.0f, 200, 20, 20, 50, false, true, 2, false, null, null, false );
                    }
                }
            }
            

            base.Update();
        }

        public void ExplodeVehicle( Vehicle veh ) {
            LocalPlayer.Character.IsInvincible = true;
            LocalPlayer.Character.Kill();
            veh.IsInvincible = false;
            veh.IsExplosionProof = false;
            veh.ExplodeNetworked();
        }

        public override void PlayerSpawn( ) {
            LocalPlayer.Character.IsInvincible = true;
            ClearVehicles();
            if( Team == (int)Teams.Trucker ) {
                HUD.SetGoal( "Destroy all the bikes", 230, 0, 0, 255, 5 );
                SpawnTruck();
            }
            else {
                HUD.SetGoal( "Destroy all the bikes", 230, 0, 0, 255, 5 );
                SpawnBike();
            }
            base.PlayerSpawn( );
        }

        public async Task SpawnBike() {
            if( Bike != null )
                Bike.Delete();
            LocalPlayer.Character.Position = ClientGlobals.LastSpawn;
            Bike = await World.CreateVehicle( Bikes[rand.Next( 0, Bikes.Count )], LocalPlayer.Character.Position, 266.6f );
            if( Bike != null )
                LocalPlayer.Character.SetIntoVehicle( Bike, VehicleSeat.Driver );
            SetGameplayCamRelativeHeading( 0 );
            LocalPlayer.Character.CanBeKnockedOffBike = false;
        }

        public async Task SpawnTruck() {
            if( Truck != null )
                Truck.Delete();
            LocalPlayer.Character.Position = ClientGlobals.LastSpawn;
            Truck = await World.CreateVehicle( "monster", LocalPlayer.Character.Position, -60f );
            Truck.CanBeVisiblyDamaged = false;
            Truck.CanEngineDegrade = false;
            Truck.CanTiresBurst = false;
            Truck.CanWheelsBreak = false;
            Truck.EngineHealth = 999999;
            Truck.MaxHealth = 999999;
            Truck.Health = 999999;
            Truck.EnginePowerMultiplier = 100;
            Truck.Gravity = 35;
            Truck.IsInvincible = true;
            Truck.IsFireProof = true;

            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fCamberStiffnesss", 0.1f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fInitialDragCoeff ", 10f );
            SetVehicleHandlingFloat( NetworkGetEntityFromNetworkId( Truck.NetworkId ), "CHandlingData", "fMass", 10000f );
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

            LocalPlayer.Character.SetIntoVehicle( Truck, VehicleSeat.Driver );

            SetGameplayCamRelativeHeading( 0 );

        }

        public override void End() {
            ClearVehicles();
            base.End();
        }

        public void ClearVehicles() {
            if( Truck != null )
                Truck.Delete();
            if( Bike != null )
                Bike.Delete();
        }
    }
}
