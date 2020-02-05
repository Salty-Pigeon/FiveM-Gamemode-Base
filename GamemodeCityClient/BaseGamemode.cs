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
        IList<uint> PlayerWeapons = new List<uint>();
        uint lastWep = 0;

        public int SPECTATOR = -1;

        public BaseGamemode( string gamemode ) {
            Globals.GameCoins = 0;
            Gamemode = gamemode.ToLower();
            if( !ClientGlobals.Gamemodes.ContainsKey( Gamemode ) )
                ClientGlobals.Gamemodes.Add( Gamemode, this);
            Game.PlayerPed.Opacity = 255;
        }

        public virtual void Start( float gameTime ) {
            LocalPlayer.Character.MaxHealth = 100;
            ClientGlobals.SetSpectator( false );
            ClientGlobals.WriteChat( Gamemode.ToUpper(), "Game started.", 255, 0, 0 );
            RemoveAllPedWeapons( PlayerPedId(), true );
            GameTimerEnd = GetGameTimer() + gameTime;
            HUD.Start();
        }

        public virtual void End() {
            ClientGlobals.WriteChat( Gamemode.ToUpper(), "Game finished!", 255, 0, 0 );
            Map.ClearObjects();
            ClientGlobals.SetSpectator( true );
            ClientGlobals.CurrentGame = null;
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
            ClientGlobals.Team = team;
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
