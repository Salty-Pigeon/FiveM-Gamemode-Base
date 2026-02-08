using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamemodeCityShared;

namespace GamemodeCityClient {
    public class Main : BaseScript {

        MapVote MapVote;

        public Main() {

            EventHandlers["onClientResourceStart"] += new Action<string>( OnClientResourceStart );
            EventHandlers["playerSpawned"] += new Action<object>( PlayerSpawn );

            EventHandlers["salty:StartGame"] += new Action<string, float, object, Vector3, Vector3, float>( StartGame );
            EventHandlers["salty:EndGame"] += new Action( EndGame );
            EventHandlers["salty:CacheMap"] += new Action<string>( CacheMap );
            EventHandlers["salty:OpenMapGUI"] += new Action( OpenMapGUI );
            EventHandlers["salty:Spawn"] += new Action<int, Vector3, uint, float>( Spawn );
            EventHandlers["salty:SetTeam"] += new Action<int>( SetTeam );
            EventHandlers["salty:MapVote"] += new Action<IDictionary<string, object>>( VoteMap );
            EventHandlers["salty:GameVote"] += new Action<IDictionary<string, object>, float>( VoteGame );
            EventHandlers["salty:VoteUpdate"] += new Action<string>( VoteUpdate );
            EventHandlers["salty:VoteEnd"] += new Action<string>( VoteEnd );
            EventHandlers["salty:UpdateTime"] += new Action<float>( UpdateTime );
            EventHandlers["salty:updatePlayerDetail"] += new Action<int, string, object>( UpdateDetail );

            base.Tick += Tick;
        }

        private void UpdateDetail( int ply, string key, object data ) {
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.SetPlayerDetail( ply, key, data );
            }
        }

