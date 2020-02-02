using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class SaltyWeapon : SaltyEntity {

        int AmmoInClip = -1;
        public int AmmoCount = 30;


        public SaltyWeapon( SpawnType entType, uint hash, Vector3 position ) : base( entType, hash, position ) {
            Model = Globals.Weapons[hash]["ModelHashKey"];
            AmmoCount = Convert.ToInt32(Globals.Weapons[hash]["DefaultClipSize"]);
            CreateEntity();
        }


        public override void Update() {

            if( !Equipped && Position.DistanceToSquared( LocalPlayer.Character.Position ) <= pickupRange && pickupTime - GetGameTimer() < 0 ) {
                if( ClientGlobals.CurrentGame != null ) {
                    if( LocalPlayer.Character.Weapons.HasWeapon((WeaponHash)Hash) ) {
                        ClientGlobals.CurrentGame.AddAmmo( Hash, AmmoCount );
                        Destroy();
                    }
                    else if( ClientGlobals.CurrentGame.CanPickupWeapon( Hash ) && IsControlJustReleased( 1, (int)eControl.ControlPickup ) ) {
                        Equip();
                    }
                } 
            }
        }

        public void Equip() {

            Debug.WriteLine( "Equipped" );

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
