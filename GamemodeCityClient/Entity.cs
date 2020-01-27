using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    class Entity : BaseScript {

        int ID;
        public int Hash;
        public Vector3 Position;

        float pickupTime = 0;
        float pickupDelay;

        public enum Type {
            OBJECT,
            WEAPON
        }

        Type EntityType;

        public Entity( Type entType, int hash, Vector3 position ) {
            EntityType = entType;
            Hash = hash;
            Position = position;
            
        }

        public void CreateEntity() {
            ID = CreateObject(Hash, Position.X, Position.Y, Position.Z, true, true, true);
        }

        public void Drop() {
            SetObjectPhysicsParams(ID, 10, 10, 10, 3, 3, 10, 10, 10, 10, 10, 10);
            PlaceObjectOnGroundProperly(ID);
            ActivatePhysics(ID);
            pickupTime = GetGameTimer() + pickupDelay;
        }

        public void Pickup() {
            if (pickupTime > GetGameTimer())
                return;
        }

    }
}
