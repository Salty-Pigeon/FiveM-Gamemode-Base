using System.Collections.Generic;
using System.Linq;

namespace GamemodeCityShared {
    public class PedModelInfo {
        public string Hash;
        public string Name;
        public string Category;

        public PedModelInfo( string hash, string name, string category ) {
            Hash = hash;
            Name = name;
            Category = category;
        }
    }

    public static class AppearanceConstants {
        public const int RegularModelCost = 500;      // Male/Female
        public const int SpecialModelCost = 1000;      // Special
        public const int FreemodeModelCost = 100000;   // Freemode
        public const int ClothingCost = 0;
        public const int PropCost = 0;
        public const int HairStyleCost = 0;

        public static int GetModelCost( string modelHash ) {
            var model = PedModels.All.FirstOrDefault( m => m.Hash == modelHash );
            if( model == null ) return RegularModelCost;
            if( model.Category == "Freemode" ) return FreemodeModelCost;
            if( model.Category == "Special" ) return SpecialModelCost;
            return RegularModelCost;
        }

        public static readonly string[] ComponentNames = {
            "Face",        // 0
            "Mask",        // 1
            "Hair",        // 2
            "Torso",       // 3
            "Legs",        // 4
            "Bag",         // 5
            "Shoes",       // 6
            "Accessory",   // 7
            "Undershirt",  // 8
            "Armor",       // 9
            "Decals",      // 10
            "Tops"         // 11
        };

        public static readonly string[] PropNames = {
            "Hats",        // 0
            "Glasses",     // 1
            "Ears",        // 2
            "Watch",       // 6
            "Bracelet"     // 7
        };

        public static readonly int[] PropIndices = { 0, 1, 2, 6, 7 };

        public static readonly string[] FaceFeatureKeys = {
            "Nose Width",          // 0
            "Nose Peak Height",    // 1
            "Nose Peak Length",    // 2
            "Nose Bone Height",    // 3
            "Nose Peak Lowering",  // 4
            "Nose Bone Twist",     // 5
            "Eyebrow Height",      // 6
            "Eyebrow Depth",       // 7
            "Cheekbone Height",    // 8
            "Cheekbone Width",     // 9
            "Cheek Width",         // 10
            "Eye Opening",         // 11
            "Lip Thickness",       // 12
            "Jaw Bone Width",      // 13
            "Jaw Bone Shape",      // 14
            "Chin Height",         // 15
            "Chin Length",         // 16
            "Chin Width",          // 17
            "Chin Hole Size",      // 18
            "Neck Thickness"       // 19
        };

        public static readonly string[] HeadOverlayKeys = {
            "Blemishes",       // 0
            "Facial Hair",     // 1
            "Eyebrows",        // 2
            "Ageing",          // 3
            "Makeup",          // 4
            "Blush",           // 5
            "Complexion",      // 6
            "Sun Damage",      // 7
            "Lipstick",        // 8
            "Moles/Freckles",  // 9
            "Chest Hair",      // 10
            "Body Blemishes"   // 11
        };

        public static readonly string[] EyeColorNames = {
            "Green",           // 0
            "Emerald",         // 1
            "Light Blue",      // 2
            "Ocean Blue",      // 3
            "Light Brown",     // 4
            "Dark Brown",      // 5
            "Hazel",           // 6
            "Dark Gray",       // 7
            "Light Gray",      // 8
            "Pink",            // 9
            "Yellow",          // 10
            "Purple",          // 11
            "Blackout",        // 12
            "Shades of Gray",  // 13
            "Tequila Sunrise", // 14
            "Neon",            // 15
            "Red Inferno",     // 16
            "Alien Blue",      // 17
            "Gold Dust",       // 18
            "Amber",           // 19
            "Sea Green",       // 20
            "Mist",            // 21
            "Retro",           // 22
            "Mint",            // 23
            "Tiger Eye",       // 24
            "Mocha",           // 25
            "Sterling",        // 26
            "Midnight",        // 27
            "Blaze",           // 28
            "Frost",           // 29
            "Vivid",           // 30
        };
    }

