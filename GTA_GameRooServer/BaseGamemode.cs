using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA_GameRooShared;

namespace GTA_GameRooServer {

    public class BaseGamemode : BaseScript, IDisposable {

        public string Gamemode;

        public ServerMap Map;

        public Settings Settings = new Settings();

        public Dictionary<int, float> TeamScores = new Dictionary<int, float>();

        public float GameTime = 0;

        public Dictionary<Player, Dictionary<string, object>> PlayerDetails = new Dictionary<Player, Dictionary<string, object>>();

        public List<Player> Spectators = new List<Player>();

        /// <summary>
        /// Set this before calling End() to award win XP to these players.
        /// Players not in this list get participation XP only.
        /// </summary>
        public List<Player> WinningPlayers = new List<Player>();

        public const int XP_WIN = 50;
        public const int XP_PARTICIPATE = 15;

        public bool PreGame = true;
        private float PreGameTime = 0;

        public BaseGamemode( string gamemode ) {
            Globals.GameCoins = 0;
            Gamemode = gamemode.ToLower();
            if (!ServerGlobals.Gamemodes.ContainsKey(Gamemode))
                ServerGlobals.Gamemodes.Add(Gamemode, this);

        }

        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose( bool disposing ) {
            if( disposed )
                return;

            if( disposing ) {
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        ~BaseGamemode() {
            Dispose( false );
        }

        public virtual void Start( ) {

            TriggerClientEvent( "salty:StartGame", Gamemode, Settings.GameLength, Settings.Weapons, Map.Position, Map.Size, Map.Rotation );

            foreach( var barrier in Map.GetWinBarriers() ) {
                uint packedSize = (uint)(((int)(barrier.SizeX * 10f)) << 16 | ((int)(barrier.SizeY * 10f)));
                TriggerClientEvent( "salty:Spawn", (int)SpawnType.WIN_BARRIER, barrier.Position, packedSize, barrier.Heading );
            }
        }

        public virtual void OnTimerEnd() {
            End();
        }

        public virtual void Update() {
            if( PreGame && PreGameTime > 0 ) {
                if( PreGameTime < GetGameTimer() ) {
                    PreGame = false;
                    Main.StartGame( ServerGlobals.CurrentGame.Gamemode );
                }
            }
        }

        public virtual void End() {
            // Award XP to all participants and winners
            foreach( var player in new PlayerList() ) {
                if( WinningPlayers.Contains( player ) ) {
                    PlayerProgression.AwardXP( player, XP_WIN );
                } else {
                    PlayerProgression.AwardXP( player, XP_PARTICIPATE );
                }
            }

            TriggerClientEvent( "salty:EndGame" );
            ServerGlobals.CurrentRound++;
            if( ServerGlobals.CurrentRound < Settings.Rounds ) {
                WriteChat( "GameRoo", "Next round starting in " + Math.Round( Settings.PreGameTime / 1000 ), 200, 200, 20 );
                PreGameTime = GetGameTimer() + Settings.PreGameTime;
                PreGame = true;
            }
            else {
                ServerGlobals.CurrentRound = 0;
                ServerGlobals.CurrentGame = null;
                Main.ScheduleGameVote( 10000 );
            }
            Dispose();
        }

        public Player GetPlayer( string src ) {
            return new PlayerList().Where( x => x.Handle == src ).First();
        }

        public virtual void OnPlayerKilled( Player victim, Player attacker, Vector3 deathcords , uint weaponHash) {

        }

        public virtual void OnPlayerDied( Player victim, int killerType, Vector3 deathCoords ) {

        }

        public static void WriteChat( string prefix, string str, int r, int g, int b ) {
            TriggerClientEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }



        public static void WriteChat( Player ply, string prefix, string str, int r, int g, int b ) {
            ply.TriggerEvent( "chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            } );
        }

        public void SpawnPlayer( Player player, int team ) {
            var spawn = Map.GetSpawn( SpawnType.PLAYER, team );
            player.TriggerEvent( "salty:Spawn", (int)SpawnType.PLAYER, spawn.Position, (uint)0, spawn.Heading );
        }

        public void SpawnPlayers() {
            foreach( var ply in new PlayerList() ) {
                SpawnPlayer( ply );
            }
        }

        public void SpawnPlayers( int team ) {
            foreach( var ply in new PlayerList() ) {
                SpawnPlayer( ply, team );
            }
        }

        public void SpawnPlayer( Player player ) {
            object team = GetPlayerDetail( player, "team" );
            if( team != null ) {
                var spawn = Map.GetSpawn( SpawnType.PLAYER, Convert.ToInt32( team ) );
                player.TriggerEvent( "salty:Spawn", (int)SpawnType.PLAYER, spawn.Position, (uint)0, spawn.Heading );
            } else {
                SpawnPlayer( player, 0 );
            }
        }

        public void SpawnWeapon( Vector3 pos, uint hash ) {
            TriggerClientEvent( "salty:Spawn", (int)SpawnType.WEAPON, pos, hash, 0f );
        }

        public void AddScore( Player ply, float amount ) {
            object score = GetPlayerDetail( ply, "score" );
            if( score != null ) {
                SetPlayerDetail( ply, "score", Convert.ToSingle( score ) + amount );
            } else {
                SetPlayerDetail( ply, "score", amount );
            }
        }

        public void SetTeam( Player ply, int team ) {
            ply.TriggerEvent( "salty:SetTeam", team );
            SetPlayerDetail( ply, "team", team );
        }

        public List<Player> GetTeamPlayers(int team ) {
            List<Player> teamPlayers = new List<Player>();
            foreach( var player in new PlayerList() ) {
                object teamObj = GetPlayerDetail( player, "team" );
                if( teamObj != null && Convert.ToInt32( teamObj ) == team ) {
                    teamPlayers.Add( player );
                }
            }
            return teamPlayers;
        }


        public virtual void OnDetailUpdate( Player ply, string key, object oldValue, object newValue ) {

            TriggerClientEvent( "salty:updatePlayerDetail", Convert.ToInt32( ply.Handle ), key, newValue );
        }

        public void SetPlayerDetail( Player ply, string detail, object data ) {
            if( !PlayerDetails.ContainsKey( ply ) ) {
                PlayerDetails.Add( ply, new Dictionary<string, object>() );
            }
            if( !PlayerDetails[ply].ContainsKey(detail) ) {
                PlayerDetails[ply].Add( detail, data );
            }
            OnDetailUpdate( ply, detail, PlayerDetails[ply][detail], data );
            PlayerDetails[ply][detail] = data;
        }

        public void AddPlayerDetail( Player ply, string detail, float amount ) {
            object current = GetPlayerDetail( ply, detail );
            float currentVal = current != null ? Convert.ToSingle( current ) : 0f;
            SetPlayerDetail( ply, detail, currentVal + amount );
        }

        public object GetPlayerDetail( Player ply, string detail ) {
            if( !PlayerDetails.ContainsKey(ply) ) {
                PlayerDetails.Add( ply, new Dictionary<string, object>() );
            } else if( PlayerDetails[ply].ContainsKey( detail ) ) {
                return PlayerDetails[ply][detail];
            }
            return null;
        }

    }


}
