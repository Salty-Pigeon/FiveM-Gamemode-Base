using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityServer {
    public class Settings {

        public List<uint> Weapons = new List<uint>();
        public Dictionary<uint, float> WeaponWeights = new Dictionary<uint, float>();
        public float GameLength = 0;

        public int Lives = 0;
    }
}
