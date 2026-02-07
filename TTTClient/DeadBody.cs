using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityClient;
using GamemodeCityShared;

namespace TTTClient {
    public class DeadBody : BaseScript {

        public int ServerID;
        public int ID = -1;
        public uint Model;
        public int PlayerPed;
        public int PlayerID;
        public int KillerPed;
        public int KillerID;
        public Vector3 Position;
        public string Name;
        public uint WeaponHash;

        public string Team;

        public bool isDiscovered = false;
        public string Caption = "Unidentified body [E]";

        public DeadBody( Vector3 position,int plyID, int killerID, uint weaponHash ) {
            PlayerPed = GetPlayerPed( plyID );
            Model = (uint)GetEntityModel( PlayerPed );
            KillerPed = GetPlayerPed(killerID);
            PlayerID = plyID;
            KillerID = killerID;
            Name = GetPlayerName( plyID );
            Position = position;
            WeaponHash = weaponHash;
            ID = CreatePed( 4, Model, Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z + 1, 0.0f, true, true );

        }

        public DeadBody( Vector3 position, uint model, string name, string team ) {
            Model = model;
            Name = name;
            Team = team;
            Position = position;
            PlayerID = -1;
            KillerID = -1;
            PlayerPed = -1;
            KillerPed = -1;
            WeaponHash = 0;
            ID = CreatePed( 4, Model, position.X, position.Y, position.Z + 1, 0.0f, true, true );
        }

        public void Detective() {

        }

        public void Discovered() {
            isDiscovered = true;
            string teamText = !string.IsNullOrEmpty( Team ) ? " (" + Team + ")" : "";
            if( Globals.Weapons.ContainsKey( WeaponHash ) ) {
                Caption = Name + teamText + " killed with " + Globals.Weapons[WeaponHash]["Name"];
            } else {
                Caption = Name + teamText;
            }
        }

        public void View() {

        }

        public void Update() {
            SetPedToRagdoll( ID, -1, -1, 0, true, true, true );
        }

    }
}
