using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class Map : BaseScript {

        public Vector3 Position;
        public Vector3 Size;
        public string Name;

        List<Spawn> Spawns = new List<Spawn>();

        public Map( string name, Vector3 pos, Vector3 size ) {
            Name = name;
            Position = pos;
            Size = size;
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

        public bool IsInBounds( Vector3 pos ) {
            return (pos.X > Position.X - (Size.X / 2) && pos.X < Position.X + (Size.X / 2) && pos.Y > Position.Y - (Size.Y / 2) && pos.Y < Position.Y + (Size.Y / 2));
        }

        public void ClearObjects() {
            ClearAreaOfObjects(Position.X, Position.Y, Position.Z, Size.X + Size.Y + Size.Z, 0);
            ClearAreaOfProjectiles(Position.X, Position.Y, Position.Z, Size.X + Size.Y + Size.Z, true);
        }

    }
}
