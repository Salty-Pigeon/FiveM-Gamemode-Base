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
            map.Rotation = data.Rotation;
            map.MinPlayers = data.MinPlayers;
            map.MaxPlayers = data.MaxPlayers;

            map.Spawns = new List<Spawn>();
            foreach( var spawnData in data.Spawns ) {
                map.Spawns.Add( Spawn.FromSpawnData( spawnData ) );
            }

            return map;
        }

        /// <summary>
        /// Rotates a local offset (dx, dy) around the center by the map's Rotation (degrees).
        /// </summary>
        private Vector3 RotatePoint( float dx, float dy, float z ) {
            float rad = Rotation * ((float)System.Math.PI / 180f);
            float cos = (float)System.Math.Cos( rad );
            float sin = (float)System.Math.Sin( rad );
            return new Vector3(
                Position.X + dx * cos - dy * sin,
                Position.Y + dx * sin + dy * cos,
                z
            );
        }

        /// <summary>
        /// Draws a filled wall quad (two-sided) between two bottom and two top corners using DrawPoly.
        /// </summary>
        private void DrawWall( float x1, float y1, float x2, float y2, float zBot, float zTop, int r, int g, int b, int a ) {
            // Front side (two triangles)
            DrawPoly( x1, y1, zBot, x2, y2, zBot, x2, y2, zTop, r, g, b, a );
            DrawPoly( x1, y1, zBot, x2, y2, zTop, x1, y1, zTop, r, g, b, a );
            // Back side (reversed winding)
            DrawPoly( x2, y2, zBot, x1, y1, zBot, x1, y1, zTop, r, g, b, a );
            DrawPoly( x2, y2, zTop, x2, y2, zBot, x1, y1, zTop, r, g, b, a );
        }

        public void DrawBoundaries() {
            bool playerInside = IsInZone( Game.PlayerPed.Position );
            int r = playerInside ? 30 : 255;
            int g = playerInside ? 200 : 30;
            int b = 30;
            int a = 50;

            float hx = Size.X / 2;
            float hy = Size.Y / 2;
            float zBot = Position.Z - 50f;
            float zTop = Position.Z + 200f;

            // Four corners in local space, rotated to world
            Vector3 nw = RotatePoint( -hx, -hy, 0 );
            Vector3 ne = RotatePoint(  hx, -hy, 0 );
            Vector3 se = RotatePoint(  hx,  hy, 0 );
            Vector3 sw = RotatePoint( -hx,  hy, 0 );

            // Filled boundary walls (4 walls)
            DrawWall( nw.X, nw.Y, ne.X, ne.Y, zBot, zTop, r, g, b, a ); // North
            DrawWall( ne.X, ne.Y, se.X, se.Y, zBot, zTop, r, g, b, a ); // East
            DrawWall( se.X, se.Y, sw.X, sw.Y, zBot, zTop, r, g, b, a ); // South
            DrawWall( sw.X, sw.Y, nw.X, nw.Y, zBot, zTop, r, g, b, a ); // West

            // Corner posts (tall vertical markers)
            float postSize = 0.3f;
            float postBot = Position.Z;
            float postTop = Position.Z + 40f;
            int pr = 255; int pg = 255; int pb = 0; int pa = 120;

            DrawBox( nw.X - postSize, nw.Y - postSize, postBot, nw.X + postSize, nw.Y + postSize, postTop, pr, pg, pb, pa );
            DrawBox( ne.X - postSize, ne.Y - postSize, postBot, ne.X + postSize, ne.Y + postSize, postTop, pr, pg, pb, pa );
            DrawBox( se.X - postSize, se.Y - postSize, postBot, se.X + postSize, se.Y + postSize, postTop, pr, pg, pb, pa );
            DrawBox( sw.X - postSize, sw.Y - postSize, postBot, sw.X + postSize, sw.Y + postSize, postTop, pr, pg, pb, pa );

            // Height ceiling visualization
            if( Size.Z > 0 ) {
                float zCeil = Position.Z + (Size.Z / 2);
                // Ceiling as filled quad (two-sided)
                DrawPoly( nw.X, nw.Y, zCeil, ne.X, ne.Y, zCeil, se.X, se.Y, zCeil, 100, 100, 255, 30 );
                DrawPoly( nw.X, nw.Y, zCeil, se.X, se.Y, zCeil, sw.X, sw.Y, zCeil, 100, 100, 255, 30 );
                DrawPoly( se.X, se.Y, zCeil, ne.X, ne.Y, zCeil, nw.X, nw.Y, zCeil, 100, 100, 255, 30 );
                DrawPoly( sw.X, sw.Y, zCeil, se.X, se.Y, zCeil, nw.X, nw.Y, zCeil, 100, 100, 255, 30 );
            }
        }

        public void DrawSpawns() {
            foreach( Spawn spawn in Spawns ) {
                // Team-colored markers
                int mr = spawn.R;
                int mg = spawn.G;
                int mb = spawn.B;

                if( spawn.SpawnType == SpawnType.WIN_BARRIER ) {
                    mr = 255; mg = 200; mb = 0;
                }

                // Add team distinction for player spawns
                if( spawn.SpawnType == SpawnType.PLAYER ) {
                    switch( spawn.Team ) {
                        case 0: mr = 255; mg = 50; mb = 50; break;   // Team 0 = Red
                        case 1: mr = 50; mg = 50; mb = 255; break;   // Team 1 = Blue
                        case 2: mr = 50; mg = 255; mb = 50; break;   // Team 2 = Green
                        case 3: mr = 255; mg = 255; mb = 50; break;  // Team 3 = Yellow
                    }
                }

                if( spawn.SpawnType == SpawnType.WIN_BARRIER ) {
                    // Draw rotated box outline on ground
                    float hx = spawn.SizeX > 0 ? spawn.SizeX / 2f : 2.5f;
                    float hy = spawn.SizeY > 0 ? spawn.SizeY / 2f : 2.5f;
                    float rad = spawn.Heading * ((float)System.Math.PI / 180f);
                    float cos = (float)System.Math.Cos( rad );
                    float sin = (float)System.Math.Sin( rad );
                    float z = spawn.Position.Z + 0.3f;

                    // Four corners rotated around center
                    float nwX = spawn.Position.X + (-hx) * cos - (-hy) * sin;
                    float nwY = spawn.Position.Y + (-hx) * sin + (-hy) * cos;
                    float neX = spawn.Position.X + ( hx) * cos - (-hy) * sin;
                    float neY = spawn.Position.Y + ( hx) * sin + (-hy) * cos;
                    float seX = spawn.Position.X + ( hx) * cos - ( hy) * sin;
                    float seY = spawn.Position.Y + ( hx) * sin + ( hy) * cos;
                    float swX = spawn.Position.X + (-hx) * cos - ( hy) * sin;
                    float swY = spawn.Position.Y + (-hx) * sin + ( hy) * cos;

                    // Box outline
                    DrawLine( nwX, nwY, z, neX, neY, z, mr, mg, mb, 255 );
                    DrawLine( neX, neY, z, seX, seY, z, mr, mg, mb, 255 );
                    DrawLine( seX, seY, z, swX, swY, z, mr, mg, mb, 255 );
                    DrawLine( swX, swY, z, nwX, nwY, z, mr, mg, mb, 255 );

                    // Fill with semi-transparent polys
                    DrawPoly( nwX, nwY, z, neX, neY, z, seX, seY, z, mr, mg, mb, 40 );
                    DrawPoly( nwX, nwY, z, seX, seY, z, swX, swY, z, mr, mg, mb, 40 );
                    DrawPoly( seX, seY, z, neX, neY, z, nwX, nwY, z, mr, mg, mb, 40 );
                    DrawPoly( swX, swY, z, seX, seY, z, nwX, nwY, z, mr, mg, mb, 40 );

                    // Checkered flag marker at center
                    DrawMarker( 4, spawn.Position.X, spawn.Position.Y, spawn.Position.Z + 1,
                        0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f,
                        1.5f, 1.5f, 1.5f, mr, mg, mb, 120,
                        false, true, 2, false, null, null, false );
                } else {
                    DrawMarker( 2, spawn.Position.X, spawn.Position.Y, spawn.Position.Z + 1,
                        0.0f, 0.0f, 0.0f, 0.0f, 180.0f, 0.0f,
                        2.0f, 2.0f, 2.0f, mr, mg, mb, 80,
                        false, true, 2, false, null, null, false );
                }

                // Heading direction line
                if( spawn.SpawnType == SpawnType.PLAYER ) {
                    float hRad = spawn.Heading * ((float)System.Math.PI / 180f);
                    float dirX = spawn.Position.X - (float)System.Math.Sin( hRad ) * 2.5f;
                    float dirY = spawn.Position.Y + (float)System.Math.Cos( hRad ) * 2.5f;
                    DrawLine( spawn.Position.X, spawn.Position.Y, spawn.Position.Z + 0.5f,
                              dirX, dirY, spawn.Position.Z + 0.5f,
                              255, 255, 255, 255 );
                }

                // 3D text label above spawn
                string label = spawn.SpawnType.ToString();
                if( spawn.SpawnType == SpawnType.PLAYER ) {
                    label = "T" + spawn.Team + " H:" + spawn.Heading.ToString( "0" );
                } else if( spawn.SpawnType == SpawnType.WIN_BARRIER ) {
                    label = "BARRIER " + spawn.SizeX.ToString( "0" ) + "x" + spawn.SizeY.ToString( "0" ) + " R:" + spawn.Heading.ToString( "0" );
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