    public static class PedModels {
        public static List<PedModelInfo> All = new List<PedModelInfo> {
            // ==================== Freemode (full customization) ====================
            new PedModelInfo( "mp_m_freemode_01", "Custom Male", "Freemode" ),
            new PedModelInfo( "mp_f_freemode_01", "Custom Female", "Freemode" ),

            // ==================== Male (~25) ====================
            new PedModelInfo( "a_m_y_hipster_01", "Hipster", "Male" ),
            new PedModelInfo( "a_m_y_hipster_02", "Hipster Alt", "Male" ),
            new PedModelInfo( "a_m_y_hipster_03", "Hipster Trendy", "Male" ),
            new PedModelInfo( "a_m_y_skater_01", "Skater", "Male" ),
            new PedModelInfo( "a_m_y_surfer_01", "Surfer", "Male" ),
            new PedModelInfo( "a_m_y_vinewood_01", "Vinewood", "Male" ),
            new PedModelInfo( "a_m_y_business_01", "Businessman", "Male" ),
            new PedModelInfo( "a_m_y_business_02", "Business Exec", "Male" ),
            new PedModelInfo( "a_m_y_beach_01", "Beach Guy", "Male" ),
            new PedModelInfo( "a_m_y_runner_01", "Runner", "Male" ),
            new PedModelInfo( "a_m_y_cyclist_01", "Cyclist", "Male" ),
            new PedModelInfo( "a_m_y_stunt_01", "Stunt Double", "Male" ),
            new PedModelInfo( "a_m_m_farmer_01", "Farmer", "Male" ),
            new PedModelInfo( "a_m_m_golfer_01", "Golfer", "Male" ),
            new PedModelInfo( "a_m_m_hillbilly_01", "Hillbilly", "Male" ),
            new PedModelInfo( "a_m_m_paparazzi_01", "Paparazzi", "Male" ),
            new PedModelInfo( "a_m_m_tourist_01", "Tourist", "Male" ),
            new PedModelInfo( "a_m_m_tramp_01", "Tramp", "Male" ),
            new PedModelInfo( "a_m_y_genstreet_01", "Street Guy", "Male" ),
            new PedModelInfo( "a_m_y_musclbeach_01", "Muscle Beach", "Male" ),
            new PedModelInfo( "g_m_y_lost_01", "Lost MC", "Male" ),
            new PedModelInfo( "g_m_y_ballasout_01", "Ballas", "Male" ),
            new PedModelInfo( "g_m_y_famfor_01", "Families", "Male" ),
            new PedModelInfo( "s_m_y_pilot_01", "Pilot", "Male" ),
            new PedModelInfo( "s_m_y_fireman_01", "Fireman", "Male" ),

            // ==================== Female (~25) ====================
            new PedModelInfo( "a_f_y_hipster_01", "Hipster Girl", "Female" ),
            new PedModelInfo( "a_f_y_hipster_02", "Hipster Alt Girl", "Female" ),
            new PedModelInfo( "a_f_y_hipster_03", "Hipster Trendy Girl", "Female" ),
            new PedModelInfo( "a_f_y_hipster_04", "Hipster Artsy", "Female" ),
            new PedModelInfo( "a_f_y_fitness_01", "Fitness Girl", "Female" ),
            new PedModelInfo( "a_f_y_fitness_02", "Fitness Alt", "Female" ),
            new PedModelInfo( "a_f_y_beach_01", "Beach Girl", "Female" ),
            new PedModelInfo( "a_f_y_business_01", "Businesswoman", "Female" ),
            new PedModelInfo( "a_f_y_business_02", "Business Exec", "Female" ),
            new PedModelInfo( "a_f_y_business_04", "Business Formal", "Female" ),
            new PedModelInfo( "a_f_y_vinewood_01", "Vinewood Girl", "Female" ),
            new PedModelInfo( "a_f_y_vinewood_02", "Vinewood Glam", "Female" ),
            new PedModelInfo( "a_f_y_tourist_01", "Tourist Girl", "Female" ),
            new PedModelInfo( "a_f_y_runner_01", "Runner Girl", "Female" ),
            new PedModelInfo( "a_f_y_skater_01", "Skater Girl", "Female" ),
            new PedModelInfo( "a_f_y_soucent_01", "South Central", "Female" ),
            new PedModelInfo( "a_f_y_soucent_02", "South Central Alt", "Female" ),
            new PedModelInfo( "a_f_y_tennis_01", "Tennis Player", "Female" ),
            new PedModelInfo( "a_f_y_topless_01", "Topless", "Female" ),
            new PedModelInfo( "a_f_y_bevhills_01", "Beverly Hills", "Female" ),
            new PedModelInfo( "a_f_y_bevhills_02", "Beverly Hills Alt", "Female" ),
            new PedModelInfo( "a_f_m_beach_01", "Beach Lady", "Female" ),
            new PedModelInfo( "a_f_m_fatbla_01", "Casual Lady", "Female" ),
            new PedModelInfo( "a_f_y_genhot_01", "Hot Girl", "Female" ),
            new PedModelInfo( "a_f_y_golfer_01", "Golfer Girl", "Female" ),

            // ==================== Special (~15) ====================
            new PedModelInfo( "s_m_y_clown_01", "Clown", "Special" ),
            new PedModelInfo( "s_m_m_mover_01", "Mover", "Special" ),
            new PedModelInfo( "s_m_y_mime", "Mime", "Special" ),
            new PedModelInfo( "s_m_y_swat_01", "SWAT", "Special" ),
            new PedModelInfo( "s_m_y_cop_01", "Cop", "Special" ),
            new PedModelInfo( "s_m_m_paramedic_01", "Paramedic", "Special" ),
            new PedModelInfo( "s_m_m_scientist_01", "Scientist", "Special" ),
            new PedModelInfo( "s_m_y_blackops_01", "Black Ops", "Special" ),
            new PedModelInfo( "s_m_y_marine_01", "Marine", "Special" ),
            new PedModelInfo( "u_m_y_zombie_01", "Zombie", "Special" ),
            new PedModelInfo( "s_m_m_highsec_01", "Security Guard", "Special" ),
            new PedModelInfo( "s_m_y_ranger_01", "Ranger", "Special" ),
            new PedModelInfo( "s_m_y_sheriff_01", "Sheriff", "Special" ),
            new PedModelInfo( "a_m_m_eastsa_02", "East LS OG", "Special" ),
            new PedModelInfo( "ig_lifeinvad_01", "Life Invader", "Special" )
        };
    }
}
