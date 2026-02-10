using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooShared;

namespace GTA_GameRooClient {

    public class WinBarrierData {
        public Vector3 Position;
        public float SizeX;
        public float SizeY;
        public float Rotation;
    }

    public class BaseGamemode : BaseScript, IDisposable {

        string Gamemode;

        public ClientMap Map;
        public HUD HUD;

        public float GameTimerEnd;

        float deathTimer = 0;
        float gracePeriod = 1000 * 5;

        public List<uint> GameWeapons = new List<uint>();
        protected IList<uint> PlayerWeapons = new List<uint>();
        uint lastWep = 0;

        public const int SPECTATOR = -1;
        public bool CountdownActive = false;

        public List<WinBarrierData> WinBarriers = new List<WinBarrierData>();

        public static int Team = 0;
        public static bool SuppressWeaponPickup = false;

        // Client-side death detection fallback (NPC kills don't fire baseevents)
        bool deathReported = false;

        public Dictionary<int, Dictionary<string, object>> PlayerDetails = new Dictionary<int, Dictionary<string, object>>();


        public virtual void OnDetailUpdate( int ply, string key, object oldValue, object newValue ) {

        }

        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose( bool disposing ) {
            if( disposed )
                return;

            if( disposing ) {
                // Free any other managed objects here.
                //
            }

            HUD.Dispose();
            Map = null;

            // Free any unmanaged objects here.
            //

            disposed = true;
        }

        ~BaseGamemode() {
            Dispose( false );
        }

