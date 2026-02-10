using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooShared;

namespace GTA_GameRooClient {
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
            int modelHash = GetHashKey( Model );
            ID = CreateObject( modelHash, Position.X, Position.Y, Position.Z, true, true, false);
            Debug.WriteLine( "[SaltyEntity] CreateEntity model=" + Model + " modelHash=" + modelHash + " ID=" + ID + " inputPos=" + Position.X.ToString("F1") + "," + Position.Y.ToString("F1") + "," + Position.Z.ToString("F1") );
            if( ID == 0 ) {
                Debug.WriteLine( "[SaltyEntity] FAIL: CreateObject returned 0! Model may not be loaded." );
                return;
            }
            Drop();
            Position = GetEntityCoords( ID, true );
            Debug.WriteLine( "[SaltyEntity] finalPos=" + Position.X.ToString("F1") + "," + Position.Y.ToString("F1") + "," + Position.Z.ToString("F1") + " exists=" + DoesEntityExist( ID ) );
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
