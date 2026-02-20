using UnityEngine;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Defines all OSRS-style skills with their properties and XP tables.
    /// Uses the exact OSRS XP formula: floor(X + 300 * 2^(X/7)) summed for each level.
    /// </summary>
    public enum SkillType
    {
        Woodcutting,
        Mining,
        Fishing,
        Cooking,
        Smithing,
        Crafting,
        Herblore,
        Farming,
        Firemaking,
        Fletching,
        Runecrafting,
        Hunter,
        Agility,
        Thieving,
        Prayer,
        Magic,
        Attack,
        Strength,
        Defence,
        Hitpoints,
        Ranged,
        Construction
    }

    [System.Serializable]
    public class SkillSaveData
    {
        public SkillType skillType;
        public int currentXP;
    }

    public static class SkillConstants
    {
        public const int MaxLevel = 99;
        public const int MaxXP = 200_000_000;
        public const int TotalSkills = 22;

        // Exact OSRS XP table (precomputed for levels 1-99)
        private static readonly int[] xpTable;

        static SkillConstants()
        {
            xpTable = new int[MaxLevel + 1];
            xpTable[0] = 0;
            xpTable[1] = 0;

            int accumulated = 0;
            for (int level = 1; level < MaxLevel; level++)
            {
                accumulated += (int)Mathf.Floor(level + 300f * Mathf.Pow(2f, level / 7f));
                xpTable[level + 1] = (int)Mathf.Floor(accumulated / 4f);
            }
        }

        public static int GetXPForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level > MaxLevel) level = MaxLevel;
            return xpTable[level];
        }

        public static int GetLevelForXP(int xp)
        {
            for (int level = MaxLevel; level >= 1; level--)
            {
                if (xp >= xpTable[level])
                    return level;
            }
            return 1;
        }

        public static int GetXPToNextLevel(int currentXP, int currentLevel)
        {
            if (currentLevel >= MaxLevel) return 0;
            return xpTable[currentLevel + 1] - currentXP;
        }

        public static float GetProgressToNextLevel(int currentXP, int currentLevel)
        {
            if (currentLevel >= MaxLevel) return 1f;
            int xpForCurrent = xpTable[currentLevel];
            int xpForNext = xpTable[currentLevel + 1];
            int range = xpForNext - xpForCurrent;
            if (range <= 0) return 1f;
            return (float)(currentXP - xpForCurrent) / range;
        }

        public static string GetSkillDisplayName(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Woodcutting: return "Woodcutting";
                case SkillType.Mining: return "Mining";
                case SkillType.Fishing: return "Fishing";
                case SkillType.Cooking: return "Cooking";
                case SkillType.Smithing: return "Smithing";
                case SkillType.Crafting: return "Crafting";
                case SkillType.Herblore: return "Herblore";
                case SkillType.Farming: return "Farming";
                case SkillType.Firemaking: return "Firemaking";
                case SkillType.Fletching: return "Fletching";
                case SkillType.Runecrafting: return "Runecrafting";
                case SkillType.Hunter: return "Hunter";
                case SkillType.Agility: return "Agility";
                case SkillType.Thieving: return "Thieving";
                case SkillType.Prayer: return "Prayer";
                case SkillType.Magic: return "Magic";
                case SkillType.Attack: return "Attack";
                case SkillType.Strength: return "Strength";
                case SkillType.Defence: return "Defence";
                case SkillType.Hitpoints: return "Hitpoints";
                case SkillType.Ranged: return "Ranged";
                case SkillType.Construction: return "Construction";
                default: return skill.ToString();
            }
        }

        public static Color GetSkillColor(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Woodcutting: return new Color(0.36f, 0.25f, 0.13f);
                case SkillType.Mining: return new Color(0.55f, 0.55f, 0.6f);
                case SkillType.Fishing: return new Color(0.2f, 0.5f, 0.7f);
                case SkillType.Cooking: return new Color(0.8f, 0.4f, 0.1f);
                case SkillType.Smithing: return new Color(0.4f, 0.35f, 0.3f);
                case SkillType.Crafting: return new Color(0.6f, 0.5f, 0.3f);
                case SkillType.Herblore: return new Color(0.2f, 0.6f, 0.2f);
                case SkillType.Farming: return new Color(0.3f, 0.55f, 0.2f);
                case SkillType.Firemaking: return new Color(0.85f, 0.45f, 0.1f);
                case SkillType.Fletching: return new Color(0.1f, 0.5f, 0.45f);
                case SkillType.Runecrafting: return new Color(0.5f, 0.3f, 0.6f);
                case SkillType.Hunter: return new Color(0.5f, 0.4f, 0.2f);
                case SkillType.Agility: return new Color(0.2f, 0.2f, 0.6f);
                case SkillType.Thieving: return new Color(0.4f, 0.15f, 0.5f);
                case SkillType.Prayer: return new Color(0.9f, 0.85f, 0.6f);
                case SkillType.Magic: return new Color(0.3f, 0.3f, 0.8f);
                case SkillType.Attack: return new Color(0.7f, 0.15f, 0.15f);
                case SkillType.Strength: return new Color(0.1f, 0.6f, 0.1f);
                case SkillType.Defence: return new Color(0.4f, 0.55f, 0.75f);
                case SkillType.Hitpoints: return new Color(0.75f, 0.2f, 0.2f);
                case SkillType.Ranged: return new Color(0.1f, 0.5f, 0.1f);
                case SkillType.Construction: return new Color(0.6f, 0.4f, 0.2f);
                default: return Color.white;
            }
        }
    }
}
