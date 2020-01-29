using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient {
    public class Globals : BaseScript {

        public static Dictionary<string, BaseGamemode> Gamemodes = new Dictionary<string, BaseGamemode>();

        public static bool isNoclip = false;


        public static void Init() {

            Debug.WriteLine( Weapons.Keys.First().ToString() );
        }


        public static void SendMap( Map map ) {
            TriggerServerEvent( "saltyMap:netUpdate", new Dictionary<string, dynamic> {
                { "name", string.Join( " ", map.Name ) },
                { "position", map.Position },
                { "size", map.Size }
            } );
        }

        public static void WriteChat( string prefix, string str, int r, int g, int b ) {
            TriggerEvent("chat:addMessage", new {
                color = new[] { r, g, b },
                args = new[] { prefix, str }
            });
        }

        public static Vector3 StringToVector3( string vector ) {
            vector = vector.Replace("X:", "");
            vector = vector.Replace("Y:", "");
            vector = vector.Replace("Z:", "");
            string[] vector3 = vector.Split(' ');
            return new Vector3(float.Parse(vector3[0]), float.Parse(vector3[1]), float.Parse(vector3[2]));
        }

        public static void SetNoClip( bool toggle ) {
            isNoclip = toggle;
            SetEntityVisible(PlayerPedId(), !isNoclip, false);
            SetEntityCollision(PlayerPedId(), !isNoclip, !isNoclip);
            SetEntityInvincible(PlayerPedId(), isNoclip);
            SetEveryoneIgnorePlayer(PlayerPedId(), isNoclip);
        }


        public static void NoClipUpdate() {

            Vector3 heading = GetGameplayCamRot(0);
            SetEntityRotation(PlayerPedId(), heading.X, heading.Y, -heading.Z, 0, true);
            SetEntityHeading(PlayerPedId(), heading.Z);

            int speed = 1;

            if (IsControlPressed(0, 21)) {
                speed *= 6;
            }

            Vector3 offset = new Vector3(0, 0, 0);

            if (IsControlPressed(0, 36)) {
                offset.Z = -speed;
            }

            if (IsControlPressed(0, 22)) {
                offset.Z = speed;
            }

            if (IsControlPressed(0, 33)) {
                offset.Y = -speed;
            }

            if (IsControlPressed(0, 32)) {
                offset.Y = speed;
            }

            if (IsControlPressed(0, 35)) {
                offset.X = speed;
            }

            if (IsControlPressed(0, 34)) {
                offset.X = -speed;
            }

            var noclipPos = GetOffsetFromEntityInWorldCoords(PlayerPedId(), offset.X, offset.Y, offset.Z);
            SetEntityCoordsNoOffset(PlayerPedId(), noclipPos.X, noclipPos.Y, noclipPos.Z, false, false, false);

        }

        public static void FirstPersonForAlive() {
            DisableControlAction(0, 0, true);
            if (GetFollowPedCamViewMode() != 4)
                SetFollowPedCamViewMode(4);
        }

        public static Dictionary<uint, Dictionary<string, dynamic>> Weapons = new Dictionary<uint, Dictionary<string, dynamic>>() {
            { 741814745, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_pe" }, { "Description", "A plastic explosive charge fitted with a remote detonator. Can be thrown and then detonated or attached to a vehicle then detonated." }, { "AmmoType", "AMMO_STICKYBOMB" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_GNADE_STK" }, { "Name", "Sticky Bomb" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_GNADE_STK" }, { "HashKey", "WEAPON_STICKYBOMB" }, } },
            { 1305664598, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_lr_grenadelauncher" },{ "Description", "" }, { "AmmoType", "AMMO_GRENADELAUNCHER_SMOKE" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "10" }, { "NameGXT", "WT_GL_SMOKE" }, { "Name", "Tear Gas Launcher"}, { "DLC", "core" }, { "DescriptionGXT", "WTD_GL_SMOKE" }, { "HashKey", "WEAPON_GRENADELAUNCHER_SMOKE" }, } },
            { 1317494643, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_hammer" }, { "Description", "A robust, multi-purpose hammer with wooden handle and curved claw, this old classicstill nails the competition." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_HAMMER" }, { "Name", "Hammer" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_HAMMER" }, { "HashKey", "WEAPON_HAMMER" }, } },
            { 2228681469, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_bullpupriflemk2" },{ "Description", "So precise, so exquisite, it's not so much a hail of bullets as a symphony." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_BULLRIFLE2" }, { "Name", "Bullpup Rifle Mk II" }, { "DLC", "mpchristmas2017"}, { "DescriptionGXT", "WTD_BULLRIFLE2" }, { "HashKey", "WEAPON_BULLPUPRIFLE_MK2" }, } },
            { 911657153, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_stungun" }, { "Description", "Fires a projectile that administers a voltage capable of temporarily stunning an assailant. Takes approximately 4 seconds to recharge after firing." }, { "AmmoType", "AMMO_STUNGUN" }, { "Group", "GROUP_STUNGUN" }, { "DefaultClipSize", "2104529083" }, { "NameGXT", "WT_STUN" }, { "Name", "Stun Gun" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_STUN" }, { "HashKey", "WEAPON_STUNGUN" }, } },
            { 984333226, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_heavyshotgun" }, { "Description", "The weapon to reach for when you absolutely need to make a horrible mess of the room. Best used near easy-wipe surfaces only. Part of the Last Team Standing Update." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_HVYSHGN" }, { "Name", "Heavy Shotgun" }, { "DLC", "mplts" }, { "DescriptionGXT", "WTD_HVYSHGN" }, { "HashKey", "WEAPON_HEAVYSHOTGUN" }, } },
            { 2982836145, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_lr_rpg" }, { "Description", "A portable, shoulder-launched, anti-tank weapon that fires explosive warheads. Very effective for taking down vehicles or large groups of assailants." }, { "AmmoType", "AMMO_RPG" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_RPG" }, { "Name","RPG" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_RPG" }, { "HashKey", "WEAPON_RPG" }, } },
            { 3342088282, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sr_marksmanrifle" }, {"Description", "Whether you're up close or a disconcertingly long way away, this weapon willget the job done. A multi-range tool for tools. Part of the Last Team Standing Update." }, {"AmmoType", "AMMO_SNIPER" }, { "Group", "GROUP_SNIPER" }, { "DefaultClipSize", "8" }, { "NameGXT", "WT_MKRIFLE" }, { "Name", "Marksman Rifle" }, { "DLC", "mplts" }, { "DescriptionGXT", "WTD_MKRIFLE" }, { "HashKey", "WEAPON_MARKSMANRIFLE" }, } },
            { 453432689, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_PI_PISTOL" }, { "Description", "Standard handgun. A .45 caliber pistol with a magazine capacity of 12 rounds that can be extended to 16." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "12" }, { "NameGXT", "WT_PIST" }, { "Name", "Pistol" }, { "DLC", "core" }, { "DescriptionGXT", "WT_PIST_DESC" }, { "HashKey", "WEAPON_PISTOL" }, } },
            { 2939590305, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_raygun" }, { "Description", "Republican Space Ranger Special, fresh from the galactic war on socialism: no ammo,no mag, just one brutal energy pulse after another." }, { "AmmoType", "AMMO_RAYPISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_RAYPISTOL" }, { "Name", "Up-n-Atomizer" }, { "DLC", "mpchristmas2018" }, { "DescriptionGXT", "WTD_RAYPISTOL" }, { "HashKey", "WEAPON_RAYPISTOL" }, } },
            { 2725352035, new Dictionary<string, dynamic>(){ { "ModelHashKey", "" }, { "Description", ""}, { "Group", "GROUP_UNARMED" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_UNARMED" }, { "Name", "Unarmed" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_UNARMED" }, { "HashKey", "WEAPON_UNARMED" }, } },
            { 126349499, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_snowball" }, { "Description", "" }, { "AmmoType", "AMMO_SNOWBALL" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_SNWBALL" }, { "Name", "Snowball" }, { "DLC", "mpchristmas2" }, { "DescriptionGXT", "WTD_SNWBALL" }, { "HashKey", "WEAPON_SNOWBALL" }, } },
            { 2285322324, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_sns_pistolmk2" }, {"Description", "The ultimate purse-filler: if you want to make Saturday Night really special, this is your ticket." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_SNSPISTOL2" }, { "Name", "SNS Pistol Mk II" }, { "DLC", "mpchristmas2017" }, { "DescriptionGXT", "WTD_SNSPISTOL2" }, { "HashKey", "WEAPON_SNSPISTOL_MK2" }, } },
            { 1649403952, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_assaultrifle_smg" }, { "Description", "Half the size, all the power, double the recoil: there's no riskier way to say \"I'm compensating for something\". Part of Lowriders: Custom Classics." }, { "AmmoType","AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_CMPRIFLE" }, { "Name", "Compact Rifle" }, { "DLC", "mplowrider2" }, { "DescriptionGXT", "WTD_CMPRIFLE" }, { "HashKey", "WEAPON_COMPACTRIFLE" }, } },
            { 2484171525, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_poolcue" }, { "Description", "Ah, there's no sound as satisfying as the crack of a perfect break, especially when it's the other guy's spine. Part of Bikers." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_POOLCUE" }, { "Name", "Pool Cue" }, { "DLC", "mpbiker" }, { "DescriptionGXT", "WTD_POOLCUE" }, { "HashKey", "WEAPON_POOLCUE" }, } },
            { 101631238, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_am_fire_exting" }, { "Description", "" }, { "AmmoType", "AMMO_FIREEXTINGUISHER" }, { "Group", "GROUP_FIREEXTINGUISHER" }, { "DefaultClipSize", "2000" }, { "NameGXT", "WT_FIRE" }, { "Name", "Fire Extinguisher" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_FIRE" }, { "HashKey", "WEAPON_FIREEXTINGUISHER" }, } },
            { 2481070269, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_grenadefrag" }, { "Description", "Standard fragmentation grenade. Pull pin, throw, then find cover. Ideal for eliminating clustered assailants." }, { "AmmoType", "AMMO_GRENADE" }, { "Group", "GROUP_THROWN"}, { "DefaultClipSize", "1" }, { "NameGXT", "WT_GNADE" }, { "Name", "Grenade" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_GNADE" }, { "HashKey", "WEAPON_GRENADE" }, } },
            { 171789620, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_SB_PDW" }, { "Description", "Who said personal weaponry couldn't be worthy of military personnel? Thanks to our lobbyists, not Congress. Integral suppressor. Part of the Ill-Gotten Gains Update Part 1." }, { "AmmoType", "AMMO_SMG" }, { "Group", "GROUP_SMG" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_COMBATPDW" }, { "Name", "Combat PDW" }, { "DLC", "mpluxe" }, { "DescriptionGXT", "WTD_COMBATPDW" }, { "HashKey", "WEAPON_COMBATPDW" }, } },
            { 4191993645, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_hatchet" }, { "Description", "Make kindling... of your pals with this easy to wield, easy to hide hatchet. Exclusive content for returning players." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_HATCHET" }, { "Name", "Hatchet" }, { "DLC", "spupgrade" }, { "DescriptionGXT", "WTD_HATCHET" }, { "HashKey", "WEAPON_HATCHET" }, } },
            { 584646201, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_PI_APPISTOL" }, { "Description", "High-penetration, fully-automatic pistol. Holds 18 rounds in magazine with option to extend to 36 rounds." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "18" }, { "NameGXT", "WT_PIST_AP" }, { "Name", "AP Pistol" }, { "DLC", "core"}, { "DescriptionGXT", "WTD_PIST_AP" }, { "HashKey", "WEAPON_APPISTOL" }, } },
            { 2508868239, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_bat" }, { "Description", "Aluminum baseball bat with leather grip. Lightweight yet powerful for all you big hitters out there." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_BAT" }, { "Name", "Baseball Bat" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_BAT" }, { "HashKey", "WEAPON_BAT" }, } },
            { 1141786504, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_gclub" }, { "Description", "Standard length, mid iron golf club with rubber grip for a lethal short game." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_GOLFCLUB" }, { "Name","Golf Club" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_GOLFCLUB" }, { "HashKey", "WEAPON_GOLFCLUB" }, } },
            { 100416529, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sr_sniperrifle" }, { "Description", "Standard sniper rifle. Ideal for situations that require accuracy at long range. Limitations include slow reload speed and very low rate of fire." }, { "AmmoType", "AMMO_SNIPER" }, { "Group", "GROUP_SNIPER" }, { "DefaultClipSize", "10" }, { "NameGXT", "WT_SNIP_RIF"}, { "Name", "Sniper Rifle" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SNIP_RIF" }, { "HashKey", "WEAPON_SNIPERRIFLE" }, } },
            { 1672152130, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_lr_homing" }, { "Description", "Infrared guided fire-and-forget missile launcher. For all your moving target needs.Part of the Festive Surprise." }, { "AmmoType", "AMMO_HOMINGLAUNCHER" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_HOMLNCH" }, { "Name", "Homing Launcher"}, { "DLC", "mpchristmas2" }, { "DescriptionGXT", "WTD_HOMLNCH" }, { "HashKey", "WEAPON_HOMINGLAUNCHER" }, } },
            { 4024951519, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_assaultsmg" }, { "Description", "A high-capacity submachine gun that is both compact and lightweight. Holds up to 30 bullets in one magazine." }, { "AmmoType", "AMMO_SMG" }, { "Group", "GROUP_SMG" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_SMG_ASL" }, { "Name", "Assault SMG" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SMG_ASL" }, { "HashKey", "WEAPON_ASSAULTSMG" }, } },
            { 961495388, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_assaultriflemk2" }, { "Description", "The definitive revision of an all-time classic: all it takes is a little work, and looks can kill after all." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_RIFLE_ASL2" }, { "Name", "Assault Rifle Mk II" }, { "DLC", "mpgunrunning" }, { "DescriptionGXT", "WTD_RIFLE_ASL2" }, { "HashKey", "WEAPON_ASSAULTRIFLE_MK2" }, } },
            { 3675956304, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_compactsmg" }, { "Description", "This fully automatic is the snare drum to your twin-engine V8 bass: no drive-bysounds quite right without it. Part of Lowriders." }, { "AmmoType", "AMMO_SMG" }, { "Group","GROUP_SMG" }, { "DefaultClipSize", "12" }, { "NameGXT", "WT_MCHPIST" }, { "Name", "Machine Pistol" }, { "DLC", "mplowrider" }, { "DescriptionGXT", "WTD_MCHPIST" }, { "HashKey", "WEAPON_MACHINEPISTOL" }, } },
            { 4256991824, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_grenadesmoke" }, { "Description", "Tear gas grenade, particularly effective at incapacitating multiple assailants. Sustained exposure can be lethal." }, { "AmmoType", "AMMO_SMOKEGRENADE" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_GNADE_SMK" }, { "Name", "Tear Gas"}, { "DLC", "core" }, { "DescriptionGXT", "WTD_GNADE_SMK" }, { "HashKey", "WEAPON_SMOKEGRENADE" }, } },
            { 4208062921, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_carbineriflemk2" },{ "Description", "This is bespoke, artisan firepower: you couldn't deliver a hail of bulletswith more love and care if you inserted them by hand." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_RIFLE_CBN2" }, { "Name", "Carbine Rifle Mk II" }, { "DLC", "mpgunrunning" }, { "DescriptionGXT", "WTD_RIFLE_CBN2" },{ "HashKey", "WEAPON_CARBINERIFLE_MK2" }, } },
            { 2138347493, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_lr_firework" }, { "Description", "Put the flair back in flare with this firework launcher, guaranteed to raise someoohs and aahs from the crowd. Part of the Independence Day Special." }, { "AmmoType", "AMMO_FIREWORK" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_FIREWRK" }, { "Name", "Firework Launcher" }, { "DLC", "mpindependence" }, { "DescriptionGXT", "WTD_FIREWRK" }, { "HashKey", "WEAPON_FIREWORK" }, } },
            { 2024373456, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_smgmk2" }, { "Description", "Lightweight, compact, with a rate of fire to die very messily for: turn any confined space into a kill box at the click of a well-oiled trigger." }, { "AmmoType", "AMMO_SMG" },{ "Group", "GROUP_SMG" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_SMG2" }, { "Name", "SMG Mk II" }, { "DLC", "mpgunrunning" }, { "DescriptionGXT", "WTD_SMG2" }, { "HashKey", "WEAPON_SMG_MK2" }, } },
            { 3231910285, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_specialcarbine" }, { "Description", "Combining accuracy, maneuverability and low recoil, this is an extremely versatile assault rifle for any combat situation. Part of The Business Update." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_SPCARBINE" }, { "Name", "Special Carbine" }, { "DLC", "mpbusiness" }, { "DescriptionGXT", "WTD_SPCARBINE" }, { "HashKey", "WEAPON_SPECIALCARBINE" }, } },
            { 727643628, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_ceramic_pistol" }, {"Description", "Not your grandma's ceramics. Although this pint-sized pistol is small enoughto fit into her purse and won't set off a metal detector." }, { "AmmoType", "AMMO_PISTOL" },{ "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "12" }, { "NameGXT", "WT_CERPST" }, { "Name", "Ceramic Pistol" }, { "DLC", "mpheist3" }, { "DescriptionGXT", "WTD_CERPST" }, { "HashKey", "WEAPON_CERAMICPISTOL" }, } },
            { 1119849093, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_mg_minigun" }, { "Description", "A devastating 6-barrel machine gun that features Gatling-style rotating barrels. Very high rate of fire (2000 to 6000 rounds per minute)." }, { "AmmoType", "AMMO_MINIGUN" }, {"Group", "GROUP_HEAVY" }, { "DefaultClipSize", "15000" }, { "NameGXT", "WT_MINIGUN" }, { "Name", "Minigun" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_MINIGUN" }, { "HashKey", "WEAPON_MINIGUN" }, } },
            { 2937143193, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_AR_ADVANCEDRIFLE" }, {"Description", "The most lightweight and compact of all assault rifles, without compromisingaccuracy and rate of fire." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_RIFLE_ADV" }, { "Name", "Advanced Rifle" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_RIFLE_ADV" }, { "HashKey", "WEAPON_ADVANCEDRIFLE" }, } },
            { 1834241177, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_railgun" }, { "Description", "All you need to know is - magnets, and it does horrible things to the things it's pointed at. Exclusive content for returning players." }, { "AmmoType", "AMMO_RAILGUN" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_RAILGUN" }, { "Name", "Railgun" }, { "DLC", "spupgrade" }, { "DescriptionGXT", "WTD_RAILGUN" }, { "HashKey", "WEAPON_RAILGUN" }, } },
            { 1432025498, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_pumpshotgunmk2" }, { "Description", "Only one thing pumps more action than a pump action: watch out, the recoil is almost as deadly as the shot." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "8" }, { "NameGXT", "WT_SG_PMP2" }, { "Name", "Pump Shotgun Mk II"}, { "DLC", "mpchristmas2017" }, { "DescriptionGXT", "WTD_SG_PMP2" }, { "HashKey", "WEAPON_PUMPSHOTGUN_MK2" }, } },
            { 1198879012, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_flaregun" }, { "Description", "Use to signal distress or drunken excitement. Warning: pointing directly at individuals may cause spontaneous combustion. Part of The Heists Update." }, { "AmmoType", "AMMO_FLAREGUN" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_FLAREGUN" }, { "Name", "Flare Gun" }, { "DLC", "mpheist" }, { "DescriptionGXT", "WTD_FLAREGUN" }, { "HashKey", "WEAPON_FLAREGUN" }, } },
            { 4019527611, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_doublebarrel" }, { "Description", "Do one thing, do it well. Who needs a high rate of fire when your first shot turns the other guy into a fine mist? Part of Lowriders: Custom Classics." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "2" }, { "NameGXT", "WT_DBSHGN" }, { "Name", "Double Barrel Shotgun" }, { "DLC", "mplowrider2" }, { "DescriptionGXT", "WTD_DBSHGN" }, { "HashKey", "WEAPON_DBSHOTGUN" }, } },
            { 3125143736, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_pipebomb" }, { "Description", "Remember, it doesn't count as an IED when you buy it in a store and use it in a first world country. Part of Bikers." }, { "AmmoType", "AMMO_PIPEBOMB" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_PIPEBOMB" }, { "Name", "Pipe Bomb" }, {"DLC", "mpbiker" }, { "DescriptionGXT", "WTD_PIPEBOMB" }, { "HashKey", "WEAPON_PIPEBOMB" }, } },
            { 3686625920, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_mg_combatmgmk2" }, { "Description", "You can never have too much of a good thing: after all, if the first shot counts, then the next hundred or so must count for double." }, { "AmmoType", "AMMO_MG" }, { "Group", "GROUP_MG" }, { "DefaultClipSize", "100" }, { "NameGXT", "WT_MG_CBT2" }, { "Name", "Combat MG Mk II" }, { "DLC", "mpgunrunning" }, { "DescriptionGXT", "WTD_MG_CBT2" }, { "HashKey", "WEAPON_COMBATMG_MK2" }, } },
            { 3800352039, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_assaultshotgun" }, { "Description", "Fully automatic shotgun with 8 round magazine and high rate of fire." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "8" }, { "NameGXT", "WT_SG_ASL" }, { "Name", "Assault Shotgun" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SG_ASL" }, { "HashKey", "WEAPON_ASSAULTSHOTGUN" }, } },
            { 940833800, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_stonehatchet" }, { "Description", "There's retro, there's vintage, and there's this. After 500 years of technological development and spiritual apocalypse, pre-Colombian chic is back." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_SHATCHET" }, { "Name", "Stone Hatchet"}, { "DLC", "mpbattle" }, { "DescriptionGXT", "WTD_SHATCHET" }, { "HashKey", "WEAPON_STONE_HATCHET" }, } },
            { 3415619887, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_revolvermk2" }, { "Description", "If you can lift it, this is the closest you'll get to shooting someone with a freight train." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_REVOLVER2" }, { "Name", "Heavy Revolver Mk II" }, { "DLC", "mpchristmas2017" }, { "DescriptionGXT", "WTD_REVOLVER2" }, { "HashKey", "WEAPON_REVOLVER_MK2" }, } },
            { 487013001, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_pumpshotgun" }, { "Description", "Standard shotgun ideal for short-range combat. A high-projectile spread makes up for its lower accuracy at long range." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "8" }, { "NameGXT", "WT_SG_PMP" }, { "Name", "Pump Shotgun"}, { "DLC", "core" }, { "DescriptionGXT", "WTD_SG_PMP" }, { "HashKey", "WEAPON_PUMPSHOTGUN" }, } },
            { 2874559379, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_apmine" }, { "Description", "Leave a present for your friends with these motion sensor landmines. Short delay after activation. Part of the Festive Surprise." }, { "AmmoType", "AMMO_PROXMINE" }, { "Group","GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_PRXMINE" }, { "Name", "Proximity Mine" }, { "DLC", "mpchristmas2" }, { "DescriptionGXT", "WTD_PRXMINE" }, { "HashKey", "WEAPON_PROXMINE" }, } },
            { 3173288789, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_minismg" }, { "Description", "Increasingly popular since the marketing team looked beyond spec ops units and started caring about the little guys in low income areas. Part of Bikers." }, { "AmmoType", "AMMO_SMG" }, { "Group", "GROUP_SMG" }, { "DefaultClipSize", "20" }, { "NameGXT", "WT_MINISMG" },{ "Name", "Mini SMG" }, { "DLC", "mpbiker" }, { "DescriptionGXT", "WTD_MINISMG" }, { "HashKey", "WEAPON_MINISMG" }, } },
            { 177293209, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sr_heavysnipermk2" }, {"Description", "Far away, yet always intimate: if you're looking for a secure foundation forthat long-distance relationship, this is it." }, { "AmmoType", "AMMO_SNIPER" }, { "Group", "GROUP_SNIPER" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_SNIP_HVY2" }, { "Name", "Heavy Sniper Mk II" }, { "DLC", "mpgunrunning" }, { "DescriptionGXT", "WTD_SNIP_HVY2" }, { "HashKey", "WEAPON_HEAVYSNIPER_MK2" }, } },
            { 3756226112, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_switchblade" }, { "Description", "From your pocket to hilt-deep in the other guy's ribs in under a second: folding knives will never go out of style. Part of Executives and Other Criminals." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_SWBLADE" }, { "Name", "Switchblade" }, { "DLC", "mpapartment" }, { "DescriptionGXT", "WTD_SWBLADE" }, { "HashKey", "WEAPON_SWITCHBLADE" }, } },
            { 3713923289, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_machette_lr" }, { "Description", "America's West African arms trade isn't just about giving. Rediscover the simple life with this rusty cleaver. Part of Lowriders." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_MACHETE" }, { "Name", "Machete" }, { "DLC", "mplowrider" }, { "DescriptionGXT", "WTD_MACHETE" }, { "HashKey", "WEAPON_MACHETE" }, } },
            { 3696079510, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_PI_SingleShot" }, { "Description", "Not for the risk averse. Make it count as you'll be reloading as much as you shoot. Part of The Ill-Gotten Gains Update Part 2." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_MKPISTOL" }, { "Name", "Marksman Pistol" }, { "DLC", "mpluxe2" }, { "DescriptionGXT", "WTD_MKPISTOL" }, { "HashKey", "WEAPON_MARKSMANPISTOL" }, } },
            { 2441047180, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_wep2_gun" }, { "Description", "A true museum piece. You want to know how the West was won - slow reload speeds and a whole heap of bloodshed." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" },{ "DefaultClipSize", "6" }, { "NameGXT", "WT_REV_NV" }, { "Name", "Navy Revolver" }, { "DLC", "mpheist3" }, { "DescriptionGXT", "WTD_REV_NV" }, { "HashKey", "WEAPON_NAVYREVOLVER" }, } },
            { 615608432, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_molotov" }, { "Description", "Crude yet highly effective incendiary weapon. No happy hour with this cocktail." },{ "AmmoType", "AMMO_MOLOTOV" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_MOLOTOV" }, { "Name", "Molotov" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_MOLOTOV" }, { "HashKey", "WEAPON_MOLOTOV" }, } },
            { 2460120199, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_dagger" }, { "Description", "You've been rocking the pirate-chic look for a while, but no vicious weapon to complete the look? Get this dagger with guarded hilt. Part of The \"I'm Not a Hipster\" Update." },{ "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_DAGGER" }, { "Name", "Antique Cavalry Dagger" }, { "DLC", "mphipster" }, { "DescriptionGXT", "WTD_DAGGER" }, { "HashKey", "WEAPON_DAGGER" }, } },
            { 2726580491, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_lr_grenadelauncher" },{ "Description", "A compact, lightweight grenade launcher with semi-automatic functionality.Holds up to 10 rounds." }, { "AmmoType", "AMMO_GRENADELAUNCHER" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "10" }, { "NameGXT", "WT_GL" }, { "Name", "Grenade Launcher" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_GL" }, { "HashKey", "WEAPON_GRENADELAUNCHER" }, } },
            { 3523564046, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_heavypistol" }, { "Description", "The heavyweight champion of the magazine fed, semi-automatic handgun world. Delivers a serious forearm workout every time. Part of The Business Update." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "18" }, { "NameGXT", "WT_HVYPISTOL" }, { "Name", "Heavy Pistol" }, { "DLC", "mpbusiness" }, { "DescriptionGXT", "WTD_HVYPISTOL" }, { "HashKey", "WEAPON_HEAVYPISTOL" }, } },
            { 3441901897, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_battleaxe" }, { "Description", "If it's good enough for medieval foot soldiers, modern border guards and pushy soccer moms, it's good enough for you. Part of Bikers." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_BATTLEAXE" }, { "Name", "Battle Axe" }, { "DLC", "mpbiker" }, { "DescriptionGXT", "WTD_BATTLEAXE" }, { "HashKey", "WEAPON_BATTLEAXE" }, } },
            { 2578377531, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_PI_PISTOL50" }, { "Description", "High-impact pistol that delivers immense power but with extremely strong recoil. Holds 9 rounds in magazine." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, {"DefaultClipSize", "9" }, { "NameGXT", "WT_PIST_50" }, { "Name", "Pistol .50" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_PIST_50" }, { "HashKey", "WEAPON_PISTOL50" }, } },
            { 2144741730, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_mg_combatmg" }, { "Description", "Lightweight, compact machine gun that combines excellent maneuverability with a high rate of fire to devastating effect." }, { "AmmoType", "AMMO_MG" }, { "Group", "GROUP_MG" }, { "DefaultClipSize", "100" }, { "NameGXT", "WT_MG_CBT" }, { "Name", "Combat MG" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_MG_CBT" }, { "HashKey", "WEAPON_COMBATMG" }, } },
            { 3220176749, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_AR_ASSAULTRIFLE" }, { "Description", "This standard assault rifle boasts a large capacity magazine and long distance accuracy." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_RIFLE_ASL" }, { "Name", "Assault Rifle" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_RIFLE_ASL" }, { "HashKey", "WEAPON_ASSAULTRIFLE" }, } },
            { 3126027122, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ch_jerrycan" }, { "Description", "" }, { "AmmoType", "AMMO_HAZARDCAN" }, { "Group", "GROUP_PETROLCAN" }, { "DefaultClipSize", "4500" }, { "NameGXT", "WT_HAZARDCAN" }, { "Name", "Hazardous Jerry Can" }, { "DLC", "mpheist3" }, { "DescriptionGXT", "WTD_HAZARDCAN" }, { "HashKey", "WEAPON_HAZARDCAN" }, } },
            { 3249783761, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_revolver" }, { "Description", "A handgun with enough stopping power to drop a crazed rhino, and heavy enough to beat it to death if you're out of ammo. Part of Executives and Other Criminals." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_REVOLVER" }, { "Name", "Heavy Revolver" }, { "DLC", "mpapartment" }, { "DescriptionGXT", "WTD_REVOLVER" }, { "HashKey", "WEAPON_REVOLVER" }, } },
            { 3219281620, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_pistolmk2" }, { "Description", "Balance, simplicity, precision: nothing keeps the peace like an extended barrel in the other guy's mouth." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "12" }, { "NameGXT", "WT_PIST2" }, { "Name", "Pistol Mk II" }, { "DLC", "mpgunrunning" }, { "DescriptionGXT", "WTD_PIST2" }, { "HashKey", "WEAPON_PISTOL_MK2" }, } },
            { 883325847, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_am_jerrycan" }, { "Description", "Leaves a trail of gasoline that can be ignited." }, { "AmmoType", "AMMO_PETROLCAN"}, { "Group", "GROUP_PETROLCAN" }, { "DefaultClipSize", "4500" }, { "NameGXT", "WT_PETROL" }, { "Name", "Jerry Can" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_PETROL" }, { "HashKey", "WEAPON_PETROLCAN" }, } },
            { 3056410471, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_mg_sminigun" }, { "Description", "Republican Space Ranger Special. GO AHEAD, SAY I'M COMPENSATING FOR SOMETHING. I DARE YOU." }, { "AmmoType", "AMMO_MINIGUN" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "15000" }, { "NameGXT", "WT_RAYMINIGUN" }, { "Name", "Widowmaker" }, { "DLC", "mpchristmas2018" }, { "DescriptionGXT", "WTD_RAYMINIGUN" }, { "HashKey", "WEAPON_RAYMINIGUN" }, } },
            { 3218215474, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_sns_pistol" }, { "Description", "Like condoms or hairspray, this fits in your pocket for a night out in a Vinewood club. It's half as accurate as a champagne cork but twice as deadly. Part of the Beach BumPack." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_SNSPISTOL" }, { "Name", "SNS Pistol" }, { "DLC", "mpbeach" }, { "DescriptionGXT", "WTD_SNSPISTOL" }, { "HashKey", "WEAPON_SNSPISTOL" }, } },
            { 4192643659, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_bottle" }, { "Description", "It's not clever and it's not pretty but, most of the time, neither is the guy coming at you with a knife. When all else fails, this gets the job done. Part of the Beach Bum Pack." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_BOTTLE" }, { "Name", "Bottle" }, { "DLC", "mpbeach" }, { "DescriptionGXT", "WTD_BOTTLE" }, { "HashKey", "WEAPON_BOTTLE" }, } },
            { 2828843422, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_musket" }, { "Description", "Armed with nothing but muskets and a superiority complex, the Brits took over half the world. Own the gun that built an empire. Part of the Independence Day Special." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SNIPER" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_MUSKET" }, { "Name", "Musket" }, { "DLC", "mpindependence" }, { "DescriptionGXT", "WTD_MUSKET" }, { "HashKey", "WEAPON_MUSKET" }, } },
            { 137902532, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_vintage_pistol" }, {"Description", "What you really need is a more recognizable gun. Stand out from the crowd atan armed robbery with this engraved pistol. Part of The \"I'm Not a Hipster\" Update." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "7" }, { "NameGXT", "WT_VPISTOL" }, { "Name", "Vintage Pistol" }, { "DLC", "mphipster" }, { "DescriptionGXT","WTD_VPISTOL" }, { "HashKey", "WEAPON_VINTAGEPISTOL" }, } },
            { 1593441988, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_PI_COMBATPISTOL" }, { "Description", "A compact, lightweight, semi-automatic pistol designed for law enforcement and personal defense. 12-round magazine with option to extend to 16 rounds." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "12" }, { "NameGXT", "WT_PIST_CBT" }, { "Name", "Combat Pistol" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_PIST_CBT"}, { "HashKey", "WEAPON_COMBATPISTOL" }, } },
            { 1785463520, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sr_marksmanriflemk2" }, { "Description", "Known in military circles as The Dislocator, this mod set will destroy both the target and your shoulder, in that order." }, { "AmmoType", "AMMO_SNIPER" }, { "Group","GROUP_SNIPER" }, { "DefaultClipSize", "8" }, { "NameGXT", "WT_MKRIFLE2" }, { "Name", "Marksman Rifle Mk II" }, { "DLC", "mpchristmas2017" }, { "DescriptionGXT", "WTD_MKRIFLE2" }, { "HashKey", "WEAPON_MARKSMANRIFLE_MK2" }, } },
            { 2634544996, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_mg_mg" }, { "Description", "General purpose machine gun that combines rugged design with dependable performance. Long range penetrative power. Very effective against large groups." }, { "AmmoType", "AMMO_MG" }, { "Group", "GROUP_MG" }, { "DefaultClipSize", "54" }, { "NameGXT", "WT_MG" }, { "Name", "MG" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_MG" }, { "HashKey", "WEAPON_MG" }, } },
            { 736523883, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_smg" }, { "Description", "This is known as a good all-round submachine gun. Lightweight with an accurate sight and 30-round magazine capacity." }, { "AmmoType", "AMMO_SMG" }, { "Group", "GROUP_SMG" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_SMG" }, { "Name", "SMG" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SMG" }, { "HashKey", "WEAPON_SMG" }, } },
            { 205991906, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sr_heavysniper" }, { "Description", "Features armor-piercing rounds for heavy damage. Comes with laser scope as standard." }, { "AmmoType", "AMMO_SNIPER" }, { "Group", "GROUP_SNIPER" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_SNIP_HVY" }, { "Name", "Heavy Sniper" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SNIP_HVY" }, { "HashKey", "WEAPON_HEAVYSNIPER" }, } },
            { 2343591895, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_flashlight" }, { "Description", "Intensify your fear of the dark with this short range, battery-powered light source. Handy for blunt force trauma. Part of The Halloween Surprise." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_FLASHLIGHT" }, { "Name", "Flashlight" }, { "DLC", "mphalloween" }, { "DescriptionGXT", "WTD_FLASHLIGHT" }, { "HashKey", "WEAPON_FLASHLIGHT" }, } },
            { 1198256469, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_srifle" }, { "Description", "Republican Space Ranger Special. If you want to turn a little green man into littlegreen goo, this is the only American way to do it." }, { "AmmoType", "AMMO_MG" }, { "Group","GROUP_MG" }, { "DefaultClipSize", "9999" }, { "NameGXT", "WT_RAYCARBINE" }, { "Name", "Unholy Hellbringer" }, { "DLC", "mpchristmas2018" }, { "DescriptionGXT", "WTD_RAYCARBINE" }, { "HashKey", "WEAPON_RAYCARBINE" }, } },
            { 600439132, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_am_baseball" }, { "Description", "" }, { "AmmoType", "AMMO_BALL" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_BALL" }, { "Name", "Ball" }, { "DLC", "core" }, { "DescriptionGXT","WTD_BALL" }, { "HashKey", "WEAPON_BALL" }, } },
            { 2132975508, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_bullpuprifle" }, { "Description", "The latest Chinese import taking America by storm, this rifle is known for its balanced handling. Lightweight and very controllable in automatic fire. Part of The High Life Update." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize","30" }, { "NameGXT", "WT_BULLRIFLE" }, { "Name", "Bullpup Rifle" }, { "DLC", "mpbusiness2" }, { "DescriptionGXT", "WTD_BULLRIFLE" }, { "HashKey", "WEAPON_BULLPUPRIFLE" }, } },
            { 2640438543, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_bullpupshotgun" }, { "Description", "More than makes up for its slow, pump-action rate of fire with its range and spread.  Decimates anything in its projectile path." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "14" }, { "NameGXT", "WT_SG_BLP" }, { "Name","Bullpup Shotgun" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SG_BLP" }, { "HashKey", "WEAPON_BULLPUPSHOTGUN" }, } },
            { 125959754, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_lr_compactgl" }, { "Description", "Focus groups using the regular model suggested it was too accurate and found it awkward to use with one hand on the throttle. Easy fix. Part of Bikers." }, { "AmmoType", "AMMO_GRENADELAUNCHER" }, { "Group", "GROUP_HEAVY" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_CMPGL" }, { "Name", "Compact Grenade Launcher" }, { "DLC", "mpbiker" }, { "DescriptionGXT","WTD_CMPGL" }, { "HashKey", "WEAPON_COMPACTLAUNCHER" }, } },
            { 2210333304, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_AR_CARBINERIFLE" }, { "Description", "Combining long distance accuracy with a high-capacity magazine, the carbine rifle can be relied on to make the hit." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_RIFLE_CBN" }, { "Name", "Carbine Rifle"}, { "DLC", "core" }, { "DescriptionGXT", "WTD_RIFLE_CBN" }, { "HashKey", "WEAPON_CARBINERIFLE" }, } },
            { 419712736, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_wrench" }, { "Description", "Perennial favourite of apocalyptic survivalists and violent fathers the world over, apparently it also doubles as some kind of tool. Part of Bikers." }, { "Group", "GROUP_MELEE"}, { "DefaultClipSize", "0" }, { "NameGXT", "WT_WRENCH" }, { "Name", "Pipe Wrench" }, { "DLC", "mpbiker" }, { "DescriptionGXT", "WTD_WRENCH" }, { "HashKey", "WEAPON_WRENCH" }, } },
            { 2548703416, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_pi_wep1_gun" }, { "Description", "Because sometimes revenge is a dish best served six times, in quick succession, right between the eyes. Part of The Doomsday Heist." }, { "AmmoType", "AMMO_PISTOL" }, { "Group", "GROUP_PISTOL" }, { "DefaultClipSize", "6" }, { "NameGXT", "WT_REV_DA" }, { "Name", "Double-Action Revolver" }, { "DLC", "mpchristmas2017" }, { "DescriptionGXT", "WTD_REV_DA" }, { "HashKey", "WEAPON_DOUBLEACTION" }, } },
            { 2578778090, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_knife_01" }, { "Description", "This carbon steel 7\" bladed knife is dual edged with a serrated spine to provide improved stabbing and thrusting capabilities." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_KNIFE" }, { "Name", "Knife" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_KNIFE" }, { "HashKey", "WEAPON_KNIFE" }, } },
            { 2526821735, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ar_specialcarbinemk2" }, { "Description", "The jack of all trades just got a serious upgrade: bow to the master." }, { "AmmoType", "AMMO_RIFLE" }, { "Group", "GROUP_RIFLE" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_SPCARBINE2" }, { "Name", "Special Carbine Mk II" }, { "DLC", "mpchristmas2017" }, { "DescriptionGXT", "WTD_SPCARBINE2" }, { "HashKey", "WEAPON_SPECIALCARBINE_MK2" }, } },
            { 2227010557, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_crowbar" }, { "Description", "Heavy-duty crowbar forged from high quality, tempered steel for that extra leverage you need to get the job done." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, {"NameGXT", "WT_CROWBAR" }, { "Name", "Crowbar" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_CROWBAR" }, { "HashKey", "WEAPON_CROWBAR" }, } },
            { 1737195953, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_me_nightstick" }, { "Description", "24\" polycarbonate side-handled nightstick." }, { "Group", "GROUP_MELEE" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_NGTSTK" }, { "Name", "Nightstick" }, { "DLC", "core"}, { "DescriptionGXT", "WTD_NGTSTK" }, { "HashKey", "WEAPON_NIGHTSTICK" }, } },
            { 2694266206, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_ex_grenadesmoke" }, { "Description", "" }, { "AmmoType", "AMMO_BZGAS" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize", "1" }, { "NameGXT", "WT_BZGAS" }, { "Name", "BZ Gas" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_BZGAS" }, { "HashKey", "WEAPON_BZGAS" }, } },
            { 3638508604, new Dictionary<string, dynamic>(){ { "ModelHashKey", "W_ME_Knuckle" }, { "Description", "Perfect for knocking out gold teeth, or as a gift to the trophy partner who has everything. Part of The Ill-Gotten Gains Update Part 2." }, { "Group", "GROUP_UNARMED" }, { "DefaultClipSize", "0" }, { "NameGXT", "WT_KNUCKLE" }, { "Name", "Knuckle Duster" }, { "DLC", "mpluxe2" }, { "DescriptionGXT", "WTD_KNUCKLE" }, { "HashKey", "WEAPON_KNUCKLE" }, } },
            { 317205821, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_sweeper" }, { "Description", "How many effective tools for riot control can you tuck into your pants? OK, two. But this is the other one. Part of Bikers." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "10" }, { "NameGXT", "WT_AUTOSHGN" }, { "Name", "Sweeper Shotgun" }, { "DLC", "mpbiker" }, { "DescriptionGXT", "WTD_AUTOSHGN" }, { "HashKey", "WEAPON_AUTOSHOTGUN" }, } },
            { 2017895192, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sg_sawnoff" }, { "Description", "This single-barrel, sawed-off shotgun compensates for its low range and ammo capacity with devastating efficiency in close combat." }, { "AmmoType", "AMMO_SHOTGUN" }, { "Group", "GROUP_SHOTGUN" }, { "DefaultClipSize", "8" }, { "NameGXT", "WT_SG_SOF" }, { "Name", "Sawed-Off Shotgun" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SG_SOF" }, { "HashKey", "WEAPON_SAWNOFFSHOTGUN" }, } },
            { 324215364, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_microsmg" }, { "Description", "Combines compact design with a high rate of fire at approximately 700-900 rounds per minute." }, { "AmmoType", "AMMO_SMG" }, { "Group", "GROUP_SMG" }, { "DefaultClipSize", "16" }, { "NameGXT", "WT_SMG_MCR" }, { "Name", "Micro SMG" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_SMG_MCR" }, { "HashKey", "WEAPON_MICROSMG" }, } },
            { 1627465347, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_sb_gusenberg" }, { "Description", "Complete your look with a Prohibition gun. Looks great being fired from an Albany Roosevelt or paired with a pinstripe suit. Part of the Valentine's Day Massacre Special." }, { "AmmoType", "AMMO_MG" }, { "Group", "GROUP_MG" }, { "DefaultClipSize", "30" }, { "NameGXT", "WT_GUSNBRG" }, { "Name", "Gusenberg Sweeper" }, { "DLC", "mpvalentines" }, { "DescriptionGXT", "WTD_GUSNBRG" }, { "HashKey", "WEAPON_GUSENBERG" }, } },
            { 1233104067, new Dictionary<string, dynamic>(){ { "ModelHashKey", "w_am_flare" }, { "Description", "" }, { "AmmoType", "AMMO_FLARE" }, { "Group", "GROUP_THROWN" }, { "DefaultClipSize","1" }, { "NameGXT", "WT_FLARE" }, { "Name", "Flare" }, { "DLC", "core" }, { "DescriptionGXT", "WTD_FLARE" }, { "HashKey", "WEAPON_FLARE" }, } },
        };
    }

    public enum eControl {
        ControlNextCamera = 0,
        ControlLookLeftRight = 1,
        ControlLookUpDown = 2,
        ControlLookUpOnly = 3,
        ControlLookDownOnly = 4,
        ControlLookLeftOnly = 5,
        ControlLookRightOnly = 6,
        ControlCinematicSlowMo = 7,
        ControlFlyUpDown = 8,
        ControlFlyLeftRight = 9,
        ControlScriptedFlyZUp = 10,
        ControlScriptedFlyZDown = 11,
        ControlWeaponWheelUpDown = 12,
        ControlWeaponWheelLeftRight = 13,
        ControlWeaponWheelNext = 14,
        ControlWeaponWheelPrev = 15,
        ControlSelectNextWeapon = 16,
        ControlSelectPrevWeapon = 17,
        ControlSkipCutscene = 18,
        ControlCharacterWheel = 19,
        ControlMultiplayerInfo = 20,
        ControlSprint = 21,
        ControlJump = 22,
        ControlEnter = 23,
        ControlAttack = 24,
        ControlAim = 25,
        ControlLookBehind = 26,
        ControlPhone = 27,
        ControlSpecialAbility = 28,
        ControlSpecialAbilitySecondary = 29,
        ControlMoveLeftRight = 30,
        ControlMoveUpDown = 31,
        ControlMoveUpOnly = 32,
        ControlMoveDownOnly = 33,
        ControlMoveLeftOnly = 34,
        ControlMoveRightOnly = 35,
        ControlDuck = 36,
        ControlSelectWeapon = 37,
        ControlPickup = 38,
        ControlSniperZoom = 39,
        ControlSniperZoomInOnly = 40,
        ControlSniperZoomOutOnly = 41,
        ControlSniperZoomInSecondary = 42,
        ControlSniperZoomOutSecondary = 43,
        ControlCover = 44,
        ControlReload = 45,
        ControlTalk = 46,
        ControlDetonate = 47,
        ControlHUDSpecial = 48,
        ControlArrest = 49,
        ControlAccurateAim = 50,
        ControlContext = 51,
        ControlContextSecondary = 52,
        ControlWeaponSpecial = 53,
        ControlWeaponSpecial2 = 54,
        ControlDive = 55,
        ControlDropWeapon = 56,
        ControlDropAmmo = 57,
        ControlThrowGrenade = 58,
        ControlVehicleMoveLeftRight = 59,
        ControlVehicleMoveUpDown = 60,
        ControlVehicleMoveUpOnly = 61,
        ControlVehicleMoveDownOnly = 62,
        ControlVehicleMoveLeftOnly = 63,
        ControlVehicleMoveRightOnly = 64,
        ControlVehicleSpecial = 65,
        ControlVehicleGunLeftRight = 66,
        ControlVehicleGunUpDown = 67,
        ControlVehicleAim = 68,
        ControlVehicleAttack = 69,
        ControlVehicleAttack2 = 70,
        ControlVehicleAccelerate = 71,
        ControlVehicleBrake = 72,
        ControlVehicleDuck = 73,
        ControlVehicleHeadlight = 74,
        ControlVehicleExit = 75,
        ControlVehicleHandbrake = 76,
        ControlVehicleHotwireLeft = 77,
        ControlVehicleHotwireRight = 78,
        ControlVehicleLookBehind = 79,
        ControlVehicleCinCam = 80,
        ControlVehicleNextRadio = 81,
        ControlVehiclePrevRadio = 82,
        ControlVehicleNextRadioTrack = 83,
        ControlVehiclePrevRadioTrack = 84,
        ControlVehicleRadioWheel = 85,
        ControlVehicleHorn = 86,
        ControlVehicleFlyThrottleUp = 87,
        ControlVehicleFlyThrottleDown = 88,
        ControlVehicleFlyYawLeft = 89,
        ControlVehicleFlyYawRight = 90,
        ControlVehiclePassengerAim = 91,
        ControlVehiclePassengerAttack = 92,
        ControlVehicleSpecialAbilityFranklin = 93,
        ControlVehicleStuntUpDown = 94,
        ControlVehicleCinematicUpDown = 95,
        ControlVehicleCinematicUpOnly = 96,
        ControlVehicleCinematicDownOnly = 97,
        ControlVehicleCinematicLeftRight = 98,
        ControlVehicleSelectNextWeapon = 99,
        ControlVehicleSelectPrevWeapon = 100,
        ControlVehicleRoof = 101,
        ControlVehicleJump = 102,
        ControlVehicleGrapplingHook = 103,
        ControlVehicleShuffle = 104,
        ControlVehicleDropProjectile = 105,
        ControlVehicleMouseControlOverride = 106,
        ControlVehicleFlyRollLeftRight = 107,
        ControlVehicleFlyRollLeftOnly = 108,
        ControlVehicleFlyRollRightOnly = 109,
        ControlVehicleFlyPitchUpDown = 110,
        ControlVehicleFlyPitchUpOnly = 111,
        ControlVehicleFlyPitchDownOnly = 112,
        ControlVehicleFlyUnderCarriage = 113,
        ControlVehicleFlyAttack = 114,
        ControlVehicleFlySelectNextWeapon = 115,
        ControlVehicleFlySelectPrevWeapon = 116,
        ControlVehicleFlySelectTargetLeft = 117,
        ControlVehicleFlySelectTargetRight = 118,
        ControlVehicleFlyVerticalFlightMode = 119,
        ControlVehicleFlyDuck = 120,
        ControlVehicleFlyAttackCamera = 121,
        ControlVehicleFlyMouseControlOverride = 122,
        ControlVehicleSubTurnLeftRight = 123,
        ControlVehicleSubTurnLeftOnly = 124,
        ControlVehicleSubTurnRightOnly = 125,
        ControlVehicleSubPitchUpDown = 126,
        ControlVehicleSubPitchUpOnly = 127,
        ControlVehicleSubPitchDownOnly = 128,
        ControlVehicleSubThrottleUp = 129,
        ControlVehicleSubThrottleDown = 130,
        ControlVehicleSubAscend = 131,
        ControlVehicleSubDescend = 132,
        ControlVehicleSubTurnHardLeft = 133,
        ControlVehicleSubTurnHardRight = 134,
        ControlVehicleSubMouseControlOverride = 135,
        ControlVehiclePushbikePedal = 136,
        ControlVehiclePushbikeSprint = 137,
        ControlVehiclePushbikeFrontBrake = 138,
        ControlVehiclePushbikeRearBrake = 139,
        ControlMeleeAttackLight = 140,
        ControlMeleeAttackHeavy = 141,
        ControlMeleeAttackAlternate = 142,
        ControlMeleeBlock = 143,
        ControlParachuteDeploy = 144,
        ControlParachuteDetach = 145,
        ControlParachuteTurnLeftRight = 146,
        ControlParachuteTurnLeftOnly = 147,
        ControlParachuteTurnRightOnly = 148,
        ControlParachutePitchUpDown = 149,
        ControlParachutePitchUpOnly = 150,
        ControlParachutePitchDownOnly = 151,
        ControlParachuteBrakeLeft = 152,
        ControlParachuteBrakeRight = 153,
        ControlParachuteSmoke = 154,
        ControlParachutePrecisionLanding = 155,
        ControlMap = 156,
        ControlSelectWeaponUnarmed = 157,
        ControlSelectWeaponMelee = 158,
        ControlSelectWeaponHandgun = 159,
        ControlSelectWeaponShotgun = 160,
        ControlSelectWeaponSmg = 161,
        ControlSelectWeaponAutoRifle = 162,
        ControlSelectWeaponSniper = 163,
        ControlSelectWeaponHeavy = 164,
        ControlSelectWeaponSpecial = 165,
        ControlSelectCharacterMichael = 166,
        ControlSelectCharacterFranklin = 167,
        ControlSelectCharacterTrevor = 168,
        ControlSelectCharacterMultiplayer = 169,
        ControlSaveReplayClip = 170,
        ControlSpecialAbilityPC = 171,
        ControlPhoneUp = 172,
        ControlPhoneDown = 173,
        ControlPhoneLeft = 174,
        ControlPhoneRight = 175,
        ControlPhoneSelect = 176,
        ControlPhoneCancel = 177,
        ControlPhoneOption = 178,
        ControlPhoneExtraOption = 179,
        ControlPhoneScrollForward = 180,
        ControlPhoneScrollBackward = 181,
        ControlPhoneCameraFocusLock = 182,
        ControlPhoneCameraGrid = 183,
        ControlPhoneCameraSelfie = 184,
        ControlPhoneCameraDOF = 185,
        ControlPhoneCameraExpression = 186,
        ControlFrontendDown = 187,
        ControlFrontendUp = 188,
        ControlFrontendLeft = 189,
        ControlFrontendRight = 190,
        ControlFrontendRdown = 191,
        ControlFrontendRup = 192,
        ControlFrontendRleft = 193,
        ControlFrontendRright = 194,
        ControlFrontendAxisX = 195,
        ControlFrontendAxisY = 196,
        ControlFrontendRightAxisX = 197,
        ControlFrontendRightAxisY = 198,
        ControlFrontendPause = 199,
        ControlFrontendPauseAlternate = 200,
        ControlFrontendAccept = 201,
        ControlFrontendCancel = 202,
        ControlFrontendX = 203,
        ControlFrontendY = 204,
        ControlFrontendLb = 205,
        ControlFrontendRb = 206,
        ControlFrontendLt = 207,
        ControlFrontendRt = 208,
        ControlFrontendLs = 209,
        ControlFrontendRs = 210,
        ControlFrontendLeaderboard = 211,
        ControlFrontendSocialClub = 212,
        ControlFrontendSocialClubSecondary = 213,
        ControlFrontendDelete = 214,
        ControlFrontendEndscreenAccept = 215,
        ControlFrontendEndscreenExpand = 216,
        ControlFrontendSelect = 217,
        ControlScriptLeftAxisX = 218,
        ControlScriptLeftAxisY = 219,
        ControlScriptRightAxisX = 220,
        ControlScriptRightAxisY = 221,
        ControlScriptRUp = 222,
        ControlScriptRDown = 223,
        ControlScriptRLeft = 224,
        ControlScriptRRight = 225,
        ControlScriptLB = 226,
        ControlScriptRB = 227,
        ControlScriptLT = 228,
        ControlScriptRT = 229,
        ControlScriptLS = 230,
        ControlScriptRS = 231,
        ControlScriptPadUp = 232,
        ControlScriptPadDown = 233,
        ControlScriptPadLeft = 234,
        ControlScriptPadRight = 235,
        ControlScriptSelect = 236,
        ControlCursorAccept = 237,
        ControlCursorCancel = 238,
        ControlCursorX = 239,
        ControlCursorY = 240,
        ControlCursorScrollUp = 241,
        ControlCursorScrollDown = 242,
        ControlEnterCheatCode = 243,
        ControlInteractionMenu = 244,
        ControlMpTextChatAll = 245,
        ControlMpTextChatTeam = 246,
        ControlMpTextChatFriends = 247,
        ControlMpTextChatCrew = 248,
        ControlPushToTalk = 249,
        ControlCreatorLS = 250,
        ControlCreatorRS = 251,
        ControlCreatorLT = 252,
        ControlCreatorRT = 253,
        ControlCreatorMenuToggle = 254,
        ControlCreatorAccept = 255,
        ControlCreatorDelete = 256,
        ControlAttack2 = 257,
        ControlRappelJump = 258,
        ControlRappelLongJump = 259,
        ControlRappelSmashWindow = 260,
        ControlPrevWeapon = 261,
        ControlNextWeapon = 262,
        ControlMeleeAttack1 = 263,
        ControlMeleeAttack2 = 264,
        ControlWhistle = 265,
        ControlMoveLeft = 266,
        ControlMoveRight = 267,
        ControlMoveUp = 268,
        ControlMoveDown = 269,
        ControlLookLeft = 270,
        ControlLookRight = 271,
        ControlLookUp = 272,
        ControlLookDown = 273,
        ControlSniperZoomIn = 274,
        ControlSniperZoomOut = 275,
        ControlSniperZoomInAlternate = 276,
        ControlSniperZoomOutAlternate = 277,
        ControlVehicleMoveLeft = 278,
        ControlVehicleMoveRight = 279,
        ControlVehicleMoveUp = 280,
        ControlVehicleMoveDown = 281,
        ControlVehicleGunLeft = 282,
        ControlVehicleGunRight = 283,
        ControlVehicleGunUp = 284,
        ControlVehicleGunDown = 285,
        ControlVehicleLookLeft = 286,
        ControlVehicleLookRight = 287,
        ControlReplayStartStopRecording = 288,
        ControlReplayStartStopRecordingSecondary = 289,
        ControlScaledLookLeftRight = 290,
        ControlScaledLookUpDown = 291,
        ControlScaledLookUpOnly = 292,
        ControlScaledLookDownOnly = 293,
        ControlScaledLookLeftOnly = 294,
        ControlScaledLookRightOnly = 295,
        ControlReplayMarkerDelete = 296,
        ControlReplayClipDelete = 297,
        ControlReplayPause = 298,
        ControlReplayRewind = 299,
        ControlReplayFfwd = 300,
        ControlReplayNewmarker = 301,
        ControlReplayRecord = 302,
        ControlReplayScreenshot = 303,
        ControlReplayHidehud = 304,
        ControlReplayStartpoint = 305,
        ControlReplayEndpoint = 306,
        ControlReplayAdvance = 307,
        ControlReplayBack = 308,
        ControlReplayTools = 309,
        ControlReplayRestart = 310,
        ControlReplayShowhotkey = 311,
        ControlReplayCycleMarkerLeft = 312,
        ControlReplayCycleMarkerRight = 313,
        ControlReplayFOVIncrease = 314,
        ControlReplayFOVDecrease = 315,
        ControlReplayCameraUp = 316,
        ControlReplayCameraDown = 317,
        ControlReplaySave = 318,
        ControlReplayToggletime = 319,
        ControlReplayToggletips = 320,
        ControlReplayPreview = 321,
        ControlReplayToggleTimeline = 322,
        ControlReplayTimelinePickupClip = 323,
        ControlReplayTimelineDuplicateClip = 324,
        ControlReplayTimelinePlaceClip = 325,
        ControlReplayCtrl = 326,
        ControlReplayTimelineSave = 327,
        ControlReplayPreviewAudio = 328,
        ControlVehicleDriveLook = 329,
        ControlVehicleDriveLook2 = 330,
        ControlVehicleFlyAttack2 = 331,
        ControlRadioWheelUpDown = 332,
        ControlRadioWheelLeftRight = 333,
        ControlVehicleSlowMoUpDown = 334,
        ControlVehicleSlowMoUpOnly = 335,
        ControlVehicleSlowMoDownOnly = 336,
        ControlMapPointOfInterest = 337,
    };

 
}
