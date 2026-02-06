using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class ClientMap : Map {

        public bool Draw = false;
        public List<SaltyWeapon> Weapons = new List<SaltyWeapon>();
        private int MapBlip = -1;

        public ClientMap( int id, string name, List<string> gamemode, Vector3 position, Vector3 size, bool justCreated ) : base( id, name, gamemode, position, size ) {
            JustCreated = justCreated;
        }

        public static ClientMap FromMapData( MapData data ) {
            var map = new ClientMap(
                data.Id,
                data.Name,
                new List<string>( data.Gamemodes ),
                new Vector3( data.PosX, data.PosY, data.PosZ ),
                new Vector3( data.SizeX, data.SizeY, data.SizeZ ),
                false
            );
            map.Author = data.Author;
            map.Description = data.Description;
            map.Enabled = data.Enabled;
            map.MinPlayers = data.MinPlayers;
            map.MaxPlayers = data.MaxPlayers;

            map.Spawns = new List<Spawn>();
            foreach( var spawnData in data.Spawns ) {
                map.Spawns.Add( Spawn.FromSpawnData( spawnData ) );
            }

            return map;
        }

        public void DrawBoundaries() {
            bool playerInside = IsInZone( Game.PlayerPed.Position );
            int r = playerInside ? 30 : 255;
            int g = playerInside ? 200 : 30;
            int b = 30;
            int a = 50;

            // Boundary walls
            // North wall
            DrawBox( Position.X - (Size.X / 2), Position.Y - (Size.Y / 2), Position.Z - 50,
                     Position.X + (Size.X / 2), Position.Y - (Size.Y / 2) - 0.1f, Position.Z + 200,
                     r, g, b, a );
            // South wall
            DrawBox( Position.X - (Size.X / 2), Position.Y + (Size.Y / 2), Position.Z - 50,
                     Position.X + (Size.X / 2), Position.Y + (Size.Y / 2) + 0.1f, Position.Z + 200,
                     r, g, b, a );
            // West wall
            DrawBox( Position.X - (Size.X / 2), Position.Y - (Size.Y / 2), Position.Z - 50,
                     Position.X - (Size.X / 2) - 0.1f, Position.Y + (Size.Y / 2), Position.Z + 200,
                     r, g, b, a );
            // East wall
            DrawBox( Position.X + (Size.X / 2), Position.Y - (Size.Y / 2), Position.Z - 50,
                     Position.X + (Size.X / 2) + 0.1f, Position.Y + (Size.Y / 2), Position.Z + 200,
                     r, g, b, a );

            // Corner posts (tall vertical markers)
            float postSize = 0.3f;
            float postHeight = 40f;
            int pr = 255; int pg = 255; int pb = 0; int pa = 120;

            // NW corner
            DrawBox( Position.X - (Size.X / 2) - postSize, Position.Y - (Size.Y / 2) - postSize, Position.Z,
                     Position.X - (Size.X / 2) + postSize, Position.Y - (Size.Y / 2) + postSize, Position.Z + postHeight,
                     pr, pg, pb, pa );
            // NE corner
            DrawBox( Position.X + (Size.X / 2) - postSize, Position.Y - (Size.Y / 2) - postSize, Position.Z,
                     Position.X + (Size.X / 2) + postSize, Position.Y - (Size.Y / 2) + postSize, Position.Z + postHeight,
                     pr, pg, pb, pa );
            // SW corner
            DrawBox( Position.X - (Size.X / 2) - postSize, Position.Y + (Size.Y / 2) - postSize, Position.Z,
                     Position.X - (Size.X / 2) + postSize, Position.Y + (Size.Y / 2) + postSize, Position.Z + postHeight,
                     pr, pg, pb, pa );
            // SE corner
            DrawBox( Position.X + (Size.X / 2) - postSize, Position.Y + (Size.Y / 2) - postSize, Position.Z,
                     Position.X + (Size.X / 2) + postSize, Position.Y + (Size.Y / 2) + postSize, Position.Z + postHeight,
                     pr, pg, pb, pa );

            // Height ceiling visualization
            if( Size.Z > 0 ) {
                DrawBox( Position.X - (Size.X / 2), Position.Y - (Size.Y / 2), Position.Z + (Size.Z / 2),
                         Position.X + (Size.X / 2), Position.Y + (Size.Y / 2), Position.Z + (Size.Z / 2) + 0.1f,
                         100, 100, 255, 30 );
            }
        }

        public void DrawSpawns() {
            foreach( Spawn spawn in Spawns ) {
                // Team-colored markers
                int mr = spawn.R;
                int mg = spawn.G;
                int mb = spawn.B;

                // Add team distinction for player spawns
                if( spawn.SpawnType == SpawnType.PLAYER ) {
                    switch( spawn.Team ) {
                        case 0: mr = 255; mg = 50; mb = 50; break;   // Team 0 = Red
                        case 1: mr = 50; mg = 50; mb = 255; break;   // Team 1 = Blue
                        case 2: mr = 50; mg = 255; mb = 50; break;   // Team 2 = Green
                        case 3: mr = 255; mg = 255; mb = 50; break;  // Team 3 = Yellow
                    }
                }

                DrawMarker( 2, spawn.Position.X, spawn.Position.Y, spawn.Position.Z + 1,
                    0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f,
                    2.0f, 2.0f, 2.0f, mr, mg, mb, 80,
                    false, true, 2, false, null, null, false );

                // 3D text label above spawn
                string label = spawn.SpawnType.ToString();
                if( spawn.SpawnType == SpawnType.PLAYER ) {
                    label = "Team " + spawn.Team;
                }

                float dist = World.GetDistance( Game.PlayerPed.Position, spawn.Position );
                if( dist < 30f ) {
                    float scale = 0.3f;
                    SetTextScale( scale, scale );
                    SetTextFont( 0 );
                    SetTextColour( mr, mg, mb, 255 );
                    SetTextCentre( true );
                    SetTextDropshadow( 2, 0, 0, 0, 255 );
                    SetTextEntry( "STRING" );
                    AddTextComponentString( label );
                    SetDrawOrigin( spawn.Position.X, spawn.Position.Y, spawn.Position.Z + 3.5f, 0 );
                    DrawTextInterface( 0f, 0f );
                    ClearDrawOrigin();
                }
            }
        }

        public void CreateBlip() {
            if( MapBlip != -1 ) RemoveBlip();

            MapBlip = AddBlipForCoord( Position.X, Position.Y, Position.Z );
            SetBlipSprite( MapBlip, 398 );
            SetBlipDisplay( MapBlip, 4 );
            SetBlipScale( MapBlip, 1.0f );
            SetBlipColour( MapBlip, 5 );
            SetBlipAsShortRange( MapBlip, false );
            BeginTextCommandSetBlipName( "STRING" );
            AddTextComponentString( "Map: " + Name );
            EndTextCommandSetBlipName( MapBlip );
        }

        public void RemoveBlip() {
            if( MapBlip != -1 ) {
                int blip = MapBlip;
                RemoveBlipFunc( ref blip );
                MapBlip = -1;
            }
        }

        private void RemoveBlipFunc( ref int blip ) {
            CitizenFX.Core.Native.API.RemoveBlip( ref blip );
        }

        public void RemoveObject( SaltyEntity wep ) {
            Weapons.Remove( wep as SaltyWeapon );
        }

        public void ClearObjects() {
            foreach( var wep in Weapons.ToList() ) {
                wep.Destroy();
            }
            ClearAreaOfObjects( Position.X, Position.Y, Position.Z, Size.X + Size.Y + Size.Z, 0 );
            ClearAreaOfProjectiles( Position.X, Position.Y, Position.Z, Size.X + Size.Y + Size.Z, 0 );
        }

        private static void DrawTextInterface( float x, float y ) {
            EndTextCommandDisplayText( x, y );
        }
    }
}
