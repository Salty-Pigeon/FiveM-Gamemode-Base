using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooShared;

namespace GTA_GameRooClient {
    public class SaltyWeapon : SaltyEntity {

        public int AmmoInClip = -1;
        public int AmmoCount = 30;


        public SaltyWeapon( SpawnType entType, uint hash, Vector3 position ) : base( entType, hash, position ) {
            if( Globals.Weapons.ContainsKey( hash ) ) {
                Model = Globals.Weapons[hash]["ModelHashKey"];
                AmmoCount = Convert.ToInt32( Globals.Weapons[hash]["DefaultClipSize"] );
                if( !string.IsNullOrEmpty( Model ) ) {
                    CreateEntity();
                }
            }
        }


        public override void Update() {

            if( !Equipped && Position.DistanceToSquared( LocalPlayer.Character.Position ) <= pickupRange && pickupTime - GetGameTimer() < 0 ) {
                if( ClientGlobals.CurrentGame != null ) {
                    if( LocalPlayer.Character.Weapons.HasWeapon((WeaponHash)Hash) ) {
                        ClientGlobals.CurrentGame.AddAmmo( Hash, AmmoCount );
                        Destroy();
                    }
                    else if( !BaseGamemode.SuppressWeaponPickup && IsControlJustReleased( 1, (int)eControl.ControlPickup ) ) {
                        if( ClientGlobals.CurrentGame.CanPickupWeapon( Hash ) ) {
                            Equip();
                        } else {
                            HUD.ShowPopup( "Already carrying this weapon type", 200, 160, 30 );
                        }
                    }
                } 
            }
        }

        public void Equip() {

            Equipped = true;
            int pedId = PlayerPedId();

            GiveWeaponToPed( pedId, Hash, AmmoCount, false, true );

            if( AmmoInClip >= 0 )
                SetAmmoInClip( pedId, Hash, AmmoInClip );

            SetPedAmmo( pedId, Hash, AmmoCount );

            ClientGlobals.CurrentGame.OnWeaponPickup( Hash );

            Destroy();
        }

    }
}
