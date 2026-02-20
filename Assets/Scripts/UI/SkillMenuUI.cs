using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using RuneRealm.Skills;
using System.Collections.Generic;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style skill menu that displays all OSRS skills in a list layout.
    /// Self-builds all elements at runtime when no editor references are assigned.
    /// Toggle with K.
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
            BuildUIIfNeeded();

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

        // ─── Runtime UI Construction ───────────────────────────────────

        private void BuildUIIfNeeded()
        {
            if (menuRoot != null) return;

            var rt = GetComponent<RectTransform>();

            // Fullscreen overlay
            menuRoot = new GameObject("SkillMenuRoot");
            menuRoot.transform.SetParent(rt, false);
            var rootRT = menuRoot.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

            var bgImg = menuRoot.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            bgImg.raycastTarget = true;

            menuGroup = menuRoot.AddComponent<CanvasGroup>();

            // Title
            CreateText(menuRoot.transform, "Title", "SKILLS", 28,
                TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.7f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(400, 40));

            // Hint
            CreateText(menuRoot.transform, "Hint", "Press K to close  |  Click a skill for details", 12,
                TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(400, 20));

            // Skill list (left panel with scroll)
            var listPanel = new GameObject("SkillListPanel", typeof(RectTransform), typeof(Image));
            listPanel.transform.SetParent(menuRoot.transform, false);
            var lpRT = listPanel.GetComponent<RectTransform>();
            lpRT.anchorMin = new Vector2(0.5f, 0.5f);
            lpRT.anchorMax = new Vector2(0.5f, 0.5f);
            lpRT.anchoredPosition = new Vector2(-120, -20);
            lpRT.sizeDelta = new Vector2(280, 480);
            listPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.7f);

            // Scroll view setup
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(listPanel.transform, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.sizeDelta = new Vector2(0, 0);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.viewport = vpRT;
            scrollRect.content = cRT;
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 30f;

            skillListContainer = content.transform;

            // Create skill entry prefab template
            skillEntryPrefab = CreateSkillEntryTemplate();

            // Detail panel (right side)
            var detailGO = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
            detailGO.transform.SetParent(menuRoot.transform, false);
            var dRT = detailGO.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0.5f, 0.5f);
            dRT.anchorMax = new Vector2(0.5f, 0.5f);
            dRT.anchoredPosition = new Vector2(160, -20);
            dRT.sizeDelta = new Vector2(260, 350);
            detailGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.7f);
            detailPanel = detailGO.AddComponent<CanvasGroup>();
            detailPanel.alpha = 0f;

            detailSkillName = CreateText(detailGO.transform, "SkillName", "", 24,
                TextAlignmentOptions.Center, Color.white,
                new Vector2(0, 0.85f), new Vector2(1, 1), Vector2.zero, Vector2.zero);

            detailLevel = CreateText(detailGO.transform, "Level", "", 18,
                TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.4f),
                new Vector2(0, 0.72f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero);

            detailXP = CreateText(detailGO.transform, "XP", "", 14,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0, 0.6f), new Vector2(1, 0.72f), Vector2.zero, Vector2.zero);

            detailXPToNext = CreateText(detailGO.transform, "XPToNext", "", 14,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0, 0.5f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero);

            // Progress bar
            var barBG = new GameObject("ProgressBG", typeof(RectTransform), typeof(Image));
            barBG.transform.SetParent(detailGO.transform, false);
            var bbRT = barBG.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0.1f, 0.42f);
            bbRT.anchorMax = new Vector2(0.9f, 0.48f);
            bbRT.offsetMin = bbRT.offsetMax = Vector2.zero;
            barBG.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var barFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBG.transform, false);
            var bfRT = barFill.GetComponent<RectTransform>();
            bfRT.anchorMin = Vector2.zero;
            bfRT.anchorMax = Vector2.one;
            bfRT.offsetMin = bfRT.offsetMax = Vector2.zero;
            detailProgressBar = barFill.GetComponent<Image>();
            detailProgressBar.color = new Color(0.9f, 0.8f, 0.3f);
            detailProgressBar.type = Image.Type.Filled;
            detailProgressBar.fillMethod = Image.FillMethod.Horizontal;
            detailProgressBar.raycastTarget = false;

            detailDescription = CreateText(detailGO.transform, "Description", "", 13,
                TextAlignmentOptions.TopLeft, new Color(0.65f, 0.65f, 0.65f),
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.4f), Vector2.zero, Vector2.zero);

            // Total stats (bottom)
            totalLevelText = CreateText(menuRoot.transform, "TotalLevel", "", 16,
                TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.4f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 30), new Vector2(200, 25));

            totalXPText = CreateText(menuRoot.transform, "TotalXP", "", 14,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(140, 30), new Vector2(200, 25));

            Debug.Log("[SkillMenuUI] UI built at runtime.");
        }

        private GameObject CreateSkillEntryTemplate()
        {
            var entryGO = new GameObject("SkillEntryTemplate", typeof(RectTransform));
            entryGO.SetActive(false);

            var layoutElem = entryGO.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 36;

            // Background
            var bg = entryGO.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            bg.raycastTarget = true;

            // Skill name (left)
            var nameGO = new GameObject("SkillName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(entryGO.transform, false);
            var nRT = nameGO.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0, 0);
            nRT.anchorMax = new Vector2(0.55f, 1);
            nRT.offsetMin = new Vector2(10, 0);
            nRT.offsetMax = Vector2.zero;
            var nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.text = "";
            nameTMP.fontSize = 14;
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            nameTMP.color = new Color(0.8f, 0.8f, 0.8f);
            nameTMP.raycastTarget = false;

            // Level number (right)
            var levelGO = new GameObject("Level", typeof(RectTransform), typeof(TextMeshProUGUI));
            levelGO.transform.SetParent(entryGO.transform, false);
            var lRT = levelGO.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.7f, 0);
            lRT.anchorMax = new Vector2(1, 1);
            lRT.offsetMin = Vector2.zero;
            lRT.offsetMax = new Vector2(-10, 0);
            var levelTMP = levelGO.GetComponent<TextMeshProUGUI>();
            levelTMP.text = "";
            levelTMP.fontSize = 16;
            levelTMP.alignment = TextAlignmentOptions.MidlineRight;
            levelTMP.color = Color.white;
            levelTMP.raycastTarget = false;

            // XP progress bar (bottom strip)
            var progressBG = new GameObject("ProgressBG", typeof(RectTransform), typeof(Image));
            progressBG.transform.SetParent(entryGO.transform, false);
            var pbRT = progressBG.GetComponent<RectTransform>();
            pbRT.anchorMin = new Vector2(0.02f, 0);
            pbRT.anchorMax = new Vector2(0.68f, 0.12f);
            pbRT.offsetMin = pbRT.offsetMax = Vector2.zero;
            progressBG.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            progressBG.GetComponent<Image>().raycastTarget = false;

            var progressFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            progressFill.transform.SetParent(progressBG.transform, false);
            var pfRT = progressFill.GetComponent<RectTransform>();
            pfRT.anchorMin = Vector2.zero;
            pfRT.anchorMax = Vector2.one;
            pfRT.offsetMin = pfRT.offsetMax = Vector2.zero;
            var fillImg = progressFill.GetComponent<Image>();
            fillImg.color = new Color(0.4f, 0.6f, 0.3f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.raycastTarget = false;

            // Wire up SkillEntryUI
            var entryUI = entryGO.AddComponent<SkillEntryUI>();
            entryUI.SetupRuntimeReferences(nameTMP, levelTMP, fillImg, bg, null);

            entryGO.transform.SetParent(transform, false);
            return entryGO;
        }

        // ─── Helpers ───────────────────────────────────────────────────

        private static TextMeshProUGUI CreateText(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.anchoredPosition = anchoredPos;
            r.sizeDelta = size;
            if (anchoredPos == Vector2.zero && size == Vector2.zero)
            {
                r.offsetMin = r.offsetMax = Vector2.zero;
            }
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ─── Menu Toggle ──────────────────────────────────────────────

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
                entry.SetActive(true);
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
