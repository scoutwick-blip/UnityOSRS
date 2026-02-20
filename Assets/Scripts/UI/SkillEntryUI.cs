using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RuneRealm.Skills;

namespace RuneRealm.UI
{
    /// <summary>
    /// Individual skill entry in the skill menu.
    /// Displays skill name, level, and XP progress in Skyrim's minimal style.
    /// </summary>
    public class SkillEntryUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI skillLevelText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;

        private SkillType skill;
        private SkillMenuUI parentMenu;
        private bool isSelected;

        private Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        private Color hoverColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        private Color selectedColor = new Color(0.3f, 0.28f, 0.2f, 0.9f);

        public SkillType Skill => skill;

        /// <summary>
        /// Allows runtime code to wire up references when building UI programmatically.
        /// </summary>
        public void SetupRuntimeReferences(TextMeshProUGUI nameText, TextMeshProUGUI levelText,
            Image progress, Image bg, Image icon)
        {
            skillNameText = nameText;
            skillLevelText = levelText;
            progressFill = progress;
            backgroundImage = bg;
            iconImage = icon;
        }

        public void Initialize(SkillType skillType, SkillMenuUI menu)
        {
            skill = skillType;
            parentMenu = menu;
            Refresh();
        }

        public void Refresh()
        {
            if (SkillManager.Instance == null) return;

            int level = SkillManager.Instance.GetLevel(skill);
            float progress = SkillManager.Instance.GetProgressToNextLevel(skill);

            if (skillNameText != null)
                skillNameText.text = SkillConstants.GetSkillDisplayName(skill);

            if (skillLevelText != null)
            {
                skillLevelText.text = level.ToString();
                skillLevelText.color = level >= 99
                    ? new Color(1f, 0.85f, 0.3f) // Gold for maxed
                    : Color.white;
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = progress;
                progressFill.color = SkillConstants.GetSkillColor(skill);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (backgroundImage != null)
                backgroundImage.color = selected ? selectedColor : normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentMenu != null)
                parentMenu.SelectSkill(skill);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected && backgroundImage != null)
                backgroundImage.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected && backgroundImage != null)
                backgroundImage.color = normalColor;
        }
    }
}
