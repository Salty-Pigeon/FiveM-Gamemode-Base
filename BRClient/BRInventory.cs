using GTA_GameRooShared;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;

namespace BRClient {

    public class BRInventory {

        // Slots: 0=Gun1, 1=Gun2, 2=Melee
        public uint[] Slots = new uint[3];
        public int ActiveSlot = 0;

        public const float MAX_WEIGHT = 20f;

        // Consumables
        public int BandageCount = 0;
        public int AdrenalineCount = 0;
        public const int MAX_BANDAGES = 5;
        public const int MAX_ADRENALINE = 5;

        // Weight by weapon group name
        static readonly Dictionary<string, float> GroupWeights = new Dictionary<string, float>() {
            { "GROUP_PISTOL", 2f },
            { "GROUP_SMG", 4f },
            { "GROUP_SHOTGUN", 5f },
            { "GROUP_RIFLE", 6f },
            { "GROUP_SNIPER", 7f },
            { "GROUP_MG", 8f },
            { "GROUP_HEAVY", 10f },
        };

        public bool IsMeleeGroup( uint hash ) {
            if( !Globals.Weapons.ContainsKey( hash ) ) return false;
            string group = Globals.Weapons[hash]["Group"];
            return group == "GROUP_MELEE" || group == "GROUP_UNARMED";
        }

        public bool IsGunSlot( int slot ) {
            return slot < 2;
        }

        /// <summary>
        /// Check if weapon can be added. Returns true if it fits somewhere.
        /// </summary>
        public bool CanAdd( uint hash ) {
            if( hash == 0 ) return false;

            // If we already have this exact weapon, it would add ammo (handled externally)
            for( int i = 0; i < 3; i++ ) {
                if( Slots[i] == hash ) return true;
            }

            if( IsMeleeGroup( hash ) ) {
                return Slots[2] == 0;
            } else {
                return Slots[0] == 0 || Slots[1] == 0;
            }
        }

        /// <summary>
        /// Add weapon to appropriate slot. Returns slot index or -1 if failed.
        /// </summary>
        public int Add( uint hash ) {
            if( hash == 0 ) return -1;

            // Duplicate check - if already have this weapon, return its slot (ammo handled externally)
            for( int i = 0; i < 3; i++ ) {
                if( Slots[i] == hash ) return i;
            }

            if( IsMeleeGroup( hash ) ) {
                if( Slots[2] == 0 ) {
                    Slots[2] = hash;
                    return 2;
                }
                return -1;
            } else {
                if( Slots[0] == 0 ) {
                    Slots[0] = hash;
                    return 0;
                }
                if( Slots[1] == 0 ) {
                    Slots[1] = hash;
                    return 1;
                }
                return -1;
            }
        }

        /// <summary>
        /// Remove weapon from slot and return its hash (0 if empty).
        /// </summary>
        public uint Remove( int slot ) {
            if( slot < 0 || slot > 2 ) return 0;
            uint hash = Slots[slot];
            Slots[slot] = 0;
            return hash;
        }

        /// <summary>
        /// Cycle to next occupied slot.
        /// </summary>
        public void CycleNext() {
            for( int i = 1; i <= 3; i++ ) {
                int next = ( ActiveSlot + i ) % 3;
                if( Slots[next] != 0 ) {
                    ActiveSlot = next;
                    return;
                }
            }
        }

        /// <summary>
        /// Cycle to previous occupied slot.
        /// </summary>
        public void CyclePrev() {
            for( int i = 1; i <= 3; i++ ) {
                int prev = ( ActiveSlot - i + 3 ) % 3;
                if( Slots[prev] != 0 ) {
                    ActiveSlot = prev;
                    return;
                }
            }
        }

        /// <summary>
        /// Set active slot directly.
        /// </summary>
        public void SelectSlot( int slot ) {
            if( slot >= 0 && slot <= 2 ) {
                ActiveSlot = slot;
            }
        }

        /// <summary>
        /// Return hash of weapon in active slot (0 if empty).
        /// </summary>
        public uint GetActive() {
            return Slots[ActiveSlot];
        }

        /// <summary>
        /// Sum weights of weapons in gun slots only.
        /// </summary>
        public float GetTotalWeight() {
            float total = 0f;
            for( int i = 0; i < 2; i++ ) {
                if( Slots[i] != 0 ) {
                    total += GetWeaponWeight( Slots[i] );
                }
            }
            return total;
        }

        /// <summary>
        /// Calculate movement speed modifier based on total gun weight.
        /// </summary>
        public float GetMoveRate() {
            float weight = GetTotalWeight();
            return Math.Max( 0.80f, 1.0f - weight * 0.015f );
        }

        /// <summary>
        /// Get friendly weapon name for a slot.
        /// </summary>
        public string GetWeaponName( int slot ) {
            if( slot < 0 || slot > 2 ) return "Empty";
            uint hash = Slots[slot];
            if( hash == 0 ) return "Empty";
            if( Globals.Weapons.ContainsKey( hash ) ) {
                return Globals.Weapons[hash]["Name"];
            }
            return "Unknown";
        }

        /// <summary>
        /// Get weight of a specific weapon by its hash.
        /// </summary>
        float GetWeaponWeight( uint hash ) {
            if( hash == 0 ) return 0f;
            if( Globals.Weapons.ContainsKey( hash ) ) {
                string group = Globals.Weapons[hash]["Group"];
                if( GroupWeights.ContainsKey( group ) ) {
                    return GroupWeights[group];
                }
            }
            return 0f;
        }

        public bool AddBandage() {
            if( BandageCount >= MAX_BANDAGES ) return false;
            BandageCount++;
            return true;
        }

        public bool AddAdrenaline() {
            if( AdrenalineCount >= MAX_ADRENALINE ) return false;
            AdrenalineCount++;
            return true;
        }

        public bool UseBandage() {
            if( BandageCount <= 0 ) return false;
            BandageCount--;
            return true;
        }

        public bool UseAdrenaline() {
            if( AdrenalineCount <= 0 ) return false;
            AdrenalineCount--;
            return true;
        }

        /// <summary>
        /// Reset inventory to empty state.
        /// </summary>
        public void Clear() {
            Slots[0] = 0;
            Slots[1] = 0;
            Slots[2] = 0;
            ActiveSlot = 0;
            BandageCount = 0;
            AdrenalineCount = 0;
        }
    }
}
