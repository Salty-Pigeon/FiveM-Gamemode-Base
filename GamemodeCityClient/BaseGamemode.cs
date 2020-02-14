using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class BaseGamemode : BaseScript, IDisposable {

        string Gamemode;

        public ClientMap Map;
        public HUD HUD;

        public float GameTimerEnd;

        float deathTimer = 0;
        float gracePeriod = 1000 * 5;

        public List<uint> GameWeapons = new List<uint>();
        IList<uint> PlayerWeapons = new List<uint>();
        uint lastWep = 0;

        public int SPECTATOR = -1;

        public static int Team = 0;

        public Dictionary<int, Dictionary<string, dynamic>> PlayerDetails = new Dictionary<int, Dictionary<string, dynamic>>();


        public virtual void OnDetailUpdate( int ply, string key, dynamic oldValue, dynamic newValue ) {

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

        public void SetPlayerDetail( int ply, string detail, dynamic data ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<string, dynamic>() );
            }
            if( !PlayerDetails[ply].ContainsKey( detail ) ) {
                PlayerDetails[ply].Add( detail, data );
            }
            OnDetailUpdate( ply, detail, PlayerDetails[ply][detail], data );
            PlayerDetails[ply][detail] = data;
        }

        public dynamic GetPlayerDetail( int ply, string detail ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<string, dynamic>() );
            }
            else if( PlayerDetails[ply].ContainsKey( detail ) ) {
                return PlayerDetails[ply][detail];
            }
            return null;
        }


        public bool BuyItem( int cost ) {
            dynamic gameCoins = GetPlayerDetail( LocalPlayer.ServerId, "coins" );
            if( gameCoins == null ) { gameCoins = 0; };
            if( gameCoins >= cost ) {
                TriggerServerEvent( "salty:netUpdatePlayerDetail", "coins", (int)gameCoins - 1 );
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

        public virtual void Start( float gameTime ) {
            SetMaxWantedLevel( 0 );
            LocalPlayer.Character.Health = 100;
            LocalPlayer.Character.MaxHealth = 100;
            ClientGlobals.SetSpectator( false );
            WriteChat( Gamemode.ToUpper(), "Game started.", 255, 0, 0 );
            RemoveAllPedWeapons( PlayerPedId(), true );
            GameTimerEnd = GetGameTimer() + gameTime;
            HUD.Start();
        }

        public virtual void End() {
            WriteChat( Gamemode.ToUpper(), "Game finished!", 255, 0, 0 );
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
            foreach( var wep in Map.Weapons.ToList() ) {
                wep.Update();
            }

            if( Map != null ) {

                Map.DrawBoundarys();

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
                    ClientGlobals.CurrentGame.HUD.BoundText.Colour = System.Drawing.Color.FromArgb( 255, 0, 0 );
                    ClientGlobals.CurrentGame.HUD.BoundText.Caption = "You have " + Math.Round( secondsLeft / 1000 ) + " seconds to return or you will die.";
                    ClientGlobals.CurrentGame.HUD.BoundText.Draw();
                }


            }

            Events();
            Controls();
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
            if( IsControlJustReleased( 0, (int)eControl.ControlEnter) ) {
                DropWeapon();
            }

        }

        public void DropWeapon() {

            if( Game.PlayerPed.Weapons.Current.Hash.ToString() == "Unarmed" )
                return;

            foreach( var wep in PlayerWeapons.ToArray() ) {
                if( (uint)LocalPlayer.Character.Weapons.Current.Hash == wep ) {
                    SaltyWeapon weapon = new SaltyWeapon( SpawnType.WEAPON, wep, LocalPlayer.Character.Position );
                    weapon.AmmoCount = LocalPlayer.Character.Weapons.Current.Ammo;
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

        public bool CanPickupWeapon( uint hash ) {
            foreach( var wep in PlayerWeapons ) {
                if( GetWeapontypeGroup(hash) == GetWeapontypeGroup( wep ) ) {
                    return false;
                }
            }
            return true;
        }

    }
}
