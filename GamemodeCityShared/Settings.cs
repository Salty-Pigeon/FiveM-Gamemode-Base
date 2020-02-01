using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityShared {
    public class Settings {

        public List<uint> Weapons = new List<uint>();
        public Dictionary<uint, float> WeaponWeights = new Dictionary<uint, float>();
        public float GameLength;

        public int Teams = 0;
        public int Lives = 0;
    }
}
