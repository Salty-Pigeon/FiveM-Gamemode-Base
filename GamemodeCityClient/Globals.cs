using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GamemodeCityClient {
    public class Globals : BaseScript {

        public static Dictionary<string,Dictionary<string, dynamic>> Weapons = new Dictionary<string,Dictionary<string, dynamic>>();
        public static Dictionary<string, BaseGamemode> Gamemodes = new Dictionary<string, BaseGamemode>();

        public static bool isNoclip = false;


        public static void Init() {

            var loadFile = LoadResourceFile(GetCurrentResourceName(), "./weapons.json");
            Weapons = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(loadFile);

        }

        public static void WriteChat( string prefix, string str, int r, int g, int b ) {
            TriggerEvent("chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            });
        }

        public static Vector3 StringToVector3( string vector ) {
            vector = vector.Replace("X:", "");
            vector = vector.Replace("Y:", "");
            vector = vector.Replace("Z:", "");
            string[] vector3 = vector.Split(' ');
            return new Vector3(float.Parse(vector3[0]), float.Parse(vector3[1]), float.Parse(vector3[2]));
        }

        public static void SetNoClip( bool toggle ) {
            isNoclip = toggle;
            SetEntityVisible(PlayerPedId(), !isNoclip, false);
            SetEntityCollision(PlayerPedId(), !isNoclip, !isNoclip);
            SetEntityInvincible(PlayerPedId(), isNoclip);
            SetEveryoneIgnorePlayer(PlayerPedId(), isNoclip);
        }


        public static void NoClipUpdate() {

            Vector3 heading = GetGameplayCamRot(0);
            SetEntityRotation(PlayerPedId(), heading.X, heading.Y, -heading.Z, 0, true);
            SetEntityHeading(PlayerPedId(), heading.Z);

            int speed = 1;

            if (IsControlPressed(0, 21)) {
                speed *= 6;
            }

            Vector3 offset = new Vector3(0, 0, 0);

            if (IsControlPressed(0, 36)) {
                offset.Z = -speed;
            }

            if (IsControlPressed(0, 22)) {
                offset.Z = speed;
            }

            if (IsControlPressed(0, 33)) {
                offset.Y = -speed;
            }

            if (IsControlPressed(0, 32)) {
                offset.Y = speed;
            }

            if (IsControlPressed(0, 35)) {
                offset.X = speed;
            }

            if (IsControlPressed(0, 34)) {
                offset.X = -speed;
            }

            var noclipPos = GetOffsetFromEntityInWorldCoords(PlayerPedId(), offset.X, offset.Y, offset.Z);
            SetEntityCoordsNoOffset(PlayerPedId(), noclipPos.X, noclipPos.Y, noclipPos.Z, false, false, false);

        }

        public static void FirstPersonForAlive() {
            DisableControlAction(0, 0, true);
            if (GetFollowPedCamViewMode() != 4)
                SetFollowPedCamViewMode(4);
        }

    }
}
