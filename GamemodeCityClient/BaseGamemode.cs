using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class BaseGamemode : BaseScript {

        string Gamemode;

        public ClientMap Map;
        public HUD HUD;

        public float GameTimerEnd;

        public List<uint> GameWeapons = new List<uint>();


        public BaseGamemode( string gamemode ) {
            Gamemode = gamemode.ToLower();
            if( !ClientGlobals.Gamemodes.ContainsKey( Gamemode ) )
                ClientGlobals.Gamemodes.Add( Gamemode, this);

        }

        public virtual void Start( float gameTime ) {
            RemoveAllPedWeapons( PlayerPedId(), true );
            GameTimerEnd = GetGameTimer() + gameTime;
            HUD.Start();
        }

        public virtual void Update() {
            if( HUD != null ) {
                HUD.Draw();
            }
            foreach( var wep in Map.Weapons.ToList() ) {
                wep.Update();
            }

            Events();
            Controls();
        }

        IList<uint> PlayerWeapons = new List<uint>();
        uint lastWep = 0;

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

            Debug.WriteLine( "Dropping" );

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
            PlayerWeapons.Add( hash );

        }

        public virtual void OnWeaponDropped( uint hash ) {
            PlayerWeapons.Remove( hash );
        }

        public virtual void OnWeaponChanged(  ) {
            HUD.latestAmmo = Math.Max( 0, Game.PlayerPed.Weapons.Current.Ammo - LocalPlayer.Character.Weapons.Current.DefaultClipSize );

        }

        public virtual void AddAmmo( uint Hash, int ammo ) {
            int ped = PlayerPedId();
            int pedAmmo = GetAmmoInPedWeapon( ped, Hash );
            SetPedAmmo( ped, Hash, pedAmmo + ammo );
            HUD.latestAmmo = Math.Max( 0, Game.PlayerPed.Weapons.Current.Ammo - LocalPlayer.Character.Weapons.Current.DefaultClipSize );
        }


        public virtual void Cleanup() {

        }

        public virtual void SetTeam( int team ) {
            ClientGlobals.Team = team;
        }

        public bool CanPickupWeapon( uint hash ) {
            foreach( var wep in PlayerWeapons ) {
                if( GetWeapontypeGroup(hash) == GetWeapontypeGroup( wep ) ) {
                    Debug.WriteLine( "Can't pickup" );
                    return false;
                }
            }
            return true;
        }

    }
}