        void UpdateTime( float time ) {
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.GameTimerEnd = time;
            }
        }

        void OpenMapGUI() {
            if( HubNUI.IsOpen ) {
                // Hub already open — just refresh the maps list, keep any unsaved -1 map
                HubNUI.SendMapsUpdate();
            } else {
                // Fresh open — clean up old -1 entries (unsaved maps that now have real IDs)
                if( ClientGlobals.Maps.ContainsKey( -1 ) ) {
                    ClientGlobals.Maps.Remove( -1 );
                }
                HubNUI.OpenHub( "maps" );
            }
        }

        void CacheMap( string mapJson ) {
            try {
                MapData data = SimpleJson.Deserialize( mapJson );
                if( data == null ) return;

                ClientMap map = ClientMap.FromMapData( data );
                ClientGlobals.Maps[map.ID] = map;
            } catch( Exception ex ) {
                Debug.WriteLine( "[GamemodeCity] Error caching map: " + ex.Message );
            }
        }

        private void VoteMap( IDictionary<string, object> maps ) {
            Dictionary<int, string> Maps = new Dictionary<int, string>();
            foreach( var kvp in maps ) {
                Maps[Convert.ToInt32( kvp.Key )] = kvp.Value.ToString();
            }
            MapVote = new MapVote( Maps );
            MapVote.VoteMenu.OpenMenu();
        }

        private void VoteGame( IDictionary<string, object> gamemodes, float durationSeconds ) {
            if( HubNUI.IsOpen ) {
                HubNUI.CloseHub();
            }
            VoteNUI.OpenVote( durationSeconds );
        }

        private void VoteUpdate( string votesJson ) {
            VoteNUI.UpdateVotes( votesJson );
        }

        private void VoteEnd( string winnerId ) {
            VoteNUI.ShowWinner( winnerId );
        }

        private void PlayerSpawn( object spawnInfo ) {
            if( BaseGamemode.Team == -1 ) {
                ClientGlobals.SetNoClip( true );
            } else if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.PlayerSpawn();
            } else {
                ClientGlobals.SetSpectator( true );
                ClientGlobals.SetNoClip( true );
            }
        }

        private void SetTeam( int team ) {
            if( ClientGlobals.CurrentGame != null )
                ClientGlobals.CurrentGame.SetTeam( team );
        }

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            ClientGlobals.Init();

            TriggerServerEvent( "salty:requestProgression" );

            RegisterCommand( "icm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "icm" );
            } ), false );

            RegisterCommand( "ttt", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "ttt" );
            } ), false );

            // Solo TTT mode for testing alone
            RegisterCommand( "solottt", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartSoloTTT" );
            } ), false );

            // Solo ICM mode for testing alone
            RegisterCommand( "soloicm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartSoloICM" );
            } ), false );

            // End current TTT game (useful for solo testing)
            RegisterCommand( "endttt", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netEndGame" );
            } ), false );

            RegisterCommand( "mvb", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "mvb" );
            } ), false );

            RegisterCommand( "hp", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "hp" );
            } ), false );

            RegisterCommand( "tdm", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netStartGame", "tdm" );
            } ), false );

            RegisterCommand( "noclip", new Action<int, List<object>, string>( ( source, args, raw ) => {
                ClientGlobals.SetNoClip( !ClientGlobals.isNoclip );
            } ), false );

            RegisterCommand( "maps", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netOpenMapGUI" );
            } ), false );

            RegisterCommand( "kill", new Action<int, List<object>, string>( ( source, args, raw ) => {
                LocalPlayer.Character.Kill();
            } ), false );

            RegisterCommand( "vote", new Action<int, List<object>, string>( ( source, args, raw ) => {
                TriggerServerEvent( "salty:netBeginGameVote" );
            } ), false );

            // Map editing chat commands
            RegisterCommand( "mapname", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( !ClientGlobals.IsEditingMap || ClientGlobals.LastSelectedMap == null ) {
                    BaseGamemode.WriteChat( "Maps", "You must have a map open in the editor first.", 200, 30, 30 );
                    return;
                }
                if( args.Count == 0 ) {
                    BaseGamemode.WriteChat( "Maps", "Usage: /mapname <name>", 200, 200, 30 );
                    return;
                }
                string newName = string.Join( " ", args );
                ClientGlobals.LastSelectedMap.Name = newName;
                BaseGamemode.WriteChat( "Maps", "Map name set to: " + newName, 30, 200, 30 );
            } ), false );

            RegisterCommand( "mapauthor", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( !ClientGlobals.IsEditingMap || ClientGlobals.LastSelectedMap == null ) {
                    BaseGamemode.WriteChat( "Maps", "You must have a map open in the editor first.", 200, 30, 30 );
                    return;
                }
                if( args.Count == 0 ) {
                    BaseGamemode.WriteChat( "Maps", "Usage: /mapauthor <name>", 200, 200, 30 );
                    return;
                }
                string author = string.Join( " ", args );
                ClientGlobals.LastSelectedMap.Author = author;
                BaseGamemode.WriteChat( "Maps", "Map author set to: " + author, 30, 200, 30 );
            } ), false );

            RegisterCommand( "mapdesc", new Action<int, List<object>, string>( ( source, args, raw ) => {
                if( !ClientGlobals.IsEditingMap || ClientGlobals.LastSelectedMap == null ) {
                    BaseGamemode.WriteChat( "Maps", "You must have a map open in the editor first.", 200, 30, 30 );
                    return;
                }
                if( args.Count == 0 ) {
                    BaseGamemode.WriteChat( "Maps", "Usage: /mapdesc <description>", 200, 200, 30 );
                    return;
                }
                string desc = string.Join( " ", args );
                ClientGlobals.LastSelectedMap.Description = desc;
                BaseGamemode.WriteChat( "Maps", "Map description set to: " + desc, 30, 200, 30 );
            } ), false );
        }

        public void StartGame( string ID, float gameLength, object gameWeps, Vector3 mapPos, Vector3 mapSize, float mapRotation ) {
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.Map.ClearObjects();
            }

            ClientGlobals.CurrentGame = (BaseGamemode)Activator.CreateInstance( ClientGlobals.Gamemodes[ID.ToLower()].GetType() );
            ClientGlobals.CurrentGame.Map = new ClientMap( -1, ID, new List<string>(), mapPos, mapSize, false );
            ClientGlobals.CurrentGame.Map.Rotation = mapRotation;

            // Convert weapons from network format to List<uint>
            if( gameWeps is IList<object> wepList ) {
                foreach( var wep in wepList ) {
                    uint wepHash = Convert.ToUInt32( wep );
                    if( !ClientGlobals.CurrentGame.GameWeapons.Contains( wepHash ) )
                        ClientGlobals.CurrentGame.GameWeapons.Add( wepHash );
                }
            }

            ClientGlobals.CurrentGame.Start( gameLength );
        }

        public void EndGame() {
            if( ClientGlobals.CurrentGame != null ) {
                ClientGlobals.CurrentGame.End();
                ClientGlobals.CurrentGame = null;
                ClientGlobals.SetSpectator( true );
                ClientGlobals.SetNoClip( true );
            }
        }

        private async Task Tick() {
            if( ClientGlobals.CurrentGame != null )
                ClientGlobals.CurrentGame.Update();
            if( ClientGlobals.isNoclip )
                ClientGlobals.NoClipUpdate();
        }

        public async void Spawn( int typ, Vector3 spawn, uint hash, float heading ) {
            ClientGlobals.LastSpawn = spawn;
            ClientGlobals.LastSpawnHeading = heading;
            SpawnType type = (SpawnType)typ;
            if( type == SpawnType.PLAYER ) {
                // Freeze player while we load the area
                FreezeEntityPosition( PlayerPedId(), true );

                // Request collision and load the area around the spawn point
                RequestCollisionAtCoord( spawn.X, spawn.Y, spawn.Z );
                NewLoadSceneStart( spawn.X, spawn.Y, spawn.Z, spawn.X, spawn.Y, spawn.Z, 50f, 0 );

                // Wait for collision to load (up to 2 seconds)
                int timeout = 0;
                while( !HasCollisionLoadedAroundEntity( PlayerPedId() ) && timeout < 20 ) {
                    await Delay( 100 );
                    timeout++;
                }

                // Apply selected character model before teleporting
                string selectedModel = PlayerProgression.GetSelectedModel();
                if( !string.IsNullOrEmpty( selectedModel ) ) {
                    uint modelHash = (uint)GetHashKey( selectedModel );
                    RequestModel( modelHash );
                    int modelTimeout = 0;
                    while( !HasModelLoaded( modelHash ) && modelTimeout < 50 ) {
                        await Delay( 50 );
                        modelTimeout++;
                    }
                    if( HasModelLoaded( modelHash ) ) {
                        SetPlayerModel( PlayerId(), modelHash );
                        SetModelAsNoLongerNeeded( modelHash );
                        // Apply full appearance (clothing, face, hair, etc.)
                        PlayerProgression.ApplyFullAppearance( PlayerPedId(), PlayerProgression.AppearanceJson );
                    }
                }

                // Teleport the player
                SetEntityCoordsNoOffset( PlayerPedId(), spawn.X, spawn.Y, spawn.Z, false, false, false );
                SetEntityHeading( PlayerPedId(), heading );
                NewLoadSceneStop();

                // Small delay then unfreeze (unless a countdown is handling the unfreeze)
                await Delay( 100 );
                if( ClientGlobals.CurrentGame == null || !ClientGlobals.CurrentGame.CountdownActive )
                    FreezeEntityPosition( PlayerPedId(), false );

                PlayerSpawn( null );
            } else if( type == SpawnType.WIN_BARRIER ) {
                if( ClientGlobals.CurrentGame != null )
                    ClientGlobals.CurrentGame.WinBarriers.Add( new WinBarrierData {
                        Position = spawn,
                        SizeX = (hash >> 16) / 10f,
                        SizeY = (hash & 0xFFFF) / 10f,
                        Rotation = heading
                    } );
            } else if( type == SpawnType.WEAPON ) {
                if( ClientGlobals.CurrentGame != null ) {
                    if( ClientGlobals.CurrentGame.Map == null ) {
                        Debug.WriteLine( "[TTT] Map null when spawning weapon" );
                    } else {
                        // Pre-load the weapon model before creating the entity
                        if( Globals.Weapons.ContainsKey( hash ) ) {
                            string model = Globals.Weapons[hash]["ModelHashKey"];
                            if( !string.IsNullOrEmpty( model ) ) {
                                uint modelHash = (uint)GetHashKey( model );
                                RequestModel( modelHash );
                                int wepTimeout = 0;
                                while( !HasModelLoaded( modelHash ) && wepTimeout < 50 ) {
                                    await Delay( 50 );
                                    wepTimeout++;
                                }
                            }
                        }
                        SaltyWeapon wep = new SaltyWeapon( SpawnType.WEAPON, hash, spawn );
                        ClientGlobals.CurrentGame.Map.Weapons.Add( wep );
                    }
                } else {
                    Debug.WriteLine( "[TTT] Game null when spawning weapon" );
                }
            }
        }
    }
}
