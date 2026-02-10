using GTA_GameRooServer;
using GTA_GameRooShared;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BRServer {

    public class ZonePhase {
        public float WaitSeconds;
        public float ShrinkSeconds;
        public float RadiusPercent;
    }

    public class PlaneData {
        public float StartX, StartY, StartZ;
        public float EndX, EndY, EndZ;
        public float Speed;
        public List<string> AssignedPlayers;
    }

    public class Main : BaseGamemode {

        Random rand = new Random();

        // Zone state
        float zoneCenterX, zoneCenterY;
        float zoneCurrentRadius;
        float zoneTargetRadius;
        float zoneShrinkStart;
        float zoneShrinkEnd;
        int currentPhase = -1;
        float phaseWaitUntil = 0;
        bool isShrinking = false;
        float initialRadius;

        // Next zone preview
        float nextZoneCX, nextZoneCY, nextZoneRadius;
        bool hasNextZone = false;

        // Zone damage
        float zoneDamageTimer = 0;
        const float ZONE_DAMAGE_INTERVAL = 1000f;
        static readonly int[] ZONE_DAMAGE_PER_PHASE = { 5, 8, 12, 18, 25 };

        // Periodic zone sync
        float lastZoneBroadcast = 0;
        const float ZONE_BROADCAST_INTERVAL = 5000f;

        // Alive tracking
        List<Player> alivePlayers = new List<Player>();
        bool gameStarted = false;
        int totalStartPlayers = 0;

        // Plane tracking
        bool planesLaunched = false;
        float planeStartTime = 0;
        float planeDuration = 0;
        float planeForceJumpTime = 0;
        bool forceJumpTriggered = false;
        HashSet<string> jumpedPlayers = new HashSet<string>();
        HashSet<string> searchedContainers = new HashSet<string>();

        static readonly ZonePhase[] Phases = new ZonePhase[] {
            new ZonePhase { WaitSeconds = 90, ShrinkSeconds = 50, RadiusPercent = 0.80f },
            new ZonePhase { WaitSeconds = 75, ShrinkSeconds = 45, RadiusPercent = 0.55f },
            new ZonePhase { WaitSeconds = 45, ShrinkSeconds = 40, RadiusPercent = 0.30f },
            new ZonePhase { WaitSeconds = 25, ShrinkSeconds = 30, RadiusPercent = 0.15f },
            new ZonePhase { WaitSeconds = 10, ShrinkSeconds = 25, RadiusPercent = 0.02f },
        };

        // Weapon pools: hash -> group name for spawning
        static readonly uint[] PistolWeapons = { 453432689, 1593441988, 584646201, 3523564046, 2578377531 };
        static readonly uint[] SMGWeapons = { 736523883, 4024951519, 3173288789, 3675956304, 171789620 };
        static readonly uint[] ShotgunWeapons = { 487013001, 3800352039, 984333226, 2640438543 };
        static readonly uint[] RifleWeapons = { 3220176749, 2210333304, 2937143193, 2132975508, 3231910285 };
        static readonly uint[] SniperWeapons = { 100416529, 205991906, 3342088282 };
        static readonly uint[] MeleeWeapons = { 1317494643, 2508868239, 1141786504, 2578778090, 4191993645 };

        public Main() : base( "BR" ) {
            Settings.Weapons = new List<uint>();
            // Populate all BR weapons into settings for client weapon tracking
            Settings.Weapons.AddRange( PistolWeapons );
            Settings.Weapons.AddRange( SMGWeapons );
            Settings.Weapons.AddRange( ShotgunWeapons );
            Settings.Weapons.AddRange( RifleWeapons );
            Settings.Weapons.AddRange( SniperWeapons );
            Settings.Weapons.AddRange( MeleeWeapons );

            Settings.GameLength = 15 * 1000 * 60; // 15 minutes max
            Settings.Name = "Battle Royale";
            Settings.Rounds = 1;

            EventHandlers["br:playerJumped"] += new Action<Player>( OnPlayerJumped );
            EventHandlers["br:searchContainer"] += new Action<Player, float, float, float>( OnSearchContainer );
            EventHandlers["br:debugSpawnWeapon"] += new Action<Player, float, float, float>( OnDebugSpawnWeapon );
            EventHandlers["br:dropWeapon"] += new Action<Player, string, float, float, float, int, int>( OnDropWeapon );
        }

        public override void Start() {
            base.Start();

            PlayerList playerList = new PlayerList();
            alivePlayers.Clear();
            jumpedPlayers.Clear();
            searchedContainers.Clear();

            foreach( var player in playerList ) {
                alivePlayers.Add( player );
                SetTeam( player, 0 );
            }

            // Initialize zone
            zoneDamageTimer = 0;
            lastZoneBroadcast = 0;
            initialRadius = Math.Max( Map.Size.X, Map.Size.Y ) / 2f;
            zoneCenterX = Map.Position.X;
            zoneCenterY = Map.Position.Y;
            zoneCurrentRadius = initialRadius;
            zoneTargetRadius = initialRadius;
            currentPhase = -1;
            isShrinking = false;

            // Schedule first phase after plane ride + loot time
            phaseWaitUntil = GetGameTimer() + 60000; // 60s before first shrink

            // Launch planes
            LaunchPlanes( playerList );

            totalStartPlayers = alivePlayers.Count;
            gameStarted = true;
            Debug.WriteLine( "[BR-Server] START complete. gameStarted=true instance=" + GetHashCode() + " alivePlayers=" + alivePlayers.Count );

            BroadcastZoneUpdate();
            BroadcastAliveCount();
        }

        void ScatterWeapons() {
            SpawnWeaponGroup( PistolWeapons, rand.Next( 15, 21 ) );
            SpawnWeaponGroup( SMGWeapons, rand.Next( 10, 16 ) );
            SpawnWeaponGroup( ShotgunWeapons, rand.Next( 8, 13 ) );
            SpawnWeaponGroup( RifleWeapons, rand.Next( 6, 11 ) );
            SpawnWeaponGroup( SniperWeapons, rand.Next( 3, 6 ) );
            SpawnWeaponGroup( MeleeWeapons, rand.Next( 5, 9 ) );
        }

        void SpawnWeaponGroup( uint[] weapons, int count ) {
            for( int i = 0; i < count; i++ ) {
                uint hash = weapons[rand.Next( weapons.Length )];
                Vector3 pos = GetRandomPositionInCircle( zoneCenterX, zoneCenterY, Map.Position.Z, initialRadius );
                SpawnWeapon( pos, hash );
            }
        }

        Vector3 GetRandomPositionInCircle( float cx, float cy, float z, float radius ) {
            float angle = (float)( rand.NextDouble() * Math.PI * 2 );
            float r = radius * (float)Math.Sqrt( rand.NextDouble() );
            float x = cx + r * (float)Math.Cos( angle );
            float y = cy + r * (float)Math.Sin( angle );

            // Clamp to map bounds
            float halfX = Map.Size.X / 2f;
            float halfY = Map.Size.Y / 2f;
            x = Math.Max( Map.Position.X - halfX, Math.Min( Map.Position.X + halfX, x ) );
            y = Math.Max( Map.Position.Y - halfY, Math.Min( Map.Position.Y + halfY, y ) );

            return new Vector3( x, y, z );
        }

        void LaunchPlanes( PlayerList playerList ) {
            var players = playerList.ToList();
            int playerCount = players.Count;
            int planeCount = Math.Min( 4, (int)Math.Ceiling( playerCount / 15.0 ) );
            if( planeCount < 1 ) planeCount = 1;

            float mapHalfX = Map.Size.X / 2f;
            float mapHalfY = Map.Size.Y / 2f;
            float altitude = Map.Position.Z + 500f;
            float speed = 50f;

            var planes = new List<PlaneData>();

            for( int p = 0; p < planeCount; p++ ) {
                float lerpFactor = planeCount == 1 ? 0.5f : (float)p / ( planeCount - 1 );
                float offsetX = Map.Position.X - mapHalfX + lerpFactor * Map.Size.X;

                planes.Add( new PlaneData {
                    StartX = offsetX,
                    StartY = Map.Position.Y - mapHalfY - 100f,
                    StartZ = altitude,
                    EndX = offsetX,
                    EndY = Map.Position.Y + mapHalfY + 100f,
                    EndZ = altitude,
                    Speed = speed,
                    AssignedPlayers = new List<string>()
                } );
            }

            // Round-robin assign players to planes
            for( int i = 0; i < players.Count; i++ ) {
                planes[i % planeCount].AssignedPlayers.Add( players[i].Handle );
            }

            // Calculate flight duration
            float totalDist = Map.Size.Y + 200f;
            planeDuration = ( totalDist / speed ) * 1000f;
            planeStartTime = GetGameTimer();
            planesLaunched = true;
            forceJumpTriggered = false;

            // Calculate when the plane reaches the far edge of the zone circle
            // Planes start 100 units south of map edge, fly north through center
            // Force jump when plane is about to exit the initial zone radius
            float distToZoneExit = ( mapHalfY + 100f ) + initialRadius;
            planeForceJumpTime = planeStartTime + ( distToZoneExit / speed ) * 1000f;

            // Build plane data for network event
            var planeDataList = new List<object>();
            foreach( var plane in planes ) {
                planeDataList.Add( new {
                    sx = plane.StartX, sy = plane.StartY, sz = plane.StartZ,
                    ex = plane.EndX, ey = plane.EndY, ez = plane.EndZ,
                    speed = plane.Speed,
                    players = plane.AssignedPlayers.ToArray()
                } );
            }

            // Send plane data as individual values since FiveM serialization
            // works better with flat data
            foreach( var plane in planes ) {
                foreach( var playerHandle in plane.AssignedPlayers ) {
                    var ply = GetPlayer( playerHandle );
                    if( ply != null ) {
                        // Protect players from false deaths during plane transition
                        SpawnProtectedPlayers.Add( ply.Handle );
                        ply.TriggerEvent( "br:spawnPlane",
                            plane.StartX, plane.StartY, plane.StartZ,
                            plane.EndX, plane.EndY, plane.EndZ,
                            plane.Speed );
                    }
                }
            }
        }

        void OnDebugSpawnWeapon( [FromSource] Player player, float x, float y, float z ) {
            var game = ServerGlobals.CurrentGame as Main;
            if( game == null ) return;
            uint hash = PistolWeapons[game.rand.Next( PistolWeapons.Length )];
            Debug.WriteLine( "[BR-Server] Debug SpawnWeapon hash=" + hash + " at " + x + "," + y + "," + z );
            game.SpawnWeapon( new Vector3( x, y, z ), hash );
        }

        void OnPlayerJumped( [FromSource] Player player ) {
            var game = ServerGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( !game.jumpedPlayers.Contains( player.Handle ) ) {
                game.jumpedPlayers.Add( player.Handle );
                game.SpawnProtectedPlayers.Remove( player.Handle );
                // Give start weapons after a short delay for landing
                player.TriggerEvent( "br:giveStartWeapons" );
            }
        }

        void OnDropWeapon( [FromSource] Player player, string hashStr, float x, float y, float z, int ammo, int clipAmmo ) {
            var game = ServerGlobals.CurrentGame as Main;
            if( game == null ) return;
            if( !game.gameStarted ) return;
            // Broadcast to all clients so everyone sees the dropped weapon
            TriggerClientEvent( "br:weaponDropped", hashStr, x, y, z, ammo, clipAmmo );
        }

        // ===================== CONTAINER SEARCH =====================

        void OnSearchContainer( [FromSource] Player player, float x, float y, float z ) {
            // Event handlers only fire on the template instance (registered at resource start).
            // Forward to the active game instance.
            var game = ServerGlobals.CurrentGame as Main;
            if( game == null ) return;
            game.HandleSearchContainer( player, x, y, z );
        }

        void HandleSearchContainer( Player player, float x, float y, float z ) {
            Debug.WriteLine( "[BR-Server] HandleSearchContainer from " + player.Name + " gameStarted=" + gameStarted + " instance=" + GetHashCode() );

            if( !gameStarted ) {
                Debug.WriteLine( "[BR-Server] REJECTED: gameStarted is false!" );
                return;
            }

            string posKey = ((int)Math.Round( x )) + "," + ((int)Math.Round( y )) + "," + ((int)Math.Round( z ));
            Debug.WriteLine( "[BR-Server] posKey=" + posKey + " alreadySearched=" + searchedContainers.Contains( posKey ) );

            if( searchedContainers.Contains( posKey ) ) {
                Debug.WriteLine( "[BR-Server] Container already searched, sending empty" );
                player.TriggerEvent( "br:containerEmpty" );
                return;
            }

            searchedContainers.Add( posKey );
            TriggerClientEvent( "br:containerSearched", x, y, z );

            double roll = rand.NextDouble();
            if( roll <= 0.50 ) {
                // 50% weapon — give directly to player for auto-equip
                uint weaponHash = PickRandomContainerWeapon();
                player.TriggerEvent( "br:giveWeapon", weaponHash.ToString() );
            } else if( roll <= 0.75 ) {
                // 25% consumable
                string type = rand.NextDouble() < 0.6 ? "bandage" : "adrenaline";
                player.TriggerEvent( "br:giveConsumable", type );
            } else {
                // 25% empty
                player.TriggerEvent( "br:containerEmpty" );
            }
        }

        uint PickRandomContainerWeapon() {
            // Progressive loot: early phases = pistols only, later phases unlock better weapons
            // Phase -1,0,1: Pistols + Melee
            // Phase 2: + SMGs
            // Phase 3: + Shotguns, Rifles
            // Phase 4: + Snipers (everything)
            int phase = currentPhase;

            if( phase <= 1 ) {
                // Pistols only (with small melee chance)
                int roll = rand.Next( 100 );
                if( roll < 85 ) return PistolWeapons[rand.Next( PistolWeapons.Length )];
                return MeleeWeapons[rand.Next( MeleeWeapons.Length )];
            }

            if( phase == 2 ) {
                // Pistols + SMGs
                int roll = rand.Next( 100 );
                if( roll < 40 ) return PistolWeapons[rand.Next( PistolWeapons.Length )];
                if( roll < 85 ) return SMGWeapons[rand.Next( SMGWeapons.Length )];
                return MeleeWeapons[rand.Next( MeleeWeapons.Length )];
            }

            if( phase == 3 ) {
                // Pistols + SMGs + Shotguns + Rifles
                int roll = rand.Next( 100 );
                if( roll < 15 ) return PistolWeapons[rand.Next( PistolWeapons.Length )];
                if( roll < 40 ) return SMGWeapons[rand.Next( SMGWeapons.Length )];
                if( roll < 60 ) return ShotgunWeapons[rand.Next( ShotgunWeapons.Length )];
                if( roll < 90 ) return RifleWeapons[rand.Next( RifleWeapons.Length )];
                return MeleeWeapons[rand.Next( MeleeWeapons.Length )];
            }

            // Phase 4+: Everything including snipers
            int finalRoll = rand.Next( 100 );
            if( finalRoll < 10 ) return PistolWeapons[rand.Next( PistolWeapons.Length )];
            if( finalRoll < 25 ) return SMGWeapons[rand.Next( SMGWeapons.Length )];
            if( finalRoll < 45 ) return ShotgunWeapons[rand.Next( ShotgunWeapons.Length )];
            if( finalRoll < 75 ) return RifleWeapons[rand.Next( RifleWeapons.Length )];
            if( finalRoll < 90 ) return SniperWeapons[rand.Next( SniperWeapons.Length )];
            return MeleeWeapons[rand.Next( MeleeWeapons.Length )];
        }

        public override void Update() {
            base.Update();

            if( !gameStarted ) return;

            float now = GetGameTimer();

            // Force-jump remaining players when plane is about to leave the zone
            if( planesLaunched && !forceJumpTriggered && now >= planeForceJumpTime ) {
                forceJumpTriggered = true;
                foreach( var player in alivePlayers ) {
                    if( !jumpedPlayers.Contains( player.Handle ) ) {
                        jumpedPlayers.Add( player.Handle );
                        SpawnProtectedPlayers.Remove( player.Handle );
                        player.TriggerEvent( "br:forceJump" );
                        player.TriggerEvent( "br:giveStartWeapons" );
                    }
                }
            }

            // Clean up plane tracking when flight is over
            if( planesLaunched && now >= planeStartTime + planeDuration ) {
                planesLaunched = false;
            }

            // Zone phase logic
            if( currentPhase < Phases.Length - 1 ) {
                if( !isShrinking && now >= phaseWaitUntil ) {
                    // Start next phase shrink
                    currentPhase++;
                    var phase = Phases[currentPhase];

                    if( hasNextZone ) {
                        // Use pre-calculated next zone values
                        zoneCenterX = nextZoneCX;
                        zoneCenterY = nextZoneCY;
                        zoneTargetRadius = nextZoneRadius;
                        hasNextZone = false;
                    } else {
                        zoneTargetRadius = initialRadius * phase.RadiusPercent;

                        // Shift center randomly within current circle
                        float maxShift = zoneCurrentRadius - zoneTargetRadius;
                        if( maxShift > 0 ) {
                            float angle = (float)( rand.NextDouble() * Math.PI * 2 );
                            float shift = (float)( rand.NextDouble() * maxShift * 0.5f );
                            float newCX = zoneCenterX + shift * (float)Math.Cos( angle );
                            float newCY = zoneCenterY + shift * (float)Math.Sin( angle );

                            // Clamp to map bounds
                            float halfX = Map.Size.X / 2f;
                            float halfY = Map.Size.Y / 2f;
                            newCX = Math.Max( Map.Position.X - halfX + zoneTargetRadius,
                                     Math.Min( Map.Position.X + halfX - zoneTargetRadius, newCX ) );
                            newCY = Math.Max( Map.Position.Y - halfY + zoneTargetRadius,
                                     Math.Min( Map.Position.Y + halfY - zoneTargetRadius, newCY ) );

                            zoneCenterX = newCX;
                            zoneCenterY = newCY;
                        }
                    }

                    zoneShrinkStart = now;
                    zoneShrinkEnd = now + phase.ShrinkSeconds * 1000f;
                    isShrinking = true;

                    BroadcastZoneUpdate();
                    TriggerClientEvent( "salty:popup", "Zone shrinking - Phase " + ( currentPhase + 1 ), 200, 60, 40, phase.ShrinkSeconds * 1000f );
                }

                // Periodic broadcast during wait phase so countdown stays synced
                if( !isShrinking && now >= lastZoneBroadcast + ZONE_BROADCAST_INTERVAL ) {
                    lastZoneBroadcast = now;
                    BroadcastZoneUpdate();
                }

                if( isShrinking ) {
                    float elapsed = now - zoneShrinkStart;
                    float duration = zoneShrinkEnd - zoneShrinkStart;
                    float t = Math.Min( 1f, elapsed / duration );
                    float startRadius = currentPhase == 0 ? initialRadius :
                        initialRadius * Phases[currentPhase - 1].RadiusPercent;
                    zoneCurrentRadius = startRadius + ( zoneTargetRadius - startRadius ) * t;

                    // Periodic broadcast during shrinking so clients stay synced
                    if( now >= lastZoneBroadcast + ZONE_BROADCAST_INTERVAL ) {
                        lastZoneBroadcast = now;
                        BroadcastZoneUpdate();
                    }

                    if( t >= 1f ) {
                        isShrinking = false;
                        zoneCurrentRadius = zoneTargetRadius;
                        phaseWaitUntil = now + ( currentPhase + 1 < Phases.Length ?
                            Phases[currentPhase + 1].WaitSeconds * 1000f : 999999f );
                        float nextWait = currentPhase + 1 < Phases.Length ? Phases[currentPhase + 1].WaitSeconds * 1000f : 10000f;
                        TriggerClientEvent( "salty:popup", "Zone stable - Next shrink incoming", 230, 160, 30, Math.Min( nextWait, 10000f ) );

                        // Pre-calculate next zone position
                        int nextIdx = currentPhase + 1;
                        if( nextIdx < Phases.Length ) {
                            nextZoneRadius = initialRadius * Phases[nextIdx].RadiusPercent;
                            float nMaxShift = zoneCurrentRadius - nextZoneRadius;
                            if( nMaxShift > 0 ) {
                                float nAngle = (float)( rand.NextDouble() * Math.PI * 2 );
                                float nShift = (float)( rand.NextDouble() * nMaxShift * 0.5f );
                                nextZoneCX = zoneCenterX + nShift * (float)Math.Cos( nAngle );
                                nextZoneCY = zoneCenterY + nShift * (float)Math.Sin( nAngle );

                                float halfX = Map.Size.X / 2f;
                                float halfY = Map.Size.Y / 2f;
                                nextZoneCX = Math.Max( Map.Position.X - halfX + nextZoneRadius,
                                             Math.Min( Map.Position.X + halfX - nextZoneRadius, nextZoneCX ) );
                                nextZoneCY = Math.Max( Map.Position.Y - halfY + nextZoneRadius,
                                             Math.Min( Map.Position.Y + halfY - nextZoneRadius, nextZoneCY ) );
                            } else {
                                nextZoneCX = zoneCenterX;
                                nextZoneCY = zoneCenterY;
                            }
                            hasNextZone = true;
                        } else {
                            hasNextZone = false;
                        }

                        BroadcastZoneUpdate();
                    }
                }
            }

            // Zone damage check (every 1s)
            if( now >= zoneDamageTimer + ZONE_DAMAGE_INTERVAL ) {
                zoneDamageTimer = now;
                int phaseIdx = Math.Max( 0, Math.Min( currentPhase, ZONE_DAMAGE_PER_PHASE.Length - 1 ) );
                int damage = currentPhase >= 0 ? ZONE_DAMAGE_PER_PHASE[phaseIdx] : ZONE_DAMAGE_PER_PHASE[0];
                foreach( var player in alivePlayers.ToList() ) {
                    try {
                        Vector3 playerPos = player.Character.Position;
                        float dx = playerPos.X - zoneCenterX;
                        float dy = playerPos.Y - zoneCenterY;
                        float distSq = dx * dx + dy * dy;
                        if( distSq > zoneCurrentRadius * zoneCurrentRadius ) {
                            player.TriggerEvent( "br:zoneDamage", damage );
                        }
                    } catch {
                        // Player may have disconnected
                    }
                }
            }
        }

        public override void OnPlayerKilled( Player victim, Player attacker, Vector3 deathCoords, uint weaponHash ) {
            if( !gameStarted ) return;

            alivePlayers.RemoveAll( p => p.Handle == victim.Handle );
            SetTeam( victim, -1 ); // Spectator

            string killerName = attacker != null ? attacker.Name : "The Zone";
            string victimName = victim.Name;

            // Award kill XP
            if( attacker != null && attacker.Handle != victim.Handle ) {
                AddScore( attacker, 1 );
                WriteChat( "BR", victimName + " was eliminated by " + killerName + "!", 255, 80, 80 );
            } else {
                WriteChat( "BR", victimName + " was eliminated!", 255, 80, 80 );
            }

            TriggerClientEvent( "br:playerEliminated", alivePlayers.Count, victimName, killerName );

            int placement = alivePlayers.Count + 1;
            victim.TriggerEvent( "br:yourPlacement", placement, totalStartPlayers );

            CheckWinCondition();
        }

        public override void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {
            if( !gameStarted ) return;

            alivePlayers.RemoveAll( p => p.Handle == victim.Handle );
            SetTeam( victim, -1 );

            WriteChat( "BR", victim.Name + " was eliminated!", 255, 80, 80 );
            TriggerClientEvent( "br:playerEliminated", alivePlayers.Count, victim.Name, "The Zone" );

            int placement = alivePlayers.Count + 1;
            victim.TriggerEvent( "br:yourPlacement", placement, totalStartPlayers );

            CheckWinCondition();
        }

        void CheckWinCondition() {
            Debug.WriteLine( "[BR-Server] CheckWinCondition alivePlayers=" + alivePlayers.Count + " gameStarted=" + gameStarted + " instance=" + GetHashCode() );
            if( alivePlayers.Count <= 1 ) {
                Debug.WriteLine( "[BR-Server] GAME ENDING via CheckWinCondition! alivePlayers=" + alivePlayers.Count );
                if( alivePlayers.Count == 1 ) {
                    Player winner = alivePlayers[0];
                    WinningPlayers.Add( winner );
                    WriteChat( "BR", winner.Name + " wins the Battle Royale!", 255, 215, 0 );
                    TriggerClientEvent( "br:winner", winner.Name );
                    winner.TriggerEvent( "br:yourPlacement", 1, totalStartPlayers );
                } else {
                    WriteChat( "BR", "No survivors!", 255, 80, 80 );
                }
                gameStarted = false;
                End();
            }
        }

        public override void OnTimerEnd() {
            Debug.WriteLine( "[BR-Server] OnTimerEnd called. gameStarted=" + gameStarted );
            if( !gameStarted ) return;

            // Player closest to zone center wins
            Player closest = null;
            float closestDist = float.MaxValue;
            foreach( var player in alivePlayers ) {
                try {
                    Vector3 pos = player.Character.Position;
                    float dx = pos.X - zoneCenterX;
                    float dy = pos.Y - zoneCenterY;
                    float dist = dx * dx + dy * dy;
                    if( dist < closestDist ) {
                        closestDist = dist;
                        closest = player;
                    }
                } catch { }
            }

            if( closest != null ) {
                WinningPlayers.Add( closest );
                WriteChat( "BR", closest.Name + " wins by zone position!", 255, 215, 0 );
                TriggerClientEvent( "br:winner", closest.Name );
            }

            gameStarted = false;
            End();
        }

        void BroadcastZoneUpdate() {
            // Send remaining duration rather than absolute server timestamps
            // Client converts these to its own clock in OnZoneUpdate
            float now = GetGameTimer();
            float remainingStart = 0;
            float remainingEnd = 0;
            if( isShrinking && zoneShrinkEnd > now ) {
                remainingStart = 0;
                remainingEnd = zoneShrinkEnd - now;
            }
            float waitRemaining = 0;
            if( !isShrinking && phaseWaitUntil > now ) {
                waitRemaining = phaseWaitUntil - now;
            }
            float nCX = hasNextZone ? nextZoneCX : 0;
            float nCY = hasNextZone ? nextZoneCY : 0;
            float nR = hasNextZone ? nextZoneRadius : 0;
            TriggerClientEvent( "br:zoneUpdate",
                zoneCenterX, zoneCenterY,
                zoneCurrentRadius, zoneTargetRadius,
                remainingStart, remainingEnd,
                currentPhase, waitRemaining,
                nCX, nCY, nR );
        }

        void BroadcastAliveCount() {
            TriggerClientEvent( "br:playerEliminated", alivePlayers.Count, "", "" );
        }
    }
}
