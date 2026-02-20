using System.Collections.Generic;
using UnityEngine;
using RuneRealm.Core;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Manages all player skills, XP tracking, and level progression.
    /// Follows exact OSRS XP mechanics and level formula.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        private Dictionary<SkillType, int> skillXP = new Dictionary<SkillType, int>();
        private Dictionary<SkillType, int> skillLevels = new Dictionary<SkillType, int>();

        public event System.Action<SkillType, int, int> OnXPGained; // skill, amount, totalXP
        public event System.Action<SkillType, int> OnLevelUp; // skill, newLevel
        public event System.Action<int> OnTotalLevelChanged; // totalLevel

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSkills();
        }

        private void InitializeSkills()
        {
            foreach (SkillType skill in System.Enum.GetValues(typeof(SkillType)))
            {
                skillXP[skill] = 0;
                skillLevels[skill] = 1;

                // Hitpoints starts at 10 in OSRS
                if (skill == SkillType.Hitpoints)
                {
                    skillXP[skill] = SkillConstants.GetXPForLevel(10);
                    skillLevels[skill] = 10;
                }
            }
        }

        public void AddXP(SkillType skill, int amount)
        {
            if (amount <= 0) return;

            int previousXP = skillXP[skill];
            int previousLevel = skillLevels[skill];

            skillXP[skill] = Mathf.Min(previousXP + amount, SkillConstants.MaxXP);
            int newLevel = SkillConstants.GetLevelForXP(skillXP[skill]);
            skillLevels[skill] = newLevel;

            OnXPGained?.Invoke(skill, amount, skillXP[skill]);
            EventManager.Publish(GameEvents.SkillXPGained, skill);

            if (newLevel > previousLevel)
            {
                for (int level = previousLevel + 1; level <= newLevel; level++)
                {
                    OnLevelUp?.Invoke(skill, level);
                    EventManager.Publish(GameEvents.SkillLevelUp, skill);
                    Debug.Log($"[SkillManager] {SkillConstants.GetSkillDisplayName(skill)} leveled up to {level}!");
                }
                OnTotalLevelChanged?.Invoke(GetTotalLevel());
            }
        }

        public int GetLevel(SkillType skill)
        {
            return skillLevels.ContainsKey(skill) ? skillLevels[skill] : 1;
        }

        public int GetXP(SkillType skill)
        {
            return skillXP.ContainsKey(skill) ? skillXP[skill] : 0;
        }

        public int GetXPToNextLevel(SkillType skill)
        {
            return SkillConstants.GetXPToNextLevel(GetXP(skill), GetLevel(skill));
        }

        public float GetProgressToNextLevel(SkillType skill)
        {
            return SkillConstants.GetProgressToNextLevel(GetXP(skill), GetLevel(skill));
        }

        public int GetTotalLevel()
        {
            int total = 0;
            foreach (var kvp in skillLevels)
                total += kvp.Value;
            return total;
        }

        public long GetTotalXP()
        {
            long total = 0;
            foreach (var kvp in skillXP)
                total += kvp.Value;
            return total;
        }

        public bool MeetsRequirement(SkillType skill, int requiredLevel)
        {
            return GetLevel(skill) >= requiredLevel;
        }

        public SkillSaveData[] GetSaveData()
        {
            var dataList = new List<SkillSaveData>();
            foreach (var kvp in skillXP)
            {
                dataList.Add(new SkillSaveData
                {
                    skillType = kvp.Key,
                    currentXP = kvp.Value
                });
            }
            return dataList.ToArray();
        }

        public void LoadSaveData(SkillSaveData[] data)
        {
            if (data == null) return;

            foreach (var skillData in data)
            {
                skillXP[skillData.skillType] = skillData.currentXP;
                skillLevels[skillData.skillType] = SkillConstants.GetLevelForXP(skillData.currentXP);
            }
        }
    }
}