        public void SetPlayerDetail( int ply, string detail, object data ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<string, object>() );
            }
            if( !PlayerDetails[ply].ContainsKey( detail ) ) {
                PlayerDetails[ply].Add( detail, data );
            }
            OnDetailUpdate( ply, detail, PlayerDetails[ply][detail], data );
            PlayerDetails[ply][detail] = data;
        }

        public object GetPlayerDetail( int ply, string detail ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<string, object>() );
            }
            else if( PlayerDetails[ply].ContainsKey( detail ) ) {
                return PlayerDetails[ply][detail];
            }
            return null;
        }


        public bool BuyItem( int cost ) {
            object gameCoinsObj = GetPlayerDetail( LocalPlayer.ServerId, "coins" );
            int gameCoins = gameCoinsObj != null ? Convert.ToInt32( gameCoinsObj ) : 0;
            if( gameCoins >= cost ) {
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "coins", gameCoins - 1 );
                BaseGamemode.WriteChat( "Store", "Item bought.", 20, 200, 20 );
                return true;
            }
            else {
                BaseGamemode.WriteChat( "Store", "Out of coins.", 200, 20, 20 );
                return false;
            }
        }

        public BaseGamemode( string gamemode ) {
            Globals.GameCoins = 0;
            Game.PlayerPed.IsInvincible = false;
            Game.PlayerPed.Weapons.Current.InfiniteAmmo = false;
            Game.PlayerPed.Weapons.Current.InfiniteAmmoClip = false;
            Gamemode = gamemode.ToLower();
            if( !ClientGlobals.Gamemodes.ContainsKey( Gamemode ) )
                ClientGlobals.Gamemodes.Add( Gamemode, this);
            Game.PlayerPed.Opacity = 255;
        }

        protected async Task RunCountdown() {
            CountdownActive = true;

            // Wait for role reveal to finish
            await Delay( 3200 );

            HubNUI.ShowCountdown( 3 );
            await Delay( 1000 );
            HubNUI.ShowCountdown( 2 );
            await Delay( 1000 );
            HubNUI.ShowCountdown( 1 );
            await Delay( 1000 );
            HubNUI.ShowCountdown( 0 ); // "GO"

            CountdownActive = false;

            // Unfreeze player ped
            FreezeEntityPosition( PlayerPedId(), false );

            // Unfreeze vehicle if player is in one
            if( Game.PlayerPed.IsInVehicle() && Game.PlayerPed.CurrentVehicle != null )
                FreezeEntityPosition( Game.PlayerPed.CurrentVehicle.Handle, false );
        }

        /// <summary>
        /// Applies the player's selected shop model and appearance.
        /// Call during any spawn/game start flow. Returns true if model was applied.
        /// </summary>
        public static async Task<bool> ApplyShopModel() {
            string selectedModel = PlayerProgression.GetSelectedModel();
            if( string.IsNullOrEmpty( selectedModel ) ) return false;

            uint modelHash = (uint)GetHashKey( selectedModel );
            RequestModel( modelHash );
            int timeout = 0;
            while( !HasModelLoaded( modelHash ) && timeout < 50 ) {
                await Delay( 50 );
                timeout++;
            }
            if( !HasModelLoaded( modelHash ) ) return false;

            SetPlayerModel( PlayerId(), modelHash );
            SetModelAsNoLongerNeeded( modelHash );
            PlayerProgression.ApplyFullAppearance( PlayerPedId(), PlayerProgression.AppearanceJson );
            return true;
        }

        /// <summary>
        /// Fully resets the local player ped to a clean state.
        /// Call after model changes or at game start to ensure nothing persists.
        /// </summary>
        public static void ResetPlayerState() {
            int ped = PlayerPedId();

            // Health — use natives directly so model defaults can't interfere.
            // Native scale: 100 = dead, 200 = full for the default player range.
            SetPedMaxHealth( ped, 200 );
            SetEntityHealth( ped, 200 );

            // Armor
            SetPedArmour( ped, 0 );

            // Weapons
            RemoveAllPedWeapons( ped, true );

            // Visual damage / effects
            ClearPedBloodDamage( ped );
            ResetPedVisibleDamage( ped );
            ClearPedLastWeaponDamage( ped );

            // Movement / ragdoll
            SetPedToRagdoll( ped, 0, 0, 0, false, false, false );
            SetPedCanRagdoll( ped, true );
            ClearPedTasksImmediately( ped );
            SetPedMoveRateOverride( ped, 1.0f );

            // Flags
            Game.PlayerPed.IsInvincible = false;
            Game.PlayerPed.IsFireProof = false;
            Game.PlayerPed.IsExplosionProof = false;
            Game.PlayerPed.IsCollisionProof = false;
            Game.PlayerPed.IsMeleeProof = false;
            Game.PlayerPed.Opacity = 255;
            SetPlayerInvincible( PlayerId(), false );
            SetMaxWantedLevel( 0 );
            ClearPlayerWantedLevel( PlayerId() );
        }

        public virtual void Start( float gameTime ) {
            ClientGlobals.SetSpectator( false );

            // Resurrect the player to ensure they're alive and not in death state
            Vector3 pos = Game.PlayerPed.Position;
            NetworkResurrectLocalPlayer( pos.X, pos.Y, pos.Z, Game.PlayerPed.Heading, true, false );

            ResetPlayerState();
            deathReported = false;

            WriteChat( Gamemode.ToUpper(), "Game started.", 255, 0, 0 );
            GameTimerEnd = GetGameTimer() + gameTime;
            HUD.Start();
        }

        public virtual void End() {
            WriteChat( Gamemode.ToUpper(), "Game finished!", 255, 0, 0 );
            if( HUD != null )
                HUD.HideGameTimer();
            Map.ClearObjects();
            ClientGlobals.SetSpectator( true );
            ClientGlobals.CurrentGame = null;
            Dispose();
        }

        public void CantEnterVehichles() {
            SetPlayerMayNotEnterAnyVehicle( PlayerId() );
            DisableControlAction( 0, 75, true );
        }

        public virtual void Update() {
            if( HUD != null ) {
                HUD.Draw();
            }

            // Freeze player and their vehicle during countdown
            if( CountdownActive ) {
                FreezeEntityPosition( PlayerPedId(), true );
                if( Game.PlayerPed.IsInVehicle() && Game.PlayerPed.CurrentVehicle != null )
                    FreezeEntityPosition( Game.PlayerPed.CurrentVehicle.Handle, true );
            }

            foreach( var wep in Map.Weapons.ToList() ) {
                wep.Update();
            }

            if( Map != null ) {

                Map.DrawBoundaries();

                if( Map.IsInZone( LocalPlayer.Character.Position ) ) {
                    deathTimer = 0;
                }
                else {
                    if( deathTimer == 0 )
                        deathTimer = GetGameTimer();

                    float secondsLeft = deathTimer + gracePeriod - GetGameTimer();
                    if( secondsLeft < 0 ) {
                        Game.Player.Character.Kill();
                        deathTimer = 0;
                    }
                    // Polished out-of-bounds warning banner
                    var hud = ClientGlobals.CurrentGame.HUD;
                    float bannerW = 0.22f;
                    float bannerH = 0.038f;
                    float bannerX = 0.5f - bannerW / 2f;
                    float bannerY = 0.06f;

                    // Urgency ramps from 0 (full time) to 1 (dead)
                    float urgency = 1f - Math.Max( 0, secondsLeft ) / gracePeriod;

                    // Background — darkens and reddens as time runs out
                    int bgR = (int)( 15 + urgency * 50 );
                    int bgA = (int)( 190 + urgency * 55 );
                    hud.DrawRectangle( bannerX, bannerY, bannerW, bannerH, bgR, 8, 8, bgA );

                    // Depleting fill bar — shows remaining time visually
                    float fillPct = Math.Max( 0, secondsLeft ) / gracePeriod;
                    hud.DrawRectangle( bannerX, bannerY, fillPct * bannerW, bannerH, 200, 40, 40, 40 );

                    // Left accent
                    hud.DrawRectangle( bannerX, bannerY, 0.004f, bannerH, 200, 40, 40, 255 );

                    // Warning text with countdown
                    int secDisplay = Math.Max( 0, (int)Math.Ceiling( secondsLeft / 1000 ) );
                    string warnText = "RETURN TO PLAY AREA   " + secDisplay + "s";

                    // Pulse when critical (<=2s)
                    int textA = 255;
                    if( secDisplay <= 2 ) {
                        float pulse = (float)( Math.Sin( GetGameTimer() / 150.0 ) * 0.5 + 0.5 );
                        textA = 180 + (int)( 75 * pulse );
                    }
                    hud.DrawText2D( 0.5f, bannerY + 0.008f, warnText, 0.32f, 255, 255, 255, textA, true );
                }


            }

            foreach( var barrier in WinBarriers ) {
                if( IsInsideBarrier( Game.PlayerPed.Position, barrier ) ) {
                    OnWinBarrierReached( barrier.Position );
                    break;
                }
            }

            // Client-side death detection fallback (NPC kills don't fire baseevents)
            if( !deathReported && Team != SPECTATOR && IsEntityDead( PlayerPedId() ) ) {
                deathReported = true;
                TriggerServerEvent( "gameroo:clientDeath" );
            }

            Events();
            Controls();
        }

        public virtual void OnWinBarrierReached( Vector3 pos ) {
        }

        public static bool IsInsideBarrier( Vector3 pos, WinBarrierData barrier ) {
            float halfX = barrier.SizeX / 2f;
            float halfY = barrier.SizeY / 2f;
            if( halfX <= 0 ) halfX = 2.5f;
            if( halfY <= 0 ) halfY = 2.5f;
            float rad = -barrier.Rotation * ((float)Math.PI / 180f);
            float cos = (float)Math.Cos( rad );
            float sin = (float)Math.Sin( rad );
            float dx = pos.X - barrier.Position.X;
            float dy = pos.Y - barrier.Position.Y;
            float localX = dx * cos - dy * sin;
            float localY = dx * sin + dy * cos;
            return localX > -halfX && localX < halfX && localY > -halfY && localY < halfY;
        }

        public static void WriteChat( string prefix, string str, int r, int g, int b ) {
            TriggerEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }

        public void CantExitVehichles() {
            DisableControlAction( 0, 75, true );
        }

        public virtual void Events() {

            if( ClientGlobals.CurrentGame == null )
                return;

            // Weapon pickup
            foreach( var wepHash in ClientGlobals.CurrentGame.GameWeapons ) {
                //uint wepHash = (uint)GetWeaponHashFromPickup( GetHashKey( weps.Value ) );
                if( HasPedGotWeapon( PlayerPedId(), wepHash, false ) && !PlayerWeapons.Contains( wepHash ) ) {
                    OnWeaponPickup( wepHash );
                }

                if( !HasPedGotWeapon( PlayerPedId(), wepHash, false ) && PlayerWeapons.Contains( wepHash ) ) {
                    OnWeaponDropped( wepHash );
                } 

            }

            if( lastWep != (uint)LocalPlayer.Character.Weapons.Current.Hash ) {
                lastWep = (uint)LocalPlayer.Character.Weapons.Current.Hash;
                OnWeaponChanged();
            }

        }

        public virtual void Controls() {
            int dropKey = ControlConfig.GetControl( Gamemode, "DropWeapon" );
            if( dropKey > 0 && IsControlJustReleased( 0, dropKey ) ) {
                DropWeapon();
            } else if( dropKey <= 0 && IsControlJustReleased( 0, (int)eControl.ControlEnter ) ) {
                DropWeapon();
            }
        }

        public virtual void DropWeapon() {

            if( Game.PlayerPed.Weapons.Current.Hash.ToString() == "Unarmed" )
                return;

            foreach( var wep in PlayerWeapons.ToArray() ) {
                if( (uint)LocalPlayer.Character.Weapons.Current.Hash == wep ) {
                    SaltyWeapon weapon = new SaltyWeapon( SpawnType.WEAPON, wep, LocalPlayer.Character.Position );
                    weapon.AmmoCount = LocalPlayer.Character.Weapons.Current.Ammo;
                    weapon.AmmoInClip = LocalPlayer.Character.Weapons.Current.AmmoInClip;
                    Map.Weapons.Add( weapon );
                    PlayerWeapons.Remove( wep );
                    RemoveWeaponFromPed( PlayerPedId(), wep );
                    return;
                }
            }
        }


        public virtual void OnWeaponPickup( uint hash ) {
            if( PlayerWeapons.Contains(hash) ) {   
                HUD.latestAmmo = Math.Max( 0, Game.PlayerPed.Weapons.Current.Ammo - LocalPlayer.Character.Weapons.Current.DefaultClipSize );
            }
            else {
                PlayerWeapons.Add( hash );
            }

        }

        public virtual void OnWeaponDropped( uint hash ) {
            PlayerWeapons.Remove( hash );
        }

        public virtual void PlayerSpawn() {
            deathReported = false;
        }

        public virtual void OnWeaponChanged(  ) {
            HUD.latestAmmo = Math.Max( 0, Game.PlayerPed.Weapons.Current.Ammo - LocalPlayer.Character.Weapons.Current.DefaultClipSize );

        }

        public virtual void AddAmmo( uint Hash, int ammo ) {
            int ped = PlayerPedId();
            int pedAmmo = GetAmmoInPedWeapon( ped, Hash );
            SetPedAmmo( ped, Hash, pedAmmo + ammo );
            if( (uint)Game.PlayerPed.Weapons.Current.Hash == Hash ) {
                HUD.latestAmmo = Math.Max( 0, Game.PlayerPed.Weapons.Current.Ammo - LocalPlayer.Character.Weapons.Current.AmmoInClip );
            }
        }


        public virtual void Cleanup() {

        }

        public virtual void SetTeam( int team ) {
            Team = team;
            if( team == SPECTATOR ) {
                LocalPlayer.IsInvincible = true;
            }
        }

        public virtual bool CanPickupWeapon( uint hash ) {
            foreach( var wep in PlayerWeapons ) {
                if( GetWeapontypeGroup(hash) == GetWeapontypeGroup( wep ) ) {
                    return false;
                }
            }
            return true;
        }

    }
}
