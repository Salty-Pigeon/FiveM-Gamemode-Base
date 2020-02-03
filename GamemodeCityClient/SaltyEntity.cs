using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class SaltyEntity : BaseScript {

        int ID;
        public uint Hash;
        public string Model;
        public Vector3 Position;

        public float pickupTime = 0;
        public float pickupDelay = 1 * 1000;
        public float pickupRange = 5;

        public bool Equipped = false;


        SpawnType EntityType;

        public SaltyEntity( SpawnType entType, uint hash, Vector3 position ) {
            EntityType = entType;
            Hash = hash;
            Position = position;
        }

        public void CreateEntity() {
            ID = CreateObject( GetHashKey( Model ), Position.X, Position.Y, Position.Z, true, true, false);
            //SetObjectPhysicsParams( ID, 100, 10, 10, 3, 3, 10, 10, 10, 10, 10, 10 );
            //PlaceObjectOnGroundProperly( ID );
            //SetObjectSomething( ID, true );
            //Drop();
            //ActivatePhysics( ID );
            Drop();
            Position = GetEntityCoords( ID, true );
        }

        public void Drop() {
            Equipped = false;
            SetObjectPhysicsParams(ID, 100, 10, 10, 3, 3, 10, 10, 10, 10, 10, 10);
            PlaceObjectOnGroundProperly(ID);
            ActivatePhysics(ID);
            pickupTime = GetGameTimer() + pickupDelay;
        }

        public void Pickup() {
            if (pickupTime > GetGameTimer())
                return;
        }

        public virtual void Update() {

        }

        public void Destroy() {
            DeleteObject( ref ID );
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.Map.RemoveObject( this );
            }
        }

    }
}
