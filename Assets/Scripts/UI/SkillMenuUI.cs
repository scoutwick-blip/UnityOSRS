using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using RuneRealm.Skills;
using System.Collections.Generic;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style skill menu that displays all OSRS skills in a constellation/perk-tree layout.
    /// Each skill is represented as a node with level, XP progress, and visual indicators.
    /// </summary>
    public class SkillMenuUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private bool isOpen;

        [Header("Skill Entry Template")]
        [SerializeField] private GameObject skillEntryPrefab;
        [SerializeField] private Transform skillListContainer;

        [Header("Skill Detail Panel")]
        [SerializeField] private CanvasGroup detailPanel;
        [SerializeField] private TextMeshProUGUI detailSkillName;
        [SerializeField] private TextMeshProUGUI detailLevel;
        [SerializeField] private TextMeshProUGUI detailXP;
        [SerializeField] private TextMeshProUGUI detailXPToNext;
        [SerializeField] private Image detailProgressBar;
        [SerializeField] private TextMeshProUGUI detailDescription;

        [Header("Total Stats")]
        [SerializeField] private TextMeshProUGUI totalLevelText;
        [SerializeField] private TextMeshProUGUI totalXPText;

        [Header("Style")]
        [SerializeField] private Color normalSkillColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color maxedSkillColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color selectedSkillColor = new Color(1f, 1f, 1f);

        private List<SkillEntryUI> skillEntries = new List<SkillEntryUI>();
        private SkillType? selectedSkill;

        private void Start()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            PopulateSkillList();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleMenu();
            }

            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMenu();
            }
        }

        public void ToggleMenu()
        {
            if (isOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            isOpen = true;
            if (menuRoot != null) menuRoot.SetActive(true);
            if (menuGroup != null) menuGroup.alpha = 1f;

            RefreshAllSkills();

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.InMenu);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMenu()
        {
            isOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Playing);
        }

        private void PopulateSkillList()
        {
            if (skillEntryPrefab == null || skillListContainer == null) return;

            foreach (SkillType skill in System.Enum.GetValues(typeof(SkillType)))
            {
                GameObject entry = Instantiate(skillEntryPrefab, skillListContainer);
                var entryUI = entry.GetComponent<SkillEntryUI>();
                if (entryUI == null)
                    entryUI = entry.AddComponent<SkillEntryUI>();

                entryUI.Initialize(skill, this);
                skillEntries.Add(entryUI);
            }
        }

        public void RefreshAllSkills()
        {
            if (SkillManager.Instance == null) return;

            foreach (var entry in skillEntries)
            {
                entry.Refresh();
            }

            UpdateTotalStats();
        }

        public void SelectSkill(SkillType skill)
        {
            selectedSkill = skill;
            UpdateDetailPanel(skill);

            foreach (var entry in skillEntries)
            {
                entry.SetSelected(entry.Skill == skill);
            }
        }

        private void UpdateDetailPanel(SkillType skill)
        {
            if (SkillManager.Instance == null) return;
            if (detailPanel != null) detailPanel.alpha = 1f;

            int level = SkillManager.Instance.GetLevel(skill);
            int xp = SkillManager.Instance.GetXP(skill);
            int xpToNext = SkillManager.Instance.GetXPToNextLevel(skill);
            float progress = SkillManager.Instance.GetProgressToNextLevel(skill);

            if (detailSkillName != null)
                detailSkillName.text = SkillConstants.GetSkillDisplayName(skill);
            if (detailLevel != null)
                detailLevel.text = $"Level {level}";
            if (detailXP != null)
                detailXP.text = $"XP: {xp:N0}";
            if (detailXPToNext != null)
                detailXPToNext.text = level >= 99 ? "MAX LEVEL" : $"Next Level: {xpToNext:N0} XP";
            if (detailProgressBar != null)
                detailProgressBar.fillAmount = progress;
            if (detailDescription != null)
                detailDescription.text = GetSkillDescription(skill);
        }

        private void UpdateTotalStats()
        {
            if (SkillManager.Instance == null) return;

            if (totalLevelText != null)
                totalLevelText.text = $"Total Level: {SkillManager.Instance.GetTotalLevel()}";
            if (totalXPText != null)
                totalXPText.text = $"Total XP: {SkillManager.Instance.GetTotalXP():N0}";
        }

        private string GetSkillDescription(SkillType skill)
        {
            return skill switch
            {
                SkillType.Woodcutting => "Chop down trees to gather logs for Fletching, Firemaking, and Construction.",
                SkillType.Mining => "Mine rocks for ores used in Smithing and Crafting.",
                SkillType.Fishing => "Catch fish in rivers, lakes, and the sea for Cooking.",
                SkillType.Cooking => "Cook raw food over fires and ranges to create healing items.",
                SkillType.Smithing => "Smelt ores into bars and forge weapons and armor.",
                SkillType.Crafting => "Craft jewelry, leather armor, pottery, and other items.",
                SkillType.Herblore => "Brew potions from herbs and secondary ingredients.",
                SkillType.Farming => "Grow crops, herbs, and trees in farming patches.",
                SkillType.Firemaking => "Light fires for warmth and cooking in the wilderness.",
                SkillType.Fletching => "Craft bows and arrows from logs and other materials.",
                SkillType.Runecrafting => "Craft runes at mysterious altars for use in Magic.",
                SkillType.Hunter => "Track and trap creatures across the realm.",
                SkillType.Agility => "Navigate obstacle courses to increase your run energy.",
                SkillType.Thieving => "Pickpocket NPCs and loot chests for valuables.",
                SkillType.Prayer => "Bury bones to unlock divine protection abilities.",
                SkillType.Magic => "Cast spells for combat, teleportation, and utility.",
                SkillType.Attack => "Increases melee accuracy with all weapon types.",
                SkillType.Strength => "Increases melee damage dealt to enemies.",
                SkillType.Defence => "Reduces damage taken and allows heavier armor.",
                SkillType.Hitpoints => "Your life force. Reach 0 and you will fall.",
                SkillType.Ranged => "Attack from distance with bows and thrown weapons.",
                SkillType.Construction => "Build and furnish your own homestead.",
                _ => "A skill of the realm."
            };
        }
    }
}
