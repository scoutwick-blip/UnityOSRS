using System.Collections.Generic;
using UnityEngine;
using RuneRealm.Core;
using RuneRealm.Inventory;

namespace RuneRealm.Player
{
    /// <summary>
    /// Manages player equipment slots and stat bonuses.
    /// OSRS-style equipment with Skyrim visual equipping.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        private Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();

        [Header("Equipment Attach Points")]
        [SerializeField] private Transform headAttach;
        [SerializeField] private Transform bodyAttach;
        [SerializeField] private Transform legsAttach;
        [SerializeField] private Transform weaponAttach;
        [SerializeField] private Transform shieldAttach;
        [SerializeField] private Transform capeAttach;

        private Dictionary<EquipmentSlot, GameObject> equippedVisuals = new Dictionary<EquipmentSlot, GameObject>();

        public event System.Action<EquipmentSlot, ItemData> OnEquipmentChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize all slots as empty
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                equippedItems[slot] = null;
            }
        }

        public bool Equip(ItemData item)
        {
            if (item == null || !item.isEquipable) return false;
            if (!item.MeetsRequirements()) return false;

            // Unequip current item in that slot
            EquipmentSlot slot = item.equipSlot;
            if (equippedItems[slot] != null)
            {
                Unequip(slot);
            }

            equippedItems[slot] = item;

            // Remove from inventory
            InventoryManager.Instance?.RemoveItem(item, 1);

            // Spawn visual
            SpawnEquipmentVisual(slot, item);

            OnEquipmentChanged?.Invoke(slot, item);
            EventManager.Publish(GameEvents.ItemEquipped, item);

            return true;
        }

        public ItemData Unequip(EquipmentSlot slot)
        {
            ItemData item = equippedItems[slot];
            if (item == null) return null;

            equippedItems[slot] = null;

            // Add back to inventory
            InventoryManager.Instance?.AddItem(item, 1);

            // Remove visual
            RemoveEquipmentVisual(slot);

            OnEquipmentChanged?.Invoke(slot, null);
            EventManager.Publish(GameEvents.ItemUnequipped, item);

            return item;
        }

        public ItemData GetEquipped(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
        }

        public EquipmentStats GetTotalStats()
        {
            var total = new EquipmentStats();

            foreach (var kvp in equippedItems)
            {
                if (kvp.Value == null) continue;
                var stats = kvp.Value.equipStats;
                if (stats == null) continue;

                total.attackStab += stats.attackStab;
                total.attackSlash += stats.attackSlash;
                total.attackCrush += stats.attackCrush;
                total.attackMagic += stats.attackMagic;
                total.attackRanged += stats.attackRanged;
                total.defenceStab += stats.defenceStab;
                total.defenceSlash += stats.defenceSlash;
                total.defenceCrush += stats.defenceCrush;
                total.defenceMagic += stats.defenceMagic;
                total.defenceRanged += stats.defenceRanged;
                total.strengthBonus += stats.strengthBonus;
                total.rangedStrength += stats.rangedStrength;
                total.magicDamage += stats.magicDamage;
                total.prayerBonus += stats.prayerBonus;
            }

            return total;
        }

        public int GetToolTier(Skills.SkillType skill)
        {
            // Check weapon slot for tools
            var weapon = GetEquipped(EquipmentSlot.Weapon);
            if (weapon != null && weapon.isTool && weapon.toolSkill == skill)
                return weapon.toolTier;
            return 0;
        }

        private void SpawnEquipmentVisual(EquipmentSlot slot, ItemData item)
        {
            RemoveEquipmentVisual(slot);

            if (item.worldModel == null) return;

            Transform attachPoint = GetAttachPoint(slot);
            if (attachPoint == null) return;

            GameObject visual = Instantiate(item.worldModel, attachPoint);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            equippedVisuals[slot] = visual;
        }

        private void RemoveEquipmentVisual(EquipmentSlot slot)
        {
            if (equippedVisuals.ContainsKey(slot) && equippedVisuals[slot] != null)
            {
                Destroy(equippedVisuals[slot]);
                equippedVisuals.Remove(slot);
            }
        }

        private Transform GetAttachPoint(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => headAttach,
                EquipmentSlot.Body => bodyAttach,
                EquipmentSlot.Legs => legsAttach,
                EquipmentSlot.Weapon => weaponAttach,
                EquipmentSlot.Shield => shieldAttach,
                EquipmentSlot.Cape => capeAttach,
                _ => null
            };
        }
    }
}
