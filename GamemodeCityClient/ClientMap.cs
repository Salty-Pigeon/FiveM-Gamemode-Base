using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamemodeCityShared;


namespace GamemodeCityClient {
    public class ClientMap : Map {

        public bool JustCreated = false;

        public ClientMap( int id, string name, List<string> gamemode, Vector3 position, Vector3 size, bool justCreated ) : base ( id, name, gamemode, position, size ) {
            JustCreated = justCreated;
        }

        public void DrawBoundarys() {

            // Top box
            DrawBox(Position.X - (Size.X / 2), Position.Y - (Size.Y / 2), 0, Position.X + (Size.X / 2), Position.Y - (Size.Y / 2) - 0.1f, 1000, 255, 255, 255, 50);

            // Left box
            DrawBox(Position.X - (Size.X / 2), Position.Y - (Size.Y / 2), 0, Position.X - (Size.X / 2) - 0.1f, Position.Y + (Size.Y / 2), 1000, 255, 255, 255, 50);

            // Right box
            DrawBox(Position.X + (Size.X / 2), Position.Y + (Size.Y / 2), 0, Position.X + (Size.X / 2) + 0.1f, Position.Y - (Size.Y / 2), 1000, 255, 255, 255, 50);

            // Bottom box
            DrawBox(Position.X - (Size.X / 2), Position.Y + (Size.Y / 2), 0, Position.X + (Size.X / 2), Position.Y + (Size.Y / 2) + 0.1f, 1000, 255, 255, 255, 50);

            // Roof
            DrawBox(Position.X - (Size.X / 2), Position.Y - (Size.Y / 2), 1000, Position.X + (Size.X / 2), Position.Y + (Size.Y / 2), 1000.1f, 255, 255, 255, 50);
        }

        public void DrawSpawns() {
            var i = 0;
            foreach( Spawn spawn in Spawns) {
                DrawMarker(2, spawn.Position.X, spawn.Position.Y, spawn.Position.Z, 0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f, 2.0f, 2.0f, 2.0f, i / 10 * 6, i, i / 3, 200, false, true, 2, false, null, null, false);
                i += 50;
            }
        }

        public void ClearObjects() {
            ClearAreaOfObjects(Position.X, Position.Y, Position.Z, Size.X + Size.Y + Size.Z, 0);
            ClearAreaOfProjectiles(Position.X, Position.Y, Position.Z, Size.X + Size.Y + Size.Z, true);
        }

    }
}
