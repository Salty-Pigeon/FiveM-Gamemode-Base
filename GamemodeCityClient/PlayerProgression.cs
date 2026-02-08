using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GamemodeCityShared;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class PlayerProgression : BaseScript {

        public static int XP;
        public static int Level;
        public static int Tokens;
        public static List<string> UnlockedModels = new List<string>();
        public static string SelectedModel = "";
        public static string AppearanceJson = null;
        public static List<string> UnlockedItems = new List<string>();
        private static bool dataLoaded = false;

        private static int _previewCam = 0;
        private static int _previewPed = 0;
        private static bool _customizing = false;
        private static string _savedAppearance = null;
        private static string _savedModel = null;
        private static Vector3 _previewPos;

        public PlayerProgression() {
            EventHandlers["salty:receiveProgression"] += new Action<string>( OnReceiveProgression );

            RegisterNuiCallbackType( "purchaseModel" );
            EventHandlers["__cfx_nui:purchaseModel"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiPurchaseModel );

            RegisterNuiCallbackType( "selectModel" );
            EventHandlers["__cfx_nui:selectModel"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiSelectModel );

            // Customization callbacks
            RegisterNuiCallbackType( "customizeStart" );
            EventHandlers["__cfx_nui:customizeStart"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiCustomizeStart );

            RegisterNuiCallbackType( "customizeCancel" );
            EventHandlers["__cfx_nui:customizeCancel"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiCustomizeCancel );

            RegisterNuiCallbackType( "customizeSave" );
            EventHandlers["__cfx_nui:customizeSave"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiCustomizeSave );

            RegisterNuiCallbackType( "setCameraPreset" );
            EventHandlers["__cfx_nui:setCameraPreset"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiSetCameraPreset );

            RegisterNuiCallbackType( "changeModel" );
            EventHandlers["__cfx_nui:changeModel"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeModel );

            RegisterNuiCallbackType( "changeHeadBlend" );
            EventHandlers["__cfx_nui:changeHeadBlend"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeHeadBlend );

            RegisterNuiCallbackType( "changeFaceFeature" );
            EventHandlers["__cfx_nui:changeFaceFeature"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeFaceFeature );

            RegisterNuiCallbackType( "changeHeadOverlay" );
            EventHandlers["__cfx_nui:changeHeadOverlay"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeHeadOverlay );

            RegisterNuiCallbackType( "changeHair" );
            EventHandlers["__cfx_nui:changeHair"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeHair );

            RegisterNuiCallbackType( "changeEyeColor" );
            EventHandlers["__cfx_nui:changeEyeColor"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeEyeColor );

            RegisterNuiCallbackType( "changeComponent" );
            EventHandlers["__cfx_nui:changeComponent"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeComponent );

            RegisterNuiCallbackType( "changeProp" );
            EventHandlers["__cfx_nui:changeProp"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiChangeProp );

            RegisterNuiCallbackType( "purchaseItem" );
            EventHandlers["__cfx_nui:purchaseItem"] += new Action<IDictionary<string, object>, CallbackDelegate>( OnNuiPurchaseItem );
        }

        // ==================== Progression Data ====================

        private void OnReceiveProgression( string json ) {
            try {
                // Manual JSON parse (no external libs)
                var dict = SimpleJsonParser.Parse( json );
                if( dict == null ) return;

                XP = dict.ContainsKey( "xp" ) ? Convert.ToInt32( dict["xp"] ) : 0;
                Level = dict.ContainsKey( "level" ) ? Convert.ToInt32( dict["level"] ) : 1;
                Tokens = dict.ContainsKey( "tokens" ) ? Convert.ToInt32( dict["tokens"] ) : 0;
                SelectedModel = dict.ContainsKey( "selectedModel" ) ? dict["selectedModel"].ToString() : "";

                if( dict.ContainsKey( "unlockedModels" ) && dict["unlockedModels"] is List<object> modelList ) {
                    UnlockedModels = modelList.Select( m => m.ToString() ).ToList();
                } else {
                    UnlockedModels = new List<string>();
                }

                if( dict.ContainsKey( "unlockedItems" ) && dict["unlockedItems"] is List<object> itemList ) {
                    UnlockedItems = itemList.Select( i => i.ToString() ).ToList();
                } else {
                    UnlockedItems = new List<string>();
                }

                if( dict.ContainsKey( "appearanceJson" ) && dict["appearanceJson"] != null ) {
                    AppearanceJson = dict["appearanceJson"].ToString();
                }

                bool leveledUp = dict.ContainsKey( "leveledUp" ) && Convert.ToBoolean( dict["leveledUp"] );

                dataLoaded = true;

                // Push update to NUI
                SendNuiMessage( "{\"type\":\"updateProgression\"," +
                    "\"xp\":" + XP +
                    ",\"level\":" + Level +
                    ",\"tokens\":" + Tokens +
                    ",\"unlockedModels\":[" + BuildUnlockedModelsJson() + "]" +
                    ",\"selectedModel\":\"" + EscapeJson( SelectedModel ) + "\"" +
                    ",\"unlockedItems\":[" + BuildUnlockedItemsJson() + "]" +
                    ",\"leveledUp\":" + ( leveledUp ? "true" : "false" ) + "}" );
            } catch( Exception ex ) {
                Debug.WriteLine( "[GamemodeCity] Error parsing progression: " + ex.Message );
            }
        }

        private void OnNuiPurchaseModel( IDictionary<string, object> data, CallbackDelegate cb ) {
            string modelHash = data.ContainsKey( "modelHash" ) ? data["modelHash"].ToString() : "";
            if( !string.IsNullOrEmpty( modelHash ) ) {
                TriggerServerEvent( "salty:purchaseModel", modelHash );
            }
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiSelectModel( IDictionary<string, object> data, CallbackDelegate cb ) {
            string modelHash = data.ContainsKey( "modelHash" ) ? data["modelHash"].ToString() : "";
            if( !string.IsNullOrEmpty( modelHash ) ) {
                TriggerServerEvent( "salty:selectModel", modelHash );
            }
            cb( "{\"status\":\"ok\"}" );
        }

        public static string GetSelectedModel() {
            return dataLoaded ? SelectedModel : "";
        }

        public static string BuildProgressionJson() {
            return "{\"xp\":" + XP +
                ",\"level\":" + Level +
                ",\"tokens\":" + Tokens +
                ",\"unlockedModels\":[" + BuildUnlockedModelsJson() + "]" +
                ",\"selectedModel\":\"" + EscapeJson( SelectedModel ) + "\"" +
                ",\"unlockedItems\":[" + BuildUnlockedItemsJson() + "]" +
                ",\"appearanceJson\":" + ( AppearanceJson != null ? "\"" + EscapeJson( AppearanceJson ) + "\"" : "null" ) + "}";
        }

        public static string BuildPedModelsJson() {
            var entries = new List<string>();
            foreach( var model in PedModels.All ) {
                entries.Add( "{\"hash\":\"" + EscapeJson( model.Hash ) + "\",\"name\":\"" + EscapeJson( model.Name ) + "\",\"category\":\"" + EscapeJson( model.Category ) + "\"}" );
            }
            return "[" + string.Join( ",", entries ) + "]";
        }

        // ==================== Camera System ====================

        public static void SetPreviewCamera( string preset ) {
            if( _previewPed == 0 || !DoesEntityExist( _previewPed ) ) return;

            // Offsets relative to the preview ped (heading 180)
            // Negative X shifts ped to the LEFT of screen (away from the UI panel)
            float offX = -0.5f, offY = 3.5f, offZ = 0.3f;
            float ptX = -0.5f, ptY = 0f, ptZ = -0.1f;

            switch( preset ) {
                case "head":
                    offX = -0.2f; offY = 0.9f; offZ = 0.65f;
                    ptX = -0.2f; ptZ = 0.6f;
                    break;
                case "body":
                    offX = -0.3f; offY = 1.2f; offZ = 0.2f;
                    ptX = -0.3f; ptZ = 0.2f;
                    break;
                case "bottom":
                    offX = -0.25f; offY = 0.98f; offZ = -0.7f;
                    ptX = -0.25f; ptZ = -0.9f;
                    break;
            }

            Vector3 camPos = GetOffsetFromEntityInWorldCoords( _previewPed, offX, offY, offZ );
            Vector3 lookAt = GetOffsetFromEntityInWorldCoords( _previewPed, ptX, ptY, ptZ );

            if( _previewCam != 0 && DoesCamExist( _previewCam ) ) {
                SetCamCoord( _previewCam, camPos.X, camPos.Y, camPos.Z );
                PointCamAtCoord( _previewCam, lookAt.X, lookAt.Y, lookAt.Z );
            } else {
                _previewCam = CreateCam( "DEFAULT_SCRIPTED_CAMERA", true );
                SetCamCoord( _previewCam, camPos.X, camPos.Y, camPos.Z );
                PointCamAtCoord( _previewCam, lookAt.X, lookAt.Y, lookAt.Z );
                SetCamFov( _previewCam, 40f );
                SetCamActive( _previewCam, true );
                RenderScriptCams( true, false, 0, true, false );
            }
        }

        public static async void StartCustomization() {
            if( _customizing ) return;
            _customizing = true;

            int playerPed = PlayerPedId();
            _savedAppearance = GetCurrentAppearanceJson( playerPed );
            _savedModel = SelectedModel;

            // Spawn preview NPC below the map near the player
            Vector3 playerPos = GetEntityCoords( playerPed, true );
            _previewPos = new Vector3( playerPos.X, playerPos.Y, playerPos.Z - 50f );

            // Load model and create ped
            string modelName = string.IsNullOrEmpty( SelectedModel ) ? "mp_m_freemode_01" : SelectedModel;
            uint hash = (uint)GetHashKey( modelName );
            Debug.WriteLine( "[GamemodeCity] StartCustomization: model=" + modelName + " hash=" + hash + " pos=" + _previewPos );
            RequestModel( hash );
            int timeout = 0;
            while( !HasModelLoaded( hash ) && timeout < 100 ) {
                await BaseScript.Delay( 50 );
                timeout++;
            }

            if( HasModelLoaded( hash ) ) {
                _previewPed = CreatePed( 4, hash, _previewPos.X, _previewPos.Y, _previewPos.Z, 180f, false, false );
                SetModelAsNoLongerNeeded( hash );
                Debug.WriteLine( "[GamemodeCity] StartCustomization: ped=" + _previewPed + " exists=" + DoesEntityExist( _previewPed ) );

                FreezeEntityPosition( _previewPed, true );
                TaskStandStill( _previewPed, -1 );
                SetEntityInvincible( _previewPed, true );
                SetEntityCollision( _previewPed, false, false );
                SetBlockingOfNonTemporaryEvents( _previewPed, true );

                // Initialize freemode peds (matching fivem-appearance pattern)
                if( IsFreemodeModelName( modelName ) ) {
                    SetPedDefaultComponentVariation( _previewPed );
                    SetPedHeadBlendData( _previewPed, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f, false );
                }

                // Apply current appearance to preview ped
                if( !string.IsNullOrEmpty( _savedAppearance ) ) {
                    ApplyFullAppearance( _previewPed, _savedAppearance );
                }

                // Hide the player ped from camera
                SetEntityVisible( playerPed, false, false );

                SetPreviewCamera( "default" );
            } else {
                Debug.WriteLine( "[GamemodeCity] StartCustomization: FAILED to load model after " + timeout + " ticks" );
            }
        }

        public static async void StopCustomization( bool cancelled ) {
            if( !_customizing ) return;
            _customizing = false;

            int playerPed = PlayerPedId();

            // If saving, copy appearance from preview ped to player (including model change)
            if( !cancelled && _previewPed != 0 && DoesEntityExist( _previewPed ) ) {
                string appearance = GetCurrentAppearanceJson( _previewPed );

                // Check if the preview ped has a different model than the player
                uint previewModel = (uint)GetEntityModel( _previewPed );
                uint playerModel = (uint)GetEntityModel( playerPed );
                if( previewModel != playerModel ) {
                    RequestModel( previewModel );
                    int timeout = 0;
                    while( !HasModelLoaded( previewModel ) && timeout < 100 ) {
                        await BaseScript.Delay( 50 );
                        timeout++;
                    }
                    if( HasModelLoaded( previewModel ) ) {
                        SetPlayerModel( PlayerId(), previewModel );
                        SetModelAsNoLongerNeeded( previewModel );
                        playerPed = PlayerPedId();
                    }
                }

                ApplyFullAppearance( playerPed, appearance );
            }

            // Delete preview ped
            if( _previewPed != 0 && DoesEntityExist( _previewPed ) ) {
                DeletePed( ref _previewPed );
            }
            _previewPed = 0;

            // Show player again
            SetEntityVisible( playerPed, true, false );

            // Cleanup camera
            if( _previewCam != 0 && DoesCamExist( _previewCam ) ) {
                SetCamActive( _previewCam, false );
                DestroyCam( _previewCam, false );
            }
            _previewCam = 0;
            RenderScriptCams( false, false, 0, true, false );

            _savedAppearance = null;
            _savedModel = null;
        }

        // ==================== Appearance Natives ====================

        public static void ApplyFullAppearance( int ped, string json ) {
            if( string.IsNullOrEmpty( json ) ) return;
            try {
                var data = SimpleJsonParser.Parse( json );
                if( data == null ) return;

                bool isFreemode = IsPedModelFreemode( ped );

                // Head Blend
                if( isFreemode && data.ContainsKey( "headBlend" ) && data["headBlend"] is Dictionary<string, object> hb ) {
                    int shapeFirst = data.ContainsKey( "headBlend" ) ? GetInt( hb, "shapeFirst", 0 ) : 0;
                    int shapeSecond = GetInt( hb, "shapeSecond", 0 );
                    int shapeThird = GetInt( hb, "shapeThird", 0 );
                    int skinFirst = GetInt( hb, "skinFirst", 0 );
                    int skinSecond = GetInt( hb, "skinSecond", 0 );
                    int skinThird = GetInt( hb, "skinThird", 0 );
                    float shapeMix = GetFloat( hb, "shapeMix", 0.5f );
                    float skinMix = GetFloat( hb, "skinMix", 0.5f );
                    float thirdMix = GetFloat( hb, "thirdMix", 0f );
                    SetPedHeadBlendData( ped, shapeFirst, shapeSecond, shapeThird, skinFirst, skinSecond, skinThird, shapeMix, skinMix, thirdMix, false );
                }

                // Face Features
                if( isFreemode && data.ContainsKey( "faceFeatures" ) && data["faceFeatures"] is List<object> ff ) {
                    for( int i = 0; i < ff.Count && i < 20; i++ ) {
                        float val = Convert.ToSingle( ff[i] );
                        SetPedFaceFeature( ped, i, val );
                    }
                }

                // Head Overlays
                if( isFreemode && data.ContainsKey( "headOverlays" ) && data["headOverlays"] is List<object> ho ) {
                    for( int i = 0; i < ho.Count && i < 12; i++ ) {
                        if( ho[i] is Dictionary<string, object> ov ) {
                            int index = GetInt( ov, "index", 255 );
                            float opacity = GetFloat( ov, "opacity", 1f );
                            int colorType = GetInt( ov, "colorType", 0 );
                            int firstColor = GetInt( ov, "firstColor", 0 );
                            int secondColor = GetInt( ov, "secondColor", 0 );
                            SetPedHeadOverlay( ped, i, index, opacity );
                            SetPedHeadOverlayColor( ped, i, colorType, firstColor, secondColor );
                        }
                    }
                }

                // Hair
                if( data.ContainsKey( "hair" ) && data["hair"] is Dictionary<string, object> hair ) {
                    int style = GetInt( hair, "style", 0 );
                    int color = GetInt( hair, "color", 0 );
                    int highlight = GetInt( hair, "highlight", 0 );
                    SetPedComponentVariation( ped, 2, style, 0, 2 );
                    SetPedHairColor( ped, color, highlight );
                }

                // Eye Color
                if( isFreemode && data.ContainsKey( "eyeColor" ) ) {
                    int eyeColor = Convert.ToInt32( data["eyeColor"] );
                    SetPedEyeColor( ped, eyeColor );
                }

                // Components
                if( data.ContainsKey( "components" ) && data["components"] is List<object> comps ) {
                    for( int i = 0; i < comps.Count && i < 12; i++ ) {
                        if( i == 2 ) continue; // Skip hair component (handled above)
                        if( comps[i] is Dictionary<string, object> comp ) {
                            int drawable = GetInt( comp, "drawable", 0 );
                            int texture = GetInt( comp, "texture", 0 );
                            int palette = GetInt( comp, "palette", 2 );
                            SetPedComponentVariation( ped, i, drawable, texture, palette );
                        }
                    }
                }

                // Props
                if( data.ContainsKey( "props" ) && data["props"] is List<object> props ) {
                    foreach( var propObj in props ) {
                        if( propObj is Dictionary<string, object> prop ) {
                            int propId = GetInt( prop, "id", -1 );
                            int drawable = GetInt( prop, "drawable", -1 );
                            int texture = GetInt( prop, "texture", 0 );
                            if( propId >= 0 ) {
                                if( drawable < 0 ) {
                                    ClearPedProp( ped, propId );
                                } else {
                                    SetPedPropIndex( ped, propId, drawable, texture, true );
                                }
                            }
                        }
                    }
                }
            } catch( Exception ex ) {
                Debug.WriteLine( "[GamemodeCity] Error applying appearance: " + ex.Message );
            }
        }

        public static string GetCurrentAppearanceJson( int ped ) {
            bool isFreemode = IsPedModelFreemode( ped );

            // Detect model from the ped itself
            uint pedModel = (uint)GetEntityModel( ped );
            string modelName = SelectedModel; // fallback
            foreach( var m in PedModels.All ) {
                if( (uint)GetHashKey( m.Hash ) == pedModel ) {
                    modelName = m.Hash;
                    break;
                }
            }
            // Also check freemode models
            if( pedModel == (uint)GetHashKey( "mp_m_freemode_01" ) ) modelName = "mp_m_freemode_01";
            else if( pedModel == (uint)GetHashKey( "mp_f_freemode_01" ) ) modelName = "mp_f_freemode_01";

            string json = "{";

            // Model
            json += "\"model\":\"" + EscapeJson( modelName ) + "\"";

            // Head Blend
            if( isFreemode ) {
                // GetPedHeadBlendData outputs via ref params
                bool success = false;
                int sFirst = 0, sSecond = 0, sThird = 0, skFirst = 0, skSecond = 0, skThird = 0;
                float sMix = 0f, skMix = 0f, tMix = 0f;
                // FiveM doesn't have GetPedHeadBlendData as a simple return, use defaults
                json += ",\"headBlend\":{\"shapeFirst\":" + sFirst + ",\"shapeSecond\":" + sSecond + ",\"shapeThird\":" + sThird +
                    ",\"skinFirst\":" + skFirst + ",\"skinSecond\":" + skSecond + ",\"skinThird\":" + skThird +
                    ",\"shapeMix\":" + F( sMix ) + ",\"skinMix\":" + F( skMix ) + ",\"thirdMix\":" + F( tMix ) + "}";

                // Face Features
                json += ",\"faceFeatures\":[";
                for( int i = 0; i < 20; i++ ) {
                    if( i > 0 ) json += ",";
                    json += F( GetPedFaceFeature( ped, i ) );
                }
                json += "]";

                // Head Overlays
                json += ",\"headOverlays\":[";
                for( int i = 0; i < 12; i++ ) {
                    if( i > 0 ) json += ",";
                    int index = 255; float opacity = 1f; int colorType = 0; int firstColor = 0; int secondColor = 0;
                    // GTA doesn't expose GetPedHeadOverlay easily, use defaults
                    json += "{\"index\":" + index + ",\"opacity\":" + F( opacity ) + ",\"colorType\":" + colorType + ",\"firstColor\":" + firstColor + ",\"secondColor\":" + secondColor + "}";
                }
                json += "]";

                // Eye Color
                json += ",\"eyeColor\":0";
            }

            // Hair
            int hairDrawable = GetPedDrawableVariation( ped, 2 );
            int hairTexture = GetPedTextureVariation( ped, 2 );
            json += ",\"hair\":{\"style\":" + hairDrawable + ",\"color\":0,\"highlight\":0}";

            // Components
            json += ",\"components\":[";
            for( int i = 0; i < 12; i++ ) {
                if( i > 0 ) json += ",";
                int drawable = GetPedDrawableVariation( ped, i );
                int texture = GetPedTextureVariation( ped, i );
                json += "{\"drawable\":" + drawable + ",\"texture\":" + texture + ",\"palette\":2}";
            }
            json += "]";

            // Props
            json += ",\"props\":[";
            bool firstProp = true;
            foreach( int propId in AppearanceConstants.PropIndices ) {
                if( !firstProp ) json += ",";
                firstProp = false;
                int drawable = GetPedPropIndex( ped, propId );
                int texture = GetPedPropTextureIndex( ped, propId );
                json += "{\"id\":" + propId + ",\"drawable\":" + drawable + ",\"texture\":" + texture + "}";
            }
            json += "]";

            json += "}";
            return json;
        }

        public static string BuildAppearanceSettingsJson( int ped, string modelName = null ) {
            bool isFreemode = modelName != null ? IsFreemodeModelName( modelName ) : IsPedModelFreemode( ped );

            string json = "{\"isFreemode\":" + ( isFreemode ? "true" : "false" );

            // Component max values
            json += ",\"components\":[";
            for( int i = 0; i < 12; i++ ) {
                if( i > 0 ) json += ",";
                int maxDrawable = GetNumberOfPedDrawableVariations( ped, i );
                int curDrawable = GetPedDrawableVariation( ped, i );
                int maxTexture = GetNumberOfPedTextureVariations( ped, i, curDrawable );
                json += "{\"maxDrawable\":" + maxDrawable + ",\"maxTexture\":" + maxTexture + "}";
            }
            json += "]";

            // Prop max values
            json += ",\"props\":[";
            bool first = true;
            foreach( int propId in AppearanceConstants.PropIndices ) {
                if( !first ) json += ",";
                first = false;
                int maxDrawable = GetNumberOfPedPropDrawableVariations( ped, propId );
                int curDrawable = GetPedPropIndex( ped, propId );
                int maxTexture = curDrawable >= 0 ? GetNumberOfPedPropTextureVariations( ped, propId, curDrawable ) : 0;
                json += "{\"id\":" + propId + ",\"maxDrawable\":" + maxDrawable + ",\"maxTexture\":" + maxTexture + "}";
            }
            json += "]";

            // Head overlay max values (number of variations per overlay)
            if( isFreemode ) {
                json += ",\"overlays\":[";
                for( int i = 0; i < 12; i++ ) {
                    if( i > 0 ) json += ",";
                    int max = GetPedHeadOverlayNum( i );
                    json += max;
                }
                json += "]";
            }

            json += "}";
            return json;
        }

        private static bool IsFreemodeModelName( string modelName ) {
            return modelName == "mp_m_freemode_01" || modelName == "mp_f_freemode_01";
        }

        private static bool IsPedModelFreemode( int ped ) {
            uint model = (uint)GetEntityModel( ped );
            uint maleHash = (uint)GetHashKey( "mp_m_freemode_01" );
            uint femaleHash = (uint)GetHashKey( "mp_f_freemode_01" );
            return model == maleHash || model == femaleHash;
        }

        // ==================== NUI Callbacks ====================

        private async void OnNuiCustomizeStart( IDictionary<string, object> data, CallbackDelegate cb ) {
            try {
                string modelName = string.IsNullOrEmpty( SelectedModel ) ? "mp_m_freemode_01" : SelectedModel;
                Debug.WriteLine( "[GamemodeCity] customizeStart: model=" + modelName + " _customizing=" + _customizing + " _previewPed=" + _previewPed );
                StartCustomization();

                // Wait for preview ped to spawn
                int timeout = 0;
                while( ( _previewPed == 0 || !DoesEntityExist( _previewPed ) ) && timeout < 100 ) {
                    await Delay( 50 );
                    timeout++;
                }

                Debug.WriteLine( "[GamemodeCity] customizeStart: waited " + timeout + " ticks, _previewPed=" + _previewPed + " exists=" + ( _previewPed != 0 && DoesEntityExist( _previewPed ) ) );

                if( _previewPed == 0 || !DoesEntityExist( _previewPed ) ) {
                    Debug.WriteLine( "[GamemodeCity] customizeStart: FAILED - ped not spawned" );
                    cb( "{\"status\":\"error\",\"reason\":\"ped_timeout\"}" );
                    return;
                }

                string settings = BuildAppearanceSettingsJson( _previewPed, modelName );
                string appearance = GetCurrentAppearanceJson( _previewPed );

                Debug.WriteLine( "[GamemodeCity] customizeStart: OK, isFreemode=" + IsFreemodeModelName( modelName ) );
                cb( "{\"status\":\"ok\",\"settings\":" + settings + ",\"appearance\":" + appearance + "}" );
            } catch( Exception ex ) {
                Debug.WriteLine( "[GamemodeCity] customizeStart EXCEPTION: " + ex.Message );
                cb( "{\"status\":\"error\",\"reason\":\"exception\"}" );
            }
        }

        private void OnNuiCustomizeCancel( IDictionary<string, object> data, CallbackDelegate cb ) {
            StopCustomization( true );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiCustomizeSave( IDictionary<string, object> data, CallbackDelegate cb ) {
            // Read appearance from preview ped before StopCustomization deletes it
            if( _previewPed != 0 && DoesEntityExist( _previewPed ) ) {
                string appearance = GetCurrentAppearanceJson( _previewPed );
                AppearanceJson = appearance;
                TriggerServerEvent( "salty:saveAppearance", appearance );
            }
            StopCustomization( false );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiSetCameraPreset( IDictionary<string, object> data, CallbackDelegate cb ) {
            string preset = data.ContainsKey( "preset" ) ? data["preset"].ToString() : "default";
            SetPreviewCamera( preset );
            cb( "{\"status\":\"ok\"}" );
        }

        private async void OnNuiChangeModel( IDictionary<string, object> data, CallbackDelegate cb ) {
            string modelHash = data.ContainsKey( "modelHash" ) ? data["modelHash"].ToString() : "";
            Debug.WriteLine( "[GamemodeCity] changeModel: hash=" + modelHash + " _previewPos=" + _previewPos );
            if( string.IsNullOrEmpty( modelHash ) ) { cb( "{\"status\":\"error\",\"reason\":\"empty_hash\"}" ); return; }

            try {
                uint hash = (uint)GetHashKey( modelHash );
                RequestModel( hash );
                int timeout = 0;
                while( !HasModelLoaded( hash ) && timeout < 100 ) {
                    await Delay( 50 );
                    timeout++;
                }
                if( !HasModelLoaded( hash ) ) {
                    Debug.WriteLine( "[GamemodeCity] changeModel: model failed to load after " + timeout + " ticks" );
                    cb( "{\"status\":\"error\",\"reason\":\"model_load_timeout\"}" );
                    return;
                }

                // Delete old preview ped and create new one with the new model
                if( _previewPed != 0 && DoesEntityExist( _previewPed ) ) {
                    DeletePed( ref _previewPed );
                }

                _previewPed = CreatePed( 4, hash, _previewPos.X, _previewPos.Y, _previewPos.Z, 180f, false, false );
                SetModelAsNoLongerNeeded( hash );

                Debug.WriteLine( "[GamemodeCity] changeModel: created ped " + _previewPed + " exists=" + DoesEntityExist( _previewPed ) );

                FreezeEntityPosition( _previewPed, true );
                TaskStandStill( _previewPed, -1 );
                SetEntityInvincible( _previewPed, true );
                SetEntityCollision( _previewPed, false, false );
                SetBlockingOfNonTemporaryEvents( _previewPed, true );

                // Initialize freemode peds (matching fivem-appearance pattern)
                if( IsFreemodeModelName( modelHash ) ) {
                    SetPedDefaultComponentVariation( _previewPed );
                    SetPedHeadBlendData( _previewPed, 0, 0, 0, 0, 0, 0, 0f, 0f, 0f, false );
                }

                // Refresh camera to look at new ped
                SetPreviewCamera( "default" );

                string settings = BuildAppearanceSettingsJson( _previewPed, modelHash );
                string appearance = GetCurrentAppearanceJson( _previewPed );

                Debug.WriteLine( "[GamemodeCity] changeModel: OK, isFreemode=" + IsFreemodeModelName( modelHash ) );
                cb( "{\"status\":\"ok\",\"settings\":" + settings + ",\"appearance\":" + appearance + "}" );
            } catch( Exception ex ) {
                Debug.WriteLine( "[GamemodeCity] changeModel EXCEPTION: " + ex.Message + "\n" + ex.StackTrace );
                cb( "{\"status\":\"error\",\"reason\":\"exception\"}" );
            }
        }

        private void OnNuiChangeHeadBlend( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int shapeFirst = data.ContainsKey( "shapeFirst" ) ? Convert.ToInt32( data["shapeFirst"] ) : 0;
            int shapeSecond = data.ContainsKey( "shapeSecond" ) ? Convert.ToInt32( data["shapeSecond"] ) : 0;
            float shapeMix = data.ContainsKey( "shapeMix" ) ? Convert.ToSingle( data["shapeMix"] ) : 0.5f;
            int skinFirst = data.ContainsKey( "skinFirst" ) ? Convert.ToInt32( data["skinFirst"] ) : 0;
            int skinSecond = data.ContainsKey( "skinSecond" ) ? Convert.ToInt32( data["skinSecond"] ) : 0;
            float skinMix = data.ContainsKey( "skinMix" ) ? Convert.ToSingle( data["skinMix"] ) : 0.5f;
            SetPedHeadBlendData( _previewPed, shapeFirst, shapeSecond, 0, skinFirst, skinSecond, 0, shapeMix, skinMix, 0f, false );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiChangeFaceFeature( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int index = data.ContainsKey( "index" ) ? Convert.ToInt32( data["index"] ) : 0;
            float value = data.ContainsKey( "value" ) ? Convert.ToSingle( data["value"] ) : 0f;
            SetPedFaceFeature( _previewPed, index, value );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiChangeHeadOverlay( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int overlayId = data.ContainsKey( "overlayId" ) ? Convert.ToInt32( data["overlayId"] ) : 0;
            int index = data.ContainsKey( "index" ) ? Convert.ToInt32( data["index"] ) : 255;
            float opacity = data.ContainsKey( "opacity" ) ? Convert.ToSingle( data["opacity"] ) : 1f;
            int colorType = data.ContainsKey( "colorType" ) ? Convert.ToInt32( data["colorType"] ) : 0;
            int firstColor = data.ContainsKey( "firstColor" ) ? Convert.ToInt32( data["firstColor"] ) : 0;
            int secondColor = data.ContainsKey( "secondColor" ) ? Convert.ToInt32( data["secondColor"] ) : 0;
            SetPedHeadOverlay( _previewPed, overlayId, index, opacity );
            SetPedHeadOverlayColor( _previewPed, overlayId, colorType, firstColor, secondColor );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiChangeHair( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int style = data.ContainsKey( "style" ) ? Convert.ToInt32( data["style"] ) : 0;
            int color = data.ContainsKey( "color" ) ? Convert.ToInt32( data["color"] ) : 0;
            int highlight = data.ContainsKey( "highlight" ) ? Convert.ToInt32( data["highlight"] ) : 0;
            SetPedComponentVariation( _previewPed, 2, style, 0, 2 );
            SetPedHairColor( _previewPed, color, highlight );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiChangeEyeColor( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int color = data.ContainsKey( "color" ) ? Convert.ToInt32( data["color"] ) : 0;
            SetPedEyeColor( _previewPed, color );
            cb( "{\"status\":\"ok\"}" );
        }

        private void OnNuiChangeComponent( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int componentId = data.ContainsKey( "componentId" ) ? Convert.ToInt32( data["componentId"] ) : 0;
            int drawable = data.ContainsKey( "drawable" ) ? Convert.ToInt32( data["drawable"] ) : 0;
            int texture = data.ContainsKey( "texture" ) ? Convert.ToInt32( data["texture"] ) : 0;
            SetPedComponentVariation( _previewPed, componentId, drawable, texture, 2 );
            int maxTexture = GetNumberOfPedTextureVariations( _previewPed, componentId, drawable );
            cb( "{\"status\":\"ok\",\"maxTexture\":" + maxTexture + "}" );
        }

        private void OnNuiChangeProp( IDictionary<string, object> data, CallbackDelegate cb ) {
            if( _previewPed == 0 ) { cb( "{\"status\":\"error\"}" ); return; }
            int propId = data.ContainsKey( "propId" ) ? Convert.ToInt32( data["propId"] ) : 0;
            int drawable = data.ContainsKey( "drawable" ) ? Convert.ToInt32( data["drawable"] ) : -1;
            int texture = data.ContainsKey( "texture" ) ? Convert.ToInt32( data["texture"] ) : 0;
            if( drawable < 0 ) {
                ClearPedProp( _previewPed, propId );
                cb( "{\"status\":\"ok\",\"maxTexture\":0}" );
            } else {
                SetPedPropIndex( _previewPed, propId, drawable, texture, true );
                int maxTexture = GetNumberOfPedPropTextureVariations( _previewPed, propId, drawable );
                cb( "{\"status\":\"ok\",\"maxTexture\":" + maxTexture + "}" );
            }
        }

        private void OnNuiPurchaseItem( IDictionary<string, object> data, CallbackDelegate cb ) {
            string itemKey = data.ContainsKey( "itemKey" ) ? data["itemKey"].ToString() : "";
            if( !string.IsNullOrEmpty( itemKey ) ) {
                TriggerServerEvent( "salty:purchaseItem", itemKey );
            }
            cb( "{\"status\":\"ok\"}" );
        }

        // ==================== Helpers ====================

        private static string BuildUnlockedModelsJson() {
            var entries = new List<string>();
            foreach( var hash in UnlockedModels ) {
                entries.Add( "\"" + EscapeJson( hash ) + "\"" );
            }
            return string.Join( ",", entries );
        }

        private static string BuildUnlockedItemsJson() {
            var entries = new List<string>();
            foreach( var item in UnlockedItems ) {
                entries.Add( "\"" + EscapeJson( item ) + "\"" );
            }
            return string.Join( ",", entries );
        }

        private static string EscapeJson( string s ) {
            if( s == null ) return "";
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ).Replace( "\n", "\\n" ).Replace( "\r", "" );
        }

        private static string F( float v ) {
            return v.ToString( CultureInfo.InvariantCulture );
        }

        private static int GetInt( Dictionary<string, object> d, string key, int def ) {
            return d.ContainsKey( key ) ? Convert.ToInt32( d[key] ) : def;
        }

        private static float GetFloat( Dictionary<string, object> d, string key, float def ) {
            return d.ContainsKey( key ) ? Convert.ToSingle( d[key] ) : def;
        }
    }

    /// <summary>
    /// Minimal JSON parser for client-side use (no external dependencies).
    /// Handles objects, arrays, strings, numbers, booleans, null.
    /// </summary>
    public static class SimpleJsonParser {
        private static int pos;
        private static string src;

        public static Dictionary<string, object> Parse( string json ) {
            if( string.IsNullOrEmpty( json ) ) return null;
            src = json;
            pos = 0;
            SkipWhitespace();
            if( pos < src.Length && src[pos] == '{' ) {
                return ReadObject();
            }
            return null;
        }

        private static void SkipWhitespace() {
            while( pos < src.Length && char.IsWhiteSpace( src[pos] ) ) pos++;
        }

        private static Dictionary<string, object> ReadObject() {
            var dict = new Dictionary<string, object>();
            pos++; // skip '{'
            SkipWhitespace();
            if( pos < src.Length && src[pos] == '}' ) { pos++; return dict; }

            while( pos < src.Length ) {
                SkipWhitespace();
                string key = ReadString();
                SkipWhitespace();
                if( pos < src.Length && src[pos] == ':' ) pos++;
                SkipWhitespace();
                object value = ReadValue();
                dict[key] = value;
                SkipWhitespace();
                if( pos < src.Length && src[pos] == ',' ) { pos++; continue; }
                if( pos < src.Length && src[pos] == '}' ) { pos++; break; }
                break;
            }
            return dict;
        }

        private static List<object> ReadArray() {
            var list = new List<object>();
            pos++; // skip '['
            SkipWhitespace();
            if( pos < src.Length && src[pos] == ']' ) { pos++; return list; }

            while( pos < src.Length ) {
                SkipWhitespace();
                object value = ReadValue();
                list.Add( value );
                SkipWhitespace();
                if( pos < src.Length && src[pos] == ',' ) { pos++; continue; }
                if( pos < src.Length && src[pos] == ']' ) { pos++; break; }
                break;
            }
            return list;
        }

        private static object ReadValue() {
            SkipWhitespace();
            if( pos >= src.Length ) return null;
            char c = src[pos];
            if( c == '"' ) return ReadString();
            if( c == '{' ) return ReadObject();
            if( c == '[' ) return ReadArray();
            if( c == 't' ) { pos += 4; return true; }
            if( c == 'f' ) { pos += 5; return false; }
            if( c == 'n' ) { pos += 4; return null; }
            return ReadNumber();
        }

        private static string ReadString() {
            pos++; // skip opening quote
            int start = pos;
            var sb = new System.Text.StringBuilder();
            while( pos < src.Length ) {
                char c = src[pos];
                if( c == '\\' ) {
                    pos++;
                    if( pos < src.Length ) {
                        char esc = src[pos];
                        switch( esc ) {
                            case '"': sb.Append( '"' ); break;
                            case '\\': sb.Append( '\\' ); break;
                            case '/': sb.Append( '/' ); break;
                            case 'n': sb.Append( '\n' ); break;
                            case 'r': sb.Append( '\r' ); break;
                            case 't': sb.Append( '\t' ); break;
                            default: sb.Append( esc ); break;
                        }
                        pos++;
                    }
                } else if( c == '"' ) {
                    pos++;
                    return sb.ToString();
                } else {
                    sb.Append( c );
                    pos++;
                }
            }
            return sb.ToString();
        }

        private static object ReadNumber() {
            int start = pos;
            bool isFloat = false;
            if( pos < src.Length && ( src[pos] == '-' || src[pos] == '+' ) ) pos++;
            while( pos < src.Length && ( char.IsDigit( src[pos] ) || src[pos] == '.' || src[pos] == 'e' || src[pos] == 'E' || src[pos] == '+' || src[pos] == '-' ) ) {
                if( src[pos] == '.' || src[pos] == 'e' || src[pos] == 'E' ) isFloat = true;
                pos++;
            }
            string numStr = src.Substring( start, pos - start );
            if( isFloat ) {
                float f;
                if( float.TryParse( numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out f ) ) return f;
                return 0f;
            } else {
                int i;
                if( int.TryParse( numStr, out i ) ) return i;
                long l;
                if( long.TryParse( numStr, out l ) ) return l;
                return 0;
            }
        }
    }
}
