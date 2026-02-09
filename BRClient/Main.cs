using GTA_GameRooClient;
using GTA_GameRooShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BRClient {

    public class Main : BaseGamemode {

        BRHUD brHUD;
        Random rand = new Random();

        // Plane state
        int planeVehicle = 0;
        int pilotPed = 0;
        bool inPlane = false;
        bool hasJumped = false;
        float planeStartX, planeStartY, planeStartZ;
        float planeEndX, planeEndY, planeEndZ;
        float planeSpeed;
        float planeSpawnTime;
        bool planeModelLoaded = false;

        // Zone state (client-side interpolation)
        float zoneCenterX, zoneCenterY;
        float zoneCurrentRadius, zoneTargetRadius;
        float zoneShrinkStart, zoneShrinkEnd;
        int zonePhase = -1;
        int zoneBlip = -1;

        // Zone damage tracking
        float zoneDeathTimer = 0;
        const float ZONE_GRACE = 5000f;

        // Alive count
        int aliveCount = 0;

        // Circle drawing
        const int CIRCLE_SEGMENTS = 64;

        public Main() : base( "BR" ) {
            brHUD = new BRHUD();
            HUD = brHUD;

            var gmInfo = GamemodeRegistry.Register( "br", "Battle Royale",
                "Last player standing wins. Jump from a plane, loot weapons, survive the shrinking zone.", "#e6a020" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 32;
            gmInfo.Tags = new string[] { "Survival", "FFA" };
            gmInfo.Teams = new string[] { };
            gmInfo.Features = new string[] { "Parachute Drop", "Shrinking Zone", "Weapon Loot" };
            gmInfo.Guide = new GuideSection {
                Overview = "Battle Royale: drop from a plane, scavenge weapons, and be the last player standing as the zone shrinks.",
                HowToWin = "Be the last player alive. Stay inside the shrinking zone and eliminate opponents.",
                Rules = new string[] {
                    "You start with a knife and pistol (1 clip).",
                    "Find better weapons scattered across the map.",
                    "The zone shrinks in phases - being outside deals damage.",
                    "Dead players become spectators."
                },
                Tips = new string[] {
                    "Jump early for more loot time, or late for positioning.",
                    "Watch the minimap for the zone boundary.",
                    "Move toward the zone center before it shrinks.",
                    "Pick up weapons on the ground to upgrade your loadout."
                }
            };

            // BR-specific events
            EventHandlers["br:spawnPlane"] += new Action<float, float, float, float, float, float, float>( OnSpawnPlane );
            EventHandlers["br:forceJump"] += new Action( OnForceJump );
            EventHandlers["br:giveStartWeapons"] += new Action( OnGiveStartWeapons );
            EventHandlers["br:zoneUpdate"] += new Action<float, float, float, float, float, float, int>( OnZoneUpdate );
            EventHandlers["br:zoneDamage"] += new Action<int>( OnZoneDamage );
            EventHandlers["br:playerEliminated"] += new Action<int, string, string>( OnPlayerEliminated );
            EventHandlers["br:winner"] += new Action<string>( OnWinner );
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );
            brHUD.AliveCount = 0;
            hasJumped = false;
            inPlane = false;
            zoneDeathTimer = 0;
        }

        public override void End() {
            CleanupPlane();
            RemoveZoneBlip();
            base.End();
        }

        // ===================== PLANE SYSTEM =====================

        async void OnSpawnPlane( float sx, float sy, float sz, float ex, float ey, float ez, float speed ) {
            planeStartX = sx; planeStartY = sy; planeStartZ = sz;
            planeEndX = ex; planeEndY = ey; planeEndZ = ez;
            planeSpeed = speed;

            // Load titan model
            uint titanHash = (uint)GetHashKey( "titan" );
            uint pilotHash = (uint)GetHashKey( "s_m_m_pilot_02" );
            RequestModel( titanHash );
            RequestModel( pilotHash );

            int timeout = 0;
            while( ( !HasModelLoaded( titanHash ) || !HasModelLoaded( pilotHash ) ) && timeout < 100 ) {
                await Delay( 50 );
                timeout++;
            }

            if( !HasModelLoaded( titanHash ) || !HasModelLoaded( pilotHash ) ) {
                WriteChat( "BR", "Failed to load plane model, spawning on ground.", 200, 30, 30 );
                OnForceJump();
                return;
            }

            // Load collision at start
            RequestCollisionAtCoord( sx, sy, sz );

            // Create plane
            float heading = (float)( Math.Atan2( ex - sx, ey - sy ) * ( 180.0 / Math.PI ) );
            planeVehicle = CreateVehicle( titanHash, sx, sy, sz, heading, false, false );
            SetEntityAsMissionEntity( planeVehicle, true, true );
            SetVehicleEngineOn( planeVehicle, true, true, false );
            SetVehicleForwardSpeed( planeVehicle, speed );
            FreezeEntityPosition( planeVehicle, false );

            // Create AI pilot
            pilotPed = CreatePedInsideVehicle( planeVehicle, 4, pilotHash, -1, false, false );
            SetEntityAsMissionEntity( pilotPed, true, true );
            SetBlockingOfNonTemporaryEvents( pilotPed, true );
            SetPedKeepTask( pilotPed, true );
            TaskPlaneMission( pilotPed, planeVehicle, 0, 0, ex, ey, ez, 4, speed, -1f, -1f, ez + 200f, ez - 50f, true );

            SetModelAsNoLongerNeeded( titanHash );
            SetModelAsNoLongerNeeded( pilotHash );

            // Place player in passenger seat
            int ped = PlayerPedId();
            SetPedIntoVehicle( ped, planeVehicle, 1 );
            Game.PlayerPed.IsInvincible = true;

            // Give parachute now so GTA handles freefall -> parachute naturally on exit
            GiveWeaponToPed( ped, 0xFBAB5776, 1, false, false ); // GADGET_PARACHUTE

            inPlane = true;
            planeSpawnTime = GetGameTimer();
            planeModelLoaded = true;
        }

        void OnPlayerLeftPlane() {
            if( hasJumped ) return;
            hasJumped = true;
            inPlane = false;

            Game.PlayerPed.IsInvincible = false;
            TriggerServerEvent( "br:playerJumped" );
            SchedulePlaneCleanup();
        }

        void OnForceJump() {
            if( hasJumped ) return;
            hasJumped = true;
            inPlane = false;

            Game.PlayerPed.IsInvincible = false;
            TriggerServerEvent( "br:playerJumped" );
            SchedulePlaneCleanup();
        }

        async void SchedulePlaneCleanup() {
            await Delay( 15000 );
            CleanupPlane();
        }

        void CleanupPlane() {
            if( pilotPed != 0 && DoesEntityExist( pilotPed ) ) {
                SetEntityAsMissionEntity( pilotPed, false, true );
                DeletePed( ref pilotPed );
            }
            if( planeVehicle != 0 && DoesEntityExist( planeVehicle ) ) {
                SetEntityAsMissionEntity( planeVehicle, false, true );
                DeleteVehicle( ref planeVehicle );
            }
            pilotPed = 0;
            planeVehicle = 0;
        }

        // ===================== WEAPONS =====================

        void OnGiveStartWeapons() {
            int ped = PlayerPedId();
            GiveWeaponToPed( ped, 0x99B507EA, 0, false, false );  // WEAPON_KNIFE
            GiveWeaponToPed( ped, 0x1B06D571, 12, false, false ); // WEAPON_PISTOL (1 clip)
        }

        // ===================== ZONE =====================

        void OnZoneUpdate( float cx, float cy, float currentR, float targetR, float shrinkStart, float shrinkEnd, int phase ) {
            zoneCenterX = cx;
            zoneCenterY = cy;
            zoneCurrentRadius = currentR;
            zoneTargetRadius = targetR;
            zoneShrinkStart = shrinkStart;
            zoneShrinkEnd = shrinkEnd;
            zonePhase = phase;

            brHUD.ZonePhase = phase;
            UpdateZoneBlip();
        }

        void OnZoneDamage( int damage ) {
            int ped = PlayerPedId();
            int health = GetEntityHealth( ped );
            int newHealth = health - damage;
            if( newHealth < 0 ) newHealth = 0;
            SetEntityHealth( ped, newHealth );
        }

        void UpdateZoneBlip() {
            RemoveZoneBlip();
            zoneBlip = AddBlipForRadius( zoneCenterX, zoneCenterY, 0f, zoneCurrentRadius );
            SetBlipColour( zoneBlip, 1 ); // Red
            SetBlipAlpha( zoneBlip, 128 );
        }

        void RemoveZoneBlip() {
            if( zoneBlip != -1 ) {
                RemoveBlip( ref zoneBlip );
                zoneBlip = -1;
            }
        }

        float InterpolateZoneRadius() {
            if( zoneShrinkEnd <= zoneShrinkStart ) return zoneCurrentRadius;
            float now = GetGameTimer();
            float elapsed = now - zoneShrinkStart;
            float duration = zoneShrinkEnd - zoneShrinkStart;
            if( duration <= 0 ) return zoneTargetRadius;
            float t = Math.Min( 1f, Math.Max( 0f, elapsed / duration ) );
            return zoneCurrentRadius + ( zoneTargetRadius - zoneCurrentRadius ) * t;
        }

        void DrawZoneCircle() {
            float radius = InterpolateZoneRadius();
            float z = Map != null ? Map.Position.Z + 0.5f : 0.5f;

            // Update blip radius
            if( zoneBlip != -1 ) {
                RemoveZoneBlip();
                zoneBlip = AddBlipForRadius( zoneCenterX, zoneCenterY, 0f, radius );
                SetBlipColour( zoneBlip, 1 );
                SetBlipAlpha( zoneBlip, 128 );
            }

            // Draw circle with line segments on the ground
            float step = (float)( 2 * Math.PI / CIRCLE_SEGMENTS );
            for( int i = 0; i < CIRCLE_SEGMENTS; i++ ) {
                float a1 = step * i;
                float a2 = step * ( i + 1 );
                float x1 = zoneCenterX + radius * (float)Math.Cos( a1 );
                float y1 = zoneCenterY + radius * (float)Math.Sin( a1 );
                float x2 = zoneCenterX + radius * (float)Math.Cos( a2 );
                float y2 = zoneCenterY + radius * (float)Math.Sin( a2 );

                DrawLine( x1, y1, z, x2, y2, z, 50, 130, 255, 200 );
                DrawLine( x1, y1, z + 50f, x2, y2, z + 50f, 50, 130, 255, 100 );
            }

            // Draw vertical pillars at cardinal points for visibility
            for( int i = 0; i < 8; i++ ) {
                float a = step * i * ( CIRCLE_SEGMENTS / 8 );
                float px = zoneCenterX + radius * (float)Math.Cos( a );
                float py = zoneCenterY + radius * (float)Math.Sin( a );
                DrawLine( px, py, z, px, py, z + 100f, 50, 130, 255, 80 );
            }
        }

        bool IsInsideZone( Vector3 pos ) {
            float radius = InterpolateZoneRadius();
            float dx = pos.X - zoneCenterX;
            float dy = pos.Y - zoneCenterY;
            return ( dx * dx + dy * dy ) <= ( radius * radius );
        }

        // ===================== ELIMINATION =====================

        void OnPlayerEliminated( int alive, string victimName, string killerName ) {
            aliveCount = alive;
            brHUD.AliveCount = alive;

            if( !string.IsNullOrEmpty( victimName ) ) {
                if( !string.IsNullOrEmpty( killerName ) && killerName != "The Zone" ) {
                    HubNUI.ShowRoundEnd( killerName, "#e6a020", victimName + " eliminated" );
                }
            }
        }

        void OnWinner( string winnerName ) {
            HubNUI.ShowRoundEnd( winnerName, "#ffd700", "Winner Winner Chicken Dinner!" );
        }

        // ===================== UPDATE LOOP =====================

        public override void Update() {
            // Don't call base.Update() — we handle zone logic ourselves (no rectangle boundary)
            if( HUD != null ) {
                HUD.Draw();
            }

            // Weapon updates
            foreach( var wep in Map.Weapons.ToList() ) {
                wep.Update();
            }

            // Win barrier checks
            foreach( var barrier in WinBarriers ) {
                if( IsInsideBarrier( Game.PlayerPed.Position, barrier ) ) {
                    OnWinBarrierReached( barrier.Position );
                    break;
                }
            }

            Events();
            Controls();

            // Plane: detect when player exits the vehicle naturally (F key)
            if( inPlane && !hasJumped ) {
                brHUD.DrawText2D( 0.5f, 0.85f, "Press ~INPUT_VEH_EXIT~ to JUMP", 0.5f, 255, 255, 255, 255, true );

                // Detect player left the vehicle
                if( !IsPedInVehicle( PlayerPedId(), planeVehicle, false ) ) {
                    OnPlayerLeftPlane();
                }
            }

            // Zone rendering
            if( zoneCurrentRadius > 0 ) {
                DrawZoneCircle();

                // Zone damage warning (client-side visual only, server handles actual damage)
                Vector3 playerPos = Game.PlayerPed.Position;
                if( !IsInsideZone( playerPos ) && Team != SPECTATOR ) {
                    if( zoneDeathTimer == 0 )
                        zoneDeathTimer = GetGameTimer();

                    float secondsLeft = zoneDeathTimer + ZONE_GRACE - GetGameTimer();
                    brHUD.DrawZoneWarning( secondsLeft, ZONE_GRACE );
                } else {
                    zoneDeathTimer = 0;
                }
            }
        }
    }
}
