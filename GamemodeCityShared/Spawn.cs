﻿using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityShared {
    public class Spawn : BaseScript {

        
        public Vector3 Position;
        public SpawnType SpawnType;
        public string Entity;
        public int Team;
        public int ID;
        public int R = 0;
        public int G = 0;
        public int B = 0;

        public Spawn( int id, Vector3 position, SpawnType type, string entName, int team ) {
            ID = id;
            Position = position;
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

        public Dictionary<string,dynamic> SpawnAsSendable() {
            return new Dictionary<string, dynamic> { { "id", ID }, { "position", Position }, { "spawntype", (int)SpawnType }, { "entity", Entity }, { "team", Team } };
        }

        public static Spawn SpawnRecieved( IDictionary<string, dynamic> spawn ) {
            return new Spawn( spawn["id"], spawn["position"], spawn["spawntype"], spawn["entity"], spawn["team"] );
        }

    }

    public enum SpawnType {
        PLAYER,
        WEAPON,
        OBJECT
    }
}
