﻿using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.Native;
using GamemodeCityShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

namespace GamemodeCityClient {
    public class ClientGlobals : BaseScript {

        public static Dictionary<string, BaseGamemode> Gamemodes = new Dictionary<string, BaseGamemode>();

        public static bool isNoclip = false;

        public static Dictionary<int, ClientMap> Maps = new Dictionary<int, ClientMap>();

        public static ClientMap LastSelectedMap;

        public static BaseGamemode CurrentGame;

        public static Vector3 LastSpawn;


        public static void Init() {

        }

        public static bool BuyItem( int cost ) {
            if( Globals.GameCoins >= cost ) {
                Globals.GameCoins -= cost;
                BaseGamemode.WriteChat( "Store", "Item bought.", 20, 200, 20 );
                return true;
            }
            else {
                BaseGamemode.WriteChat( "Store", "Out of coins.", 200, 20, 20 );
                return false;
            }
        }



        public static void SetSpectator( bool spectate ) {
            if( !spectate )
                SetNoClip( false );        
            BaseGamemode.Team = spectate ? -1 : 0;

        }

        public static void SendMap( ClientMap map ) {

            string gamemode = "";
            if( map.Gamemodes != null ) {
                gamemode = string.Join( ",", map.Gamemodes );
            }

            var spawns = map.SpawnsAsSendable();

            TriggerServerEvent( "saltyMap:netUpdate", new Dictionary<string, dynamic> {
                { "id", map.ID },
                { "name", map.Name },
                { "gamemode", gamemode },
                { "position", map.Position },
                { "size", map.Size },
                { "spawns", spawns },
                { "create", map.JustCreated }
            } );
        }


        public static void SetNoClip( bool toggle ) {
            isNoclip = toggle;
            Game.Player.Character.Opacity = toggle ? 0 : 255;
            SetEntityVisible( PlayerPedId(), !isNoclip, false );
            SetEntityCollision( PlayerPedId(), !isNoclip, !isNoclip );
            SetEntityInvincible( PlayerPedId(), isNoclip );
            SetEveryoneIgnorePlayer( PlayerPedId(), isNoclip );
        }

        public static void SendNUIMessage( string name, string message ) {
            API.SendNuiMessage( "{\"type\":\"salty\",\"name\":\"" + name + "\",\"data\":\"" + message + "\"}" );
        }

        public static List<Player> GetInGamePlayers() {
            return new PlayerList().Where( x => !x.IsInvincible ).ToList();
        }

        public static void NoClipUpdate() {

            Vector3 heading = GetGameplayCamRot( 0 );
            SetEntityRotation( PlayerPedId(), heading.X, heading.Y, -heading.Z, 0, true );
            SetEntityHeading( PlayerPedId(), heading.Z );

            int speed = 1;

            if( IsControlPressed( 0, 21 ) ) {
                speed *= 6;
            }

            Vector3 offset = new Vector3( 0, 0, 0 );

            if( IsControlPressed( 0, 36 ) ) {
                offset.Z = -speed;
            }

            if( IsControlPressed( 0, 22 ) ) {
                offset.Z = speed;
            }

            if( IsControlPressed( 0, 33 ) ) {
                offset.Y = -speed;
            }

            if( IsControlPressed( 0, 32 ) ) {
                offset.Y = speed;
            }

            if( IsControlPressed( 0, 35 ) ) {
                offset.X = speed;
            }

            if( IsControlPressed( 0, 34 ) ) {
                offset.X = -speed;
            }

            var noclipPos = GetOffsetFromEntityInWorldCoords( PlayerPedId(), offset.X, offset.Y, offset.Z );
            SetEntityCoordsNoOffset( PlayerPedId(), noclipPos.X, noclipPos.Y, noclipPos.Z, false, false, false );

        }

        public static void FirstPersonForAlive() {
            DisableControlAction( 0, 0, true );
            if( GetFollowPedCamViewMode() != 4 )
                SetFollowPedCamViewMode( 4 );
        }

    }
 
}
