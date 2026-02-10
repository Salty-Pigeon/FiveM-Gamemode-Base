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

        // Container search
        static readonly string[] ContainerModelNames = new string[] {
            "prop_bin_01a", "prop_bin_02a", "prop_bin_05a",
            "prop_bin_07a", "prop_bin_07b", "prop_bin_07c", "prop_bin_07d",
            "prop_bin_08a", "prop_bin_08open",
            "prop_bin_10a", "prop_bin_10b",
            "prop_bin_11a", "prop_bin_11b",
            "prop_bin_12a", "prop_bin_14a", "prop_bin_14b",
            "prop_dumpster_01a", "prop_dumpster_02a", "prop_dumpster_02b",
            "prop_cs_bin_02",
            "prop_skip_01a", "prop_skip_05a", "prop_skip_06a", "prop_skip_08a",
        };
        uint[] containerHashes;
        bool isSearching = false;
        float searchStartTime = 0;
        const float SEARCH_DURATION = 3000f;
        const float SEARCH_RANGE = 2.5f;
        const float VEHICLE_SEARCH_RANGE = 2.5f;
        Vector3 searchContainerPos;
        int searchVehicle = 0;
        HashSet<string> searchedContainers = new HashSet<string>();
        bool containerDebug = false;

        // Inventory
        BRInventory inventory = new BRInventory();

        // Static state for BRHUD to read inventory
        public static uint[] InventorySlots = new uint[3];
        public static int InventoryActiveSlot = 0;
        public static int[] SlotAmmoClip = new int[3];
        public static int[] SlotAmmoReserve = new int[3];
        public static float InventoryTotalWeight = 0f;

        // Static consumable state for BRHUD
        public static int BandageCount = 0;
        public static int AdrenalineCount = 0;
        public static bool AdrenalineActive = false;

        // NUI inventory state
        bool inventoryOpen = false;
        float adrenalineEndTime = 0;
        static bool nuiCallbacksRegistered = false;

        // Static state for BRHUD to read (TTT body pattern)
        public static bool NearContainerFound = false;
        public static Vector3 NearContainerPos;
        public static bool NearContainerIsVehicle = false;
        public static bool IsSearchingContainer = false;
        public static float SearchProgress = 0f;
        public static bool ContainerDebugActive = false;
        public static string ContainerDebugText = "";

        public Main() : base( "BR" ) {
            brHUD = new BRHUD();
            HUD = brHUD;

            var gmInfo = GamemodeRegistry.Register( "br", "Battle Royale",
                "Last player standing wins. Jump from a plane, loot weapons, survive the shrinking zone.", "#e6a020" );
            gmInfo.MinPlayers = 2;
            gmInfo.MaxPlayers = 32;
            gmInfo.Tags = new string[] { "Survival", "FFA" };
            gmInfo.Teams = new string[] { };
            gmInfo.Features = new string[] { "Parachute Drop", "Shrinking Zone", "Weapon Loot", "Container Search" };
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
                    "Pick up weapons on the ground to upgrade your loadout.",
                    "Search bins and car boots for hidden weapons."
                }
            };

            // Register BR control defaults
            GTA_GameRooClient.ControlConfig.RegisterDefaults( "br",
                new Dictionary<string, int>() {
                    { "PrimaryWeapon", 157 },
                    { "SecondaryWeapon", 158 },
                    { "MeleeWeapon", 159 },
                    { "QuickHealth", 160 },
                    { "QuickAdrenaline", 161 },
                    { "Inventory", 37 },
                },
                new Dictionary<string, string>() {
                    { "PrimaryWeapon", "Primary Weapon (1)" },
                    { "SecondaryWeapon", "Secondary Weapon (2)" },
                    { "MeleeWeapon", "Melee Weapon (3)" },
                    { "QuickHealth", "Quick Bandage (4)" },
                    { "QuickAdrenaline", "Quick Adrenaline (5)" },
                    { "Inventory", "Open Inventory (Tab)" },
                }
            );

            // Register NUI callbacks (once only)
            if( !nuiCallbacksRegistered ) {
                nuiCallbacksRegistered = true;
                RegisterNuiCallbackType( "brCloseInventory" );
                EventHandlers["__cfx_nui:brCloseInventory"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiCloseInventory );
                RegisterNuiCallbackType( "brDropWeapon" );
                EventHandlers["__cfx_nui:brDropWeapon"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiDropWeapon );
                RegisterNuiCallbackType( "brUseConsumable" );
                EventHandlers["__cfx_nui:brUseConsumable"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiUseConsumable );
            }

            // BR-specific events
            EventHandlers["br:spawnPlane"] += new Action<float, float, float, float, float, float, float>( OnSpawnPlane );
            EventHandlers["br:forceJump"] += new Action( OnForceJump );
            EventHandlers["br:giveStartWeapons"] += new Action( OnGiveStartWeapons );
            EventHandlers["br:zoneUpdate"] += new Action<float, float, float, float, float, float, int>( OnZoneUpdate );
            EventHandlers["br:zoneDamage"] += new Action<int>( OnZoneDamage );
            EventHandlers["br:playerEliminated"] += new Action<int, string, string>( OnPlayerEliminated );
            EventHandlers["br:winner"] += new Action<string>( OnWinner );
            EventHandlers["br:containerSearched"] += new Action<float, float, float>( OnContainerSearched );
            EventHandlers["br:containerEmpty"] += new Action( OnContainerEmpty );
            EventHandlers["br:giveConsumable"] += new Action<string>( OnGiveConsumable );
            EventHandlers["br:giveWeapon"] += new Action<string>( OnGiveWeapon );
            EventHandlers["br:weaponDropped"] += new Action<string, float, float, float, int>( OnWeaponDropped );

            containerHashes = new uint[ContainerModelNames.Length];
            for( int i = 0; i < ContainerModelNames.Length; i++ ) {
                containerHashes[i] = (uint)GetHashKey( ContainerModelNames[i] );
            }

            // Debug actions
            DebugRegistry.Register( "br", "br_spawn_bin", "Spawn Bin", "Containers", async () => {
                await SpawnContainerAtPlayer( "prop_cs_bin_02" );
            } );
            DebugRegistry.Register( "br", "br_spawn_dumpster", "Spawn Dumpster", "Containers", async () => {
                await SpawnContainerAtPlayer( "prop_dumpster_01a" );
            } );
            DebugRegistry.Register( "br", "br_spawn_skip", "Spawn Skip", "Containers", async () => {
                await SpawnContainerAtPlayer( "prop_skip_01a" );
            } );
            DebugRegistry.Register( "br", "br_container_debug", "Toggle Debug Overlay", "Containers", () => {
                containerDebug = !containerDebug;
                WriteChat( "Debug", "Container debug: " + ( containerDebug ? "ON" : "OFF" ), 30, 200, 30 );
            } );
            // Weapon debug actions
            DebugRegistry.Register( "br", "br_spawn_weapon_local", "Spawn Weapon (Local)", "Weapons", async () => {
                Vector3 pos = Game.PlayerPed.Position + Game.PlayerPed.ForwardVector * 3f;
                uint hash = 453432689; // WEAPON_PISTOL
                WriteChat( "Debug", "Attempting LOCAL weapon spawn at " + pos.X.ToString( "F1" ) + "," + pos.Y.ToString( "F1" ) + "," + pos.Z.ToString( "F1" ), 200, 200, 30 );

                if( !Globals.Weapons.ContainsKey( hash ) ) {
                    WriteChat( "Debug", "FAIL: hash " + hash + " not in Globals.Weapons", 200, 30, 30 );
                    return;
                }
                string model = Globals.Weapons[hash]["ModelHashKey"];
                WriteChat( "Debug", "Model: " + model, 200, 200, 30 );

                uint modelHash = (uint)GetHashKey( model );
                RequestModel( modelHash );
                int timeout = 0;
                while( !HasModelLoaded( modelHash ) && timeout < 50 ) {
                    await Delay( 50 );
                    timeout++;
                }
                WriteChat( "Debug", "Model loaded: " + HasModelLoaded( modelHash ) + " (waited " + timeout + " ticks)", 200, 200, 30 );

                SaltyWeapon wep = new SaltyWeapon( SpawnType.WEAPON, hash, pos );
                if( Map != null ) {
                    Map.Weapons.Add( wep );
                    WriteChat( "Debug", "Weapon added to Map.Weapons (count: " + Map.Weapons.Count + ")", 30, 200, 30 );
                } else {
                    WriteChat( "Debug", "FAIL: Map is null!", 200, 30, 30 );
                }
            } );

            DebugRegistry.Register( "br", "br_spawn_weapon_server", "Spawn Weapon (Server)", "Weapons", () => {
                Vector3 pos = Game.PlayerPed.Position + Game.PlayerPed.ForwardVector * 3f;
                WriteChat( "Debug", "Requesting SERVER weapon spawn at " + pos.X.ToString( "F1" ) + "," + pos.Y.ToString( "F1" ) + "," + pos.Z.ToString( "F1" ), 200, 200, 30 );
                TriggerServerEvent( "br:debugSpawnWeapon", pos.X, pos.Y, pos.Z );
            } );

            DebugRegistry.Register( "br", "br_weapon_count", "Show Weapon Count", "Weapons", () => {
                int count = Map != null ? Map.Weapons.Count : -1;
                WriteChat( "Debug", "Map.Weapons count: " + count, 200, 200, 30 );
                if( Map != null && count > 0 ) {
                    var first = Map.Weapons[0];
                    WriteChat( "Debug", "First weapon: hash=" + first.Hash + " pos=" + first.Position + " equipped=" + first.Equipped, 200, 200, 30 );
                }
            } );

            DebugRegistry.Register( "br", "br_scan_containers", "Scan Nearby (50u)", "Containers", () => {
                Vector3 p = Game.PlayerPed.Position;
                WriteChat( "Debug", "Player pos: " + p.X.ToString( "F1" ) + ", " + p.Y.ToString( "F1" ) + ", " + p.Z.ToString( "F1" ), 200, 200, 30 );
                int found = 0;
                for( int i = 0; i < containerHashes.Length; i++ ) {
                    int obj = GetClosestObjectOfType( p.X, p.Y, p.Z, 50f, containerHashes[i], false, false, false );
                    if( obj != 0 ) {
                        Vector3 op = GetEntityCoords( obj, true );
                        float d = GetDistanceBetweenCoords( p.X, p.Y, p.Z, op.X, op.Y, op.Z, true );
                        WriteChat( "Debug", ContainerModelNames[i] + " found at " + d.ToString( "F1" ) + "u (handle " + obj + ")", 30, 200, 30 );
                        found++;
                    }
                }
                if( found == 0 ) {
                    WriteChat( "Debug", "No container objects found within 50 units for any of " + containerHashes.Length + " models.", 200, 30, 30 );
                } else {
                    WriteChat( "Debug", found + " container type(s) detected.", 30, 200, 30 );
                }
            } );
        }

        public override void Start( float gameTime ) {
            base.Start( gameTime );
            brHUD.AliveCount = 0;
            hasJumped = false;
            inPlane = false;
            zoneDeathTimer = 0;
            isSearching = false;
            searchVehicle = 0;
            searchedContainers.Clear();
            inventory.Clear();
            inventoryOpen = false;
            adrenalineEndTime = 0;
            SetPedMoveRateOverride( PlayerPedId(), 1.0f );
        }

        public override void End() {
            CleanupPlane();
            RemoveZoneBlip();
            isSearching = false;
            CloseInventory();
            SetPedMoveRateOverride( PlayerPedId(), 1.0f );
            base.End();
        }

        // ===================== PLANE SYSTEM =====================

        async void OnSpawnPlane( float sx, float sy, float sz, float ex, float ey, float ez, float speed ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( this != game ) { game.OnSpawnPlane( sx, sy, sz, ex, ey, ez, speed ); return; }

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

            // Apply selected character model before boarding
            await ApplyShopModel();

            // Tell server the model change is done
            TriggerServerEvent( "salty:spawnReady" );

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
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( this != game ) { game.OnForceJump(); return; }

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
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( this != game ) { game.OnGiveStartWeapons(); return; }

            int ped = PlayerPedId();
            GiveWeaponToPed( ped, 0x99B507EA, 0, false, false );  // WEAPON_KNIFE
            inventory.Add( 0x99B507EA ); // Knife → melee slot 2
            inventory.SelectSlot( 2 );
            SetCurrentPedWeapon( ped, 0x99B507EA, true );

            // Starting consumables
            inventory.AddBandage();
            inventory.AddBandage();
            inventory.AddAdrenaline();
        }

        public override bool CanPickupWeapon( uint hash ) {
            // If already have this exact weapon, allow (will add ammo)
            for( int i = 0; i < 3; i++ ) {
                if( inventory.Slots[i] == hash ) return true;
            }
            if( inventory.CanAdd( hash ) ) return true;
            HUD.ShowPopup( "Inventory full - drop a weapon first", 200, 160, 30 );
            return false;
        }

        public override void OnWeaponPickup( uint hash ) {
            // If already in inventory, let base handle ammo display
            for( int i = 0; i < 3; i++ ) {
                if( inventory.Slots[i] == hash ) {
                    base.OnWeaponPickup( hash );
                    return;
                }
            }
            int slot = inventory.Add( hash );
            if( slot >= 0 ) {
                inventory.SelectSlot( slot );
                SetCurrentPedWeapon( PlayerPedId(), hash, true );
                PlayerWeapons.Add( hash );
            }
        }

        public override void DropWeapon() {
            uint hash = inventory.GetActive();
            if( hash == 0 ) return;

            int slot = inventory.ActiveSlot;
            if( slot == 2 ) {
                HUD.ShowPopup( "Can't drop melee weapon", 200, 160, 30 );
                return;
            }

            int ammo = GetAmmoInPedWeapon( PlayerPedId(), hash );
            Vector3 dropPos = LocalPlayer.Character.Position + LocalPlayer.Character.ForwardVector * 1.5f;
            inventory.Remove( slot );
            PlayerWeapons.Remove( hash );
            RemoveWeaponFromPed( PlayerPedId(), hash );

            // Broadcast drop to all players via server
            TriggerServerEvent( "br:dropWeapon", hash.ToString(), dropPos.X, dropPos.Y, dropPos.Z, ammo );

            // Switch to next occupied slot
            if( inventory.GetActive() != 0 ) {
                SetCurrentPedWeapon( PlayerPedId(), inventory.GetActive(), true );
            } else {
                inventory.CycleNext();
                if( inventory.GetActive() != 0 ) {
                    SetCurrentPedWeapon( PlayerPedId(), inventory.GetActive(), true );
                } else {
                    SetCurrentPedWeapon( PlayerPedId(), 0xA2719263, true );
                }
            }
        }

        void DropWeaponFromSlot( int slot ) {
            if( slot == 2 ) {
                HUD.ShowPopup( "Can't drop melee weapon", 200, 160, 30 );
                return;
            }
            if( slot < 0 || slot > 1 ) return;
            uint hash = inventory.Slots[slot];
            if( hash == 0 ) return;

            int ammo = GetAmmoInPedWeapon( PlayerPedId(), hash );
            Vector3 dropPos = LocalPlayer.Character.Position + LocalPlayer.Character.ForwardVector * 1.5f;
            inventory.Remove( slot );
            PlayerWeapons.Remove( hash );
            RemoveWeaponFromPed( PlayerPedId(), hash );

            // Broadcast drop to all players via server
            TriggerServerEvent( "br:dropWeapon", hash.ToString(), dropPos.X, dropPos.Y, dropPos.Z, ammo );

            // Switch to next occupied slot
            if( inventory.GetActive() != 0 ) {
                SetCurrentPedWeapon( PlayerPedId(), inventory.GetActive(), true );
            } else {
                inventory.CycleNext();
                if( inventory.GetActive() != 0 ) {
                    SetCurrentPedWeapon( PlayerPedId(), inventory.GetActive(), true );
                } else {
                    SetCurrentPedWeapon( PlayerPedId(), 0xA2719263, true );
                }
            }

            if( inventoryOpen ) SendInventoryToNUI();
        }

        void UseBandage() {
            if( !inventory.UseBandage() ) {
                HUD.ShowPopup( "No bandages", 200, 160, 30 );
                return;
            }
            int ped = PlayerPedId();
            int health = GetEntityHealth( ped );
            int newHealth = Math.Min( 200, health + 50 );
            SetEntityHealth( ped, newHealth );
            HUD.ShowPopup( "Bandage used (+50 HP)", 30, 200, 80 );
            if( inventoryOpen ) SendInventoryToNUI();
        }

        void UseAdrenaline() {
            if( !inventory.UseAdrenaline() ) {
                HUD.ShowPopup( "No adrenaline", 200, 160, 30 );
                return;
            }
            adrenalineEndTime = GetGameTimer() + 10000;
            HUD.ShowPopup( "Adrenaline active (10s)", 230, 160, 30 );
            if( inventoryOpen ) SendInventoryToNUI();
        }

        void OnGiveConsumable( string type ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( this != game ) { game.OnGiveConsumable( type ); return; }

            if( type == "bandage" ) {
                if( !inventory.AddBandage() ) {
                    HUD.ShowPopup( "Bandages full", 200, 160, 30 );
                } else {
                    HUD.ShowPopup( "Found bandage", 30, 200, 80 );
                }
            } else if( type == "adrenaline" ) {
                if( !inventory.AddAdrenaline() ) {
                    HUD.ShowPopup( "Adrenaline full", 200, 160, 30 );
                } else {
                    HUD.ShowPopup( "Found adrenaline", 230, 160, 30 );
                }
            }
        }

        void OnGiveWeapon( string hashStr ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( this != game ) { game.OnGiveWeapon( hashStr ); return; }

            uint hash;
            if( !uint.TryParse( hashStr, out hash ) ) return;

            int ped = PlayerPedId();

            // Already have this weapon — just add ammo
            for( int i = 0; i < 3; i++ ) {
                if( inventory.Slots[i] == hash ) {
                    GiveWeaponToPed( ped, hash, 30, false, false );
                    string ammoName = inventory.GetWeaponName( i );
                    HUD.ShowPopup( "Found ammo for " + ammoName, 30, 200, 80 );
                    return;
                }
            }

            if( inventory.CanAdd( hash ) ) {
                int slot = inventory.Add( hash );
                if( slot >= 0 ) {
                    GiveWeaponToPed( ped, hash, 30, false, false );
                    inventory.SelectSlot( slot );
                    SetCurrentPedWeapon( ped, hash, true );
                    PlayerWeapons.Add( hash );
                    string name = inventory.GetWeaponName( slot );
                    HUD.ShowPopup( "Found " + name, 30, 200, 80 );
                }
            } else {
                // Inventory full — drop on ground as fallback, broadcast to all
                Vector3 dropPos = Game.PlayerPed.Position + Game.PlayerPed.ForwardVector * 1.5f;
                TriggerServerEvent( "br:dropWeapon", hash.ToString(), dropPos.X, dropPos.Y, dropPos.Z, 30 );
                HUD.ShowPopup( "Inventory full - weapon dropped", 200, 160, 30 );
            }
        }

        async void OnWeaponDropped( string hashStr, float x, float y, float z, int ammo ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( this != game ) { game.OnWeaponDropped( hashStr, x, y, z, ammo ); return; }

            uint hash;
            if( !uint.TryParse( hashStr, out hash ) ) return;

            // Pre-load weapon model
            if( GTA_GameRooShared.Globals.Weapons.ContainsKey( hash ) ) {
                string model = GTA_GameRooShared.Globals.Weapons[hash]["ModelHashKey"];
                if( !string.IsNullOrEmpty( model ) ) {
                    uint modelHash = (uint)GetHashKey( model );
                    RequestModel( modelHash );
                    int timeout = 0;
                    while( !HasModelLoaded( modelHash ) && timeout < 50 ) {
                        await Delay( 50 );
                        timeout++;
                    }
                }
            }

            SaltyWeapon weapon = new SaltyWeapon( SpawnType.WEAPON, hash, new Vector3( x, y, z ) );
            weapon.AmmoCount = ammo;
            Map.Weapons.Add( weapon );
        }

        void SendInventoryToNUI() {
            int ped = PlayerPedId();
            string slots = "[";
            for( int i = 0; i < 3; i++ ) {
                if( i > 0 ) slots += ",";
                uint hash = inventory.Slots[i];
                string name = inventory.GetWeaponName( i );
                int clip = 0, reserve = 0;
                if( hash != 0 && inventory.IsGunSlot( i ) ) {
                    GetAmmoInClip( ped, hash, ref clip );
                    reserve = GetAmmoInPedWeapon( ped, hash ) - clip;
                }
                slots += "{\"hash\":" + hash + ",\"name\":\"" + name + "\",\"clip\":" + clip + ",\"reserve\":" + reserve + "}";
            }
            slots += "]";
            string json = "{\"type\":\"brOpenInventory\",\"activeSlot\":" + inventory.ActiveSlot
                + ",\"slots\":" + slots
                + ",\"bandages\":" + inventory.BandageCount
                + ",\"adrenaline\":" + inventory.AdrenalineCount + "}";
            SendNuiMessage( json );
        }

        void OpenInventory() {
            if( inventoryOpen ) return;
            inventoryOpen = true;
            SendInventoryToNUI();
            SetNuiFocus( true, true );
        }

        void CloseInventory() {
            if( !inventoryOpen ) return;
            inventoryOpen = false;
            SendNuiMessage( "{\"type\":\"brCloseInventory\"}" );
            SetNuiFocus( false, false );
        }

        // NUI Callbacks
        static void OnNuiCloseInventory( IDictionary<string, object> data, CallbackDelegate cb ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game != null ) game.CloseInventory();
            cb( "{\"status\":\"ok\"}" );
        }

        static void OnNuiDropWeapon( IDictionary<string, object> data, CallbackDelegate cb ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game != null && data.ContainsKey( "slot" ) ) {
                int slot = Convert.ToInt32( data["slot"] );
                game.DropWeaponFromSlot( slot );
            }
            cb( "{\"status\":\"ok\"}" );
        }

        static void OnNuiUseConsumable( IDictionary<string, object> data, CallbackDelegate cb ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game != null && data.ContainsKey( "type" ) ) {
                string type = data["type"].ToString();
                if( type == "bandage" ) game.UseBandage();
                else if( type == "adrenaline" ) game.UseAdrenaline();
            }
            cb( "{\"status\":\"ok\"}" );
        }

        bool JustReleased( int c ) {
            return IsControlJustReleased( 0, c ) || IsDisabledControlJustReleased( 0, c );
        }

        public override void Controls() {
            // Disable weapon wheel & default cycling every frame
            DisableControlAction( 0, 12, true );  // WeaponWheelUpDown
            DisableControlAction( 0, 13, true );  // WeaponWheelLeftRight
            DisableControlAction( 0, 14, true );  // WeaponWheelNext
            DisableControlAction( 0, 15, true );  // WeaponWheelPrev
            DisableControlAction( 0, 16, true );  // SelectNextWeapon
            DisableControlAction( 0, 17, true );  // SelectPrevWeapon
            DisableControlAction( 0, 37, true );  // SelectWeapon (Tab)
            for( int i = 157; i <= 165; i++ ) DisableControlAction( 0, i, true );

            // Read configurable keybinds
            int invKey = GTA_GameRooClient.ControlConfig.GetControl( "br", "Inventory" );
            int primaryKey = GTA_GameRooClient.ControlConfig.GetControl( "br", "PrimaryWeapon" );
            int secondaryKey = GTA_GameRooClient.ControlConfig.GetControl( "br", "SecondaryWeapon" );
            int meleeKey = GTA_GameRooClient.ControlConfig.GetControl( "br", "MeleeWeapon" );
            int healthKey = GTA_GameRooClient.ControlConfig.GetControl( "br", "QuickHealth" );
            int adrenalineKey = GTA_GameRooClient.ControlConfig.GetControl( "br", "QuickAdrenaline" );

            // Tab → toggle inventory NUI
            if( JustReleased( invKey ) ) {
                if( inventoryOpen ) CloseInventory();
                else OpenInventory();
            }

            // If inventory open, skip game controls (NUI handles input)
            if( inventoryOpen ) return;

            // Weapon slot selection
            if( JustReleased( primaryKey ) ) {
                inventory.SelectSlot( 0 );
                uint active = inventory.GetActive();
                if( active != 0 ) SetCurrentPedWeapon( PlayerPedId(), active, true );
                else SetCurrentPedWeapon( PlayerPedId(), 0xA2719263, true );
            }
            if( JustReleased( secondaryKey ) ) {
                inventory.SelectSlot( 1 );
                uint active = inventory.GetActive();
                if( active != 0 ) SetCurrentPedWeapon( PlayerPedId(), active, true );
                else SetCurrentPedWeapon( PlayerPedId(), 0xA2719263, true );
            }
            if( JustReleased( meleeKey ) ) {
                inventory.SelectSlot( 2 );
                uint active = inventory.GetActive();
                if( active != 0 ) SetCurrentPedWeapon( PlayerPedId(), active, true );
                else SetCurrentPedWeapon( PlayerPedId(), 0xA2719263, true );
            }

            // Quick use consumables
            if( JustReleased( healthKey ) ) {
                UseBandage();
            }
            if( JustReleased( adrenalineKey ) ) {
                UseAdrenaline();
            }

            // Scroll wheel: cycle through occupied slots
            if( IsDisabledControlJustReleased( 0, 16 ) ) {
                inventory.CycleNext();
                uint active = inventory.GetActive();
                if( active != 0 ) SetCurrentPedWeapon( PlayerPedId(), active, true );
            }
            if( IsDisabledControlJustReleased( 0, 17 ) ) {
                inventory.CyclePrev();
                uint active = inventory.GetActive();
                if( active != 0 ) SetCurrentPedWeapon( PlayerPedId(), active, true );
            }

            // Adrenaline expiry check
            if( adrenalineEndTime > 0 && GetGameTimer() >= adrenalineEndTime ) {
                adrenalineEndTime = 0;
            }

            // Movement speed: weight + adrenaline bonus
            float moveRate = inventory.GetMoveRate();
            if( adrenalineEndTime > 0 ) {
                moveRate = Math.Min( 1.0f, moveRate + 0.15f );
            }
            SetPedMoveRateOverride( PlayerPedId(), moveRate );
        }

        public override void Events() {
            // Update ammo info for HUD (don't run base Events - it tracks weapons by group differently)
            int ped = PlayerPedId();
            for( int i = 0; i < 3; i++ ) {
                InventorySlots[i] = inventory.Slots[i];
                if( inventory.Slots[i] != 0 && inventory.IsGunSlot( i ) ) {
                    int clip = 0;
                    GetAmmoInClip( ped, inventory.Slots[i], ref clip );
                    SlotAmmoClip[i] = clip;
                    SlotAmmoReserve[i] = GetAmmoInPedWeapon( ped, inventory.Slots[i] ) - clip;
                } else {
                    SlotAmmoClip[i] = 0;
                    SlotAmmoReserve[i] = 0;
                }
            }
            InventoryActiveSlot = inventory.ActiveSlot;
            InventoryTotalWeight = inventory.GetTotalWeight();
            BandageCount = inventory.BandageCount;
            AdrenalineCount = inventory.AdrenalineCount;
            AdrenalineActive = adrenalineEndTime > 0 && GetGameTimer() < adrenalineEndTime;
        }

        // ===================== ZONE =====================

        void OnZoneUpdate( float cx, float cy, float currentR, float targetR, float shrinkStart, float shrinkEnd, int phase ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            game.zoneCenterX = cx;
            game.zoneCenterY = cy;
            game.zoneCurrentRadius = currentR;
            game.zoneTargetRadius = targetR;
            game.zoneShrinkStart = shrinkStart;
            game.zoneShrinkEnd = shrinkEnd;
            game.zonePhase = phase;

            game.brHUD.ZonePhase = phase;
            game.UpdateZoneBlip();
        }

        void OnZoneDamage( int damage ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            game.HandleZoneDamage( damage );
        }

        void HandleZoneDamage( int damage ) {
            int ped = PlayerPedId();
            int health = GetEntityHealth( ped );
            int newHealth = health - damage;
            if( newHealth < 0 ) newHealth = 0;
            SetEntityHealth( ped, newHealth );

            // Red screen flash
            StartScreenEffect( "DeathFailOut", 300, false );
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
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            game.aliveCount = alive;
            game.brHUD.AliveCount = alive;

            if( !string.IsNullOrEmpty( victimName ) ) {
                if( !string.IsNullOrEmpty( killerName ) && killerName != "The Zone" ) {
                    HubNUI.ShowRoundEnd( killerName, "#e6a020", victimName + " eliminated" );
                }
            }
        }

        void OnWinner( string winnerName ) {
            HubNUI.ShowRoundEnd( winnerName, "#ffd700", "Winner Winner Chicken Dinner!" );
        }

        // ===================== WEAPON INDICATORS =====================

        void DrawWeaponIndicators() {
            Vector3 camPos = GetGameplayCamCoords();

            foreach( var wep in Map.Weapons ) {
                if( wep.Equipped ) continue;

                float dist = GetDistanceBetweenCoords(
                    camPos.X, camPos.Y, camPos.Z,
                    wep.Position.X, wep.Position.Y, wep.Position.Z, true );

                // Floating marker visible within 40 units
                if( dist < 40f ) {
                    float markerZ = wep.Position.Z + 0.8f;
                    // Small downward chevron, bobbing, orange accent
                    DrawMarker( 2, wep.Position.X, wep.Position.Y, markerZ,
                        0f, 0f, 0f, 180f, 0f, 0f, 0.12f, 0.12f, 0.12f,
                        230, 160, 30, 140, true, false, 2, false, null, null, false );
                }

                // Weapon name when within 10 units and on screen
                if( dist < 10f ) {
                    string weaponName = "";
                    if( GTA_GameRooShared.Globals.Weapons.ContainsKey( wep.Hash ) ) {
                        weaponName = GTA_GameRooShared.Globals.Weapons[wep.Hash]["Name"];
                    }
                    if( !string.IsNullOrEmpty( weaponName ) ) {
                        float textZ = wep.Position.Z + 0.55f;
                        brHUD.DrawText3D( new Vector3( wep.Position.X, wep.Position.Y, textZ ),
                            weaponName, 0.30f, 255, 255, 255, 220, 10f );

                        // Pickup hint when very close
                        if( dist < 5f ) {
                            brHUD.DrawText3D( new Vector3( wep.Position.X, wep.Position.Y, textZ - 0.18f ),
                                "[E] Pick up", 0.24f, 230, 160, 30, 200, 6f );
                        }
                    }
                }
            }
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

            DrawWeaponIndicators();

            // Win barrier checks
            foreach( var barrier in WinBarriers ) {
                if( IsInsideBarrier( Game.PlayerPed.Position, barrier ) ) {
                    OnWinBarrierReached( barrier.Position );
                    break;
                }
            }

            UpdateContainerSearch();

            Events();
            Controls();

            // Plane: detect when player exits the vehicle naturally (F key)
            if( inPlane && !hasJumped ) {
                string exitKey = GetControlInstructionalButton( 2, 75, 1 );
                if( exitKey.StartsWith( "t_" ) ) exitKey = exitKey.Substring( 2 );
                brHUD.DrawText2D( 0.5f, 0.85f, "Press " + exitKey + " to JUMP", 0.5f, 255, 255, 255, 255, true );

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

        // ===================== CONTAINER SEARCH =====================

        string GetPosKey( float x, float y, float z ) {
            return ((int)Math.Round( x )) + "," + ((int)Math.Round( y )) + "," + ((int)Math.Round( z ));
        }

        void UpdateContainerSearch() {
            // Reset HUD state each frame
            NearContainerFound = false;
            IsSearchingContainer = false;
            SearchProgress = 0f;
            ContainerDebugActive = containerDebug;
            ContainerDebugText = "";

            if( Team == SPECTATOR ) return;
            if( IsPedInAnyVehicle( PlayerPedId(), false ) ) return;

            Vector3 playerPos = Game.PlayerPed.Position;

            // Active search in progress
            if( isSearching ) {
                string activeKey = GetPosKey( searchContainerPos.X, searchContainerPos.Y, searchContainerPos.Z );

                // Cancel if another player already searched this
                if( searchedContainers.Contains( activeKey ) ) {
                    Debug.WriteLine( "[BR-Search] CANCELLED: already searched key=" + activeKey );
                    isSearching = false;
                    WriteChat( "BR", "Already searched.", 200, 200, 200 );
                    return;
                }

                // Cancel if released E or moved away
                float dist = GetDistanceBetweenCoords( playerPos.X, playerPos.Y, playerPos.Z,
                    searchContainerPos.X, searchContainerPos.Y, searchContainerPos.Z, true );
                if( dist > SEARCH_RANGE + 1.5f || !IsControlPressed( 0, (int)eControl.ControlPickup ) ) {
                    bool tooFar = dist > SEARCH_RANGE + 1.5f;
                    bool releasedE = !IsControlPressed( 0, (int)eControl.ControlPickup );
                    Debug.WriteLine( "[BR-Search] CANCELLED: tooFar=" + tooFar + " (dist=" + dist.ToString("F1") + ") releasedE=" + releasedE );
                    isSearching = false;
                    return;
                }

                float elapsed = GetGameTimer() - searchStartTime;
                float progress = Math.Min( 1f, elapsed / SEARCH_DURATION );

                // Set static state for BRHUD to draw
                IsSearchingContainer = true;
                SearchProgress = progress;
                NearContainerFound = true;
                NearContainerPos = searchContainerPos;

                DisablePlayerFiring( PlayerId(), true );

                if( elapsed >= SEARCH_DURATION ) {
                    isSearching = false;
                    searchedContainers.Add( activeKey );
                    Debug.WriteLine( "[BR-Search] COMPLETE! key=" + activeKey + " pos=" + searchContainerPos.X.ToString("F1") + "," + searchContainerPos.Y.ToString("F1") + "," + searchContainerPos.Z.ToString("F1") );

                    // Open vehicle trunk visually
                    if( searchVehicle != 0 && DoesEntityExist( searchVehicle ) ) {
                        SetVehicleDoorOpen( searchVehicle, 5, false, false );
                    }

                    Debug.WriteLine( "[BR-Search] Sending br:searchContainer to server..." );
                    TriggerServerEvent( "br:searchContainer",
                        searchContainerPos.X, searchContainerPos.Y, searchContainerPos.Z );
                    searchVehicle = 0;
                }
                return;
            }

            // Detection phase - check prop containers
            string debugInfo = "";

            foreach( uint hash in containerHashes ) {
                int obj = GetClosestObjectOfType( playerPos.X, playerPos.Y, playerPos.Z,
                    SEARCH_RANGE, hash, false, false, false );
                if( obj != 0 ) {
                    Vector3 objPos = GetEntityCoords( obj, true );
                    string key = GetPosKey( objPos.X, objPos.Y, objPos.Z );
                    if( !searchedContainers.Contains( key ) ) {
                        // Set static state for BRHUD to draw 3D prompt
                        NearContainerFound = true;
                        NearContainerPos = objPos;
                        NearContainerIsVehicle = false;

                        if( IsControlPressed( 0, (int)eControl.ControlPickup ) ) {
                            isSearching = true;
                            searchStartTime = GetGameTimer();
                            searchContainerPos = objPos;
                            searchVehicle = 0;
                            Debug.WriteLine( "[BR-Search] START searching container at " + objPos.X.ToString("F1") + "," + objPos.Y.ToString("F1") + "," + objPos.Z.ToString("F1") + " key=" + key );
                        }
                        return;
                    }
                }
            }

            // Build debug info for overlay
            if( containerDebug ) {
                int wideFound = 0;
                string wideInfo = "";
                for( int i = 0; i < containerHashes.Length; i++ ) {
                    int obj = GetClosestObjectOfType( playerPos.X, playerPos.Y, playerPos.Z,
                        30f, containerHashes[i], false, false, false );
                    if( obj != 0 ) {
                        Vector3 op = GetEntityCoords( obj, true );
                        float d = GetDistanceBetweenCoords( playerPos.X, playerPos.Y, playerPos.Z,
                            op.X, op.Y, op.Z, true );
                        if( wideFound == 0 ) wideInfo = ContainerModelNames[i] + " @ " + d.ToString( "F1" ) + "u";
                        wideFound++;
                    }
                }
                ContainerDebugText = "Range:" + SEARCH_RANGE + "u Models:" + containerHashes.Length;
                if( wideFound > 0 )
                    ContainerDebugText += "\nNearest(30u): " + wideInfo + " (" + wideFound + " types)";
                else
                    ContainerDebugText += "\nNothing within 30u";
                ContainerDebugText += "\nSearched:" + searchedContainers.Count + " Team:" + Team;
            }

            // Check vehicle boots (parked vehicles only)
            int vehicle = GetClosestVehicle( playerPos.X, playerPos.Y, playerPos.Z, 8f, 0, 0 );
            if( vehicle != 0 && IsVehicleSeatFree( vehicle, -1 ) ) {
                Vector3 vMin = new Vector3(), vMax = new Vector3();
                GetModelDimensions( (uint)GetEntityModel( vehicle ), ref vMin, ref vMax );
                Vector3 trunkPos = GetOffsetFromEntityInWorldCoords( vehicle, 0f, vMin.Y - 0.5f, 0f );

                float trunkDist = GetDistanceBetweenCoords( playerPos.X, playerPos.Y, playerPos.Z,
                    trunkPos.X, trunkPos.Y, trunkPos.Z, true );
                if( trunkDist <= VEHICLE_SEARCH_RANGE ) {
                    string key = GetPosKey( trunkPos.X, trunkPos.Y, trunkPos.Z );
                    if( !searchedContainers.Contains( key ) ) {
                        NearContainerFound = true;
                        NearContainerPos = trunkPos;
                        NearContainerIsVehicle = true;

                        if( IsControlPressed( 0, (int)eControl.ControlPickup ) ) {
                            isSearching = true;
                            searchStartTime = GetGameTimer();
                            searchContainerPos = trunkPos;
                            searchVehicle = vehicle;
                            Debug.WriteLine( "[BR-Search] START searching vehicle boot at " + trunkPos.X.ToString("F1") + "," + trunkPos.Y.ToString("F1") + "," + trunkPos.Z.ToString("F1") );
                        }
                        return;
                    }
                }
            }
        }

        void OnContainerSearched( float x, float y, float z ) {
            var game = ClientGlobals.CurrentGame as Main;
            if( game == null ) return;
            string key = GetPosKey( x, y, z );
            Debug.WriteLine( "[BR-Search] OnContainerSearched broadcast received, key=" + key );
            game.searchedContainers.Add( key );
        }

        void OnContainerEmpty() {
            Debug.WriteLine( "[BR-Search] OnContainerEmpty received - server says nothing found" );
            HUD.ShowPopup( "Nothing found", 150, 150, 150 );
        }

        async Task SpawnContainerAtPlayer( string modelName ) {
            uint modelHash = (uint)GetHashKey( modelName );
            RequestModel( modelHash );
            int timeout = 0;
            while( !HasModelLoaded( modelHash ) && timeout < 50 ) {
                await Delay( 50 );
                timeout++;
            }
            if( !HasModelLoaded( modelHash ) ) {
                WriteChat( "Debug", "Failed to load model: " + modelName, 200, 30, 30 );
                return;
            }
            Vector3 pos = Game.PlayerPed.Position;
            Vector3 forward = Game.PlayerPed.ForwardVector;
            float spawnX = pos.X + forward.X * 3f;
            float spawnY = pos.Y + forward.Y * 3f;
            int obj = CreateObject( (int)modelHash, spawnX, spawnY, pos.Z, true, true, false );
            PlaceObjectOnGroundProperly( obj );
            FreezeEntityPosition( obj, true );
            SetModelAsNoLongerNeeded( modelHash );
            WriteChat( "Debug", "Spawned " + modelName, 30, 200, 30 );
        }
    }
}
