using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class SaltyWeapon : SaltyEntity {

        public string WeaponModel;
        public uint WeaponHash;

        public SaltyWeapon( Type entType, int hash, Vector3 position ) : base( entType, hash, position ) {

        }



    }
}
