using System;
using System.Collections.Generic;

namespace GamemodeCityShared {

    public class MapData {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float SizeX { get; set; }
        public float SizeY { get; set; }
        public float SizeZ { get; set; }
        public List<string> Gamemodes { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public List<SpawnData> Spawns { get; set; }

        public MapData() {
            Gamemodes = new List<string>();
            Spawns = new List<SpawnData>();
            Enabled = true;
            MinPlayers = 2;
            MaxPlayers = 32;
            Author = "";
            Description = "";
            Name = "unnamed";
        }
    }

    public class SpawnData {
        public int Id { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public int SpawnType { get; set; }
        public string Entity { get; set; }
        public int Team { get; set; }

        public SpawnData() {
            Entity = "player";
        }
    }
}
