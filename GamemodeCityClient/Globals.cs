using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GamemodeCityClient {
    class Globals : BaseScript {

        public static Dictionary<string,Dictionary<string, dynamic>> Weapons = new Dictionary<string,Dictionary<string, dynamic>>();

        public static void Init() {

            var loadFile = LoadResourceFile(GetCurrentResourceName(), "./weapons.json");
            Weapons = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(loadFile);

        }
    }
}
