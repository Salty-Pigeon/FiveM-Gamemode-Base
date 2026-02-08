using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA_GameRooServer {
    public class Settings {

        public List<uint> Weapons = new List<uint>();
        public Dictionary<uint, float> WeaponWeights = new Dictionary<uint, float>();
        public float GameLength = 0;
        public string Name = "";
        public int Lives = 0;
        public int Rounds = 3;
        public float PreGameTime = 1 * 1000 * 30;
    }
}
