using CitizenFX.Core;
using System;
using System.Collections.Generic;

namespace GamemodeCityShared {
    public class Spawn : BaseScript {

        public Vector3 Position;
        public float Heading = 0f;
        public SpawnType SpawnType;
        public string Entity;
        public int Team;
        public int ID;
        public int R = 0;
        public int G = 0;
        public int B = 0;

        public Spawn( int id, Vector3 position, SpawnType type, string entName, int team, float heading = 0f ) {
            ID = id;
            Position = position;
            Heading = heading;
            SpawnType = type;
            Entity = entName;
            Team = team;
            switch( type ) {
                case SpawnType.PLAYER:
                    R = 200;
                    break;
                case SpawnType.OBJECT:
                    G = 200;
                    break;
                case SpawnType.WEAPON:
                    B = 200;
                    break;
            }
        }

        public SpawnData ToSpawnData() {
            return new SpawnData {
                Id = ID,
                PosX = Position.X,
                PosY = Position.Y,
                PosZ = Position.Z,
                Heading = Heading,
                SpawnType = (int)SpawnType,
                Entity = Entity,
                Team = Team
            };
        }

        public static Spawn FromSpawnData( SpawnData data ) {
            return new Spawn( data.Id, new Vector3( data.PosX, data.PosY, data.PosZ ), (SpawnType)data.SpawnType, data.Entity, data.Team, data.Heading );
        }
    }

    public enum SpawnType {
        PLAYER,
        WEAPON,
        OBJECT
    }
}
