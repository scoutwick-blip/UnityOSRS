using UnityEngine;
using RuneRealm.Skills;

namespace RuneRealm.Inventory
{
    /// <summary>
    /// ScriptableObject defining an item. Used for all items in the game:
    /// resources, tools, equipment, consumables, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "RuneRealm/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName;
        public string description;
        public int itemId;
        public Sprite icon;
        public GameObject worldModel;

        [Header("Item Properties")]
        public ItemCategory category;
        public ItemRarity rarity = ItemRarity.Common;
        public bool isStackable;
        public int maxStackSize = 1;
        public bool isNoteable;
        public int highAlchValue;
        public int lowAlchValue;
        public int generalStoreValue;

        [Header("Skill Requirements")]
        public SkillRequirement[] skillRequirements;

        [Header("Equipment")]
        public bool isEquipable;
        public EquipmentSlot equipSlot;
        public EquipmentStats equipStats;

        [Header("Consumable")]
        public bool isConsumable;
        public ConsumableEffect[] consumableEffects;

        [Header("Tool")]
        public bool isTool;
        public SkillType toolSkill;
        public int toolTier; // Higher = better (bronze=1, iron=2, ..., dragon=6, etc.)
        public float toolSpeedMultiplier = 1f;

        public bool MeetsRequirements()
        {
            if (skillRequirements == null) return true;
            foreach (var req in skillRequirements)
            {
                if (SkillManager.Instance != null &&
                    !SkillManager.Instance.MeetsRequirement(req.skill, req.level))
                    return false;
            }
            return true;
        }
    }

    public enum ItemCategory
    {
        Resource,
        Tool,
        Weapon,
        Armor,
        Food,
        Potion,
        Rune,
        Seed,
        Herb,
        Ore,
        Bar,
        Log,
        Fish,
        Gem,
        Bone,
        Craftable,
        Quest,
        Misc
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum EquipmentSlot
    {
        Head,
        Cape,
        Neck,
        Weapon,
        Body,
        Shield,
        Legs,
        Hands,
        Feet,
        Ring,
        Ammo
    }

    [System.Serializable]
    public class SkillRequirement
    {
        public SkillType skill;
        public int level;
    }

    [System.Serializable]
    public class EquipmentStats
    {
        public int attackStab;
        public int attackSlash;
        public int attackCrush;
        public int attackMagic;
        public int attackRanged;
        public int defenceStab;
        public int defenceSlash;
        public int defenceCrush;
        public int defenceMagic;
        public int defenceRanged;
        public int strengthBonus;
        public int rangedStrength;
        public int magicDamage;
        public int prayerBonus;
    }

    [System.Serializable]
    public class ConsumableEffect
    {
        public ConsumableEffectType effectType;
        public int value;
        public float duration;
    }

    public enum ConsumableEffectType
    {
        HealHP,
        BoostSkill,
        RestoreEnergy,
        RestorePrayer,
        Poison,
        Antipoison
    }
}
