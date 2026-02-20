using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using RuneRealm.Player;
using RuneRealm.Skills;

namespace RuneRealm.UI
{
    /// <summary>
    /// Main HUD controller in Skyrim aesthetic style.
    /// Minimal, clean HUD with compass, stamina bar, and interaction prompt.
    /// Fades elements in/out based on context.
    /// Self-builds all UI elements at runtime when no editor references are assigned.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Compass")]
        [SerializeField] private RectTransform compassBar;
        [SerializeField] private RectTransform compassMarkerContainer;
        [SerializeField] private TextMeshProUGUI compassDirectionText;

        [Header("Stamina Bar")]
        [SerializeField] private CanvasGroup staminaBarGroup;
        [SerializeField] private Image staminaFill;
        [SerializeField] private float staminaFadeDelay = 3f;
        [SerializeField] private float staminaFadeSpeed = 2f;

        [Header("Interaction Prompt")]
        [SerializeField] private CanvasGroup interactionPromptGroup;
        [SerializeField] private TextMeshProUGUI interactionText;
        [SerializeField] private TextMeshProUGUI interactionKeyText;

        [Header("XP Notification")]
        [SerializeField] private CanvasGroup xpNotificationGroup;
        [SerializeField] private TextMeshProUGUI xpNotificationText;
        [SerializeField] private Image xpSkillIcon;
        [SerializeField] private float xpNotificationDuration = 2f;

        [Header("Level Up Banner")]
        [SerializeField] private CanvasGroup levelUpGroup;
        [SerializeField] private TextMeshProUGUI levelUpText;
        [SerializeField] private TextMeshProUGUI levelUpSkillText;
        [SerializeField] private float levelUpDuration = 4f;

        [Header("Crosshair")]
        [SerializeField] private Image crosshairDot;

        [Header("Clock")]
        [SerializeField] private TextMeshProUGUI clockText;

        [Header("Skill Progress (Skyrim-style bar at bottom)")]
        [SerializeField] private CanvasGroup skillProgressGroup;
        [SerializeField] private Image skillProgressFill;
        [SerializeField] private TextMeshProUGUI skillProgressName;
        [SerializeField] private TextMeshProUGUI skillProgressLevel;

        private float staminaLastChangedTime;
        private float xpNotificationTimer;
        private float levelUpTimer;
        private float skillProgressTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildUIIfNeeded();

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnXPGained += HandleXPGained;
                SkillManager.Instance.OnLevelUp += HandleLevelUp;
            }

            // Start all notification groups hidden
            SetGroupAlpha(xpNotificationGroup, 0f);
            SetGroupAlpha(levelUpGroup, 0f);
            SetGroupAlpha(skillProgressGroup, 0f);
            SetGroupAlpha(interactionPromptGroup, 0f);
        }

        private void Update()
        {
            UpdateCompass();
            UpdateStaminaBar();
            UpdateInteractionPrompt();
            UpdateNotifications();
            UpdateClock();
        }

        // ─── Runtime UI Construction ───────────────────────────────────

        private void BuildUIIfNeeded()
        {
            if (compassDirectionText != null) return; // Already wired up

            var rt = GetComponent<RectTransform>();

            // --- Compass (top center) ---
            var compassGO = CreatePanel("Compass", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(300, 30), new Color(0, 0, 0, 0.4f));
            compassBar = compassGO.GetComponent<RectTransform>();
            compassDirectionText = CreateText(compassGO.transform, "DirectionText",
                "N", 16, TextAlignmentOptions.Center, Color.white);

            // --- Clock (top right) ---
            var clockGO = CreatePanel("Clock", rt, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-60, -10), new Vector2(80, 30), new Color(0, 0, 0, 0.4f));
            clockText = CreateText(clockGO.transform, "ClockText",
                "07:00", 16, TextAlignmentOptions.Center, Color.white);

            // --- Crosshair (center) ---
            var crosshairGO = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            crosshairGO.transform.SetParent(rt, false);
            var chRT = crosshairGO.GetComponent<RectTransform>();
            chRT.anchorMin = chRT.anchorMax = new Vector2(0.5f, 0.5f);
            chRT.sizeDelta = new Vector2(4, 4);
            crosshairDot = crosshairGO.GetComponent<Image>();
            crosshairDot.color = new Color(1, 1, 1, 0.5f);
            crosshairDot.raycastTarget = false;

            // --- Stamina Bar (bottom center, above skill bar) ---
            var staminaGO = CreatePanel("StaminaBar", rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 70), new Vector2(250, 8), new Color(0.1f, 0.1f, 0.1f, 0.6f));
            staminaBarGroup = staminaGO.AddComponent<CanvasGroup>();

            var fillGO = new GameObject("StaminaFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(staminaGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            staminaFill = fillGO.GetComponent<Image>();
            staminaFill.color = Color.white;
            staminaFill.type = Image.Type.Filled;
            staminaFill.fillMethod = Image.FillMethod.Horizontal;
            staminaFill.raycastTarget = false;

            // --- Interaction Prompt (bottom center, above stamina) ---
            var promptGO = CreatePanel("InteractionPrompt", rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 120), new Vector2(250, 40), new Color(0, 0, 0, 0.5f));
            interactionPromptGroup = promptGO.AddComponent<CanvasGroup>();

            interactionKeyText = CreateText(promptGO.transform, "KeyText",
                "[E]", 16, TextAlignmentOptions.Center, new Color(0.9f, 0.8f, 0.3f));
            var keyRT = interactionKeyText.GetComponent<RectTransform>();
            keyRT.anchorMin = new Vector2(0, 0);
            keyRT.anchorMax = new Vector2(0.3f, 1);
            keyRT.offsetMin = keyRT.offsetMax = Vector2.zero;

            interactionText = CreateText(promptGO.transform, "ActionText",
                "", 16, TextAlignmentOptions.Left, Color.white);
            var actRT = interactionText.GetComponent<RectTransform>();
            actRT.anchorMin = new Vector2(0.3f, 0);
            actRT.anchorMax = Vector2.one;
            actRT.offsetMin = actRT.offsetMax = Vector2.zero;

            // --- XP Notification (upper-right area) ---
            var xpGO = CreatePanel("XPNotification", rt, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-120, -50), new Vector2(200, 30), new Color(0, 0, 0, 0.5f));
            xpNotificationGroup = xpGO.AddComponent<CanvasGroup>();
            xpNotificationText = CreateText(xpGO.transform, "XPText",
                "", 14, TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.4f));

            // --- Level Up Banner (center, above crosshair) ---
            var lvlGO = CreatePanel("LevelUp", rt, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f),
                Vector2.zero, new Vector2(350, 60), new Color(0, 0, 0, 0.7f));
            levelUpGroup = lvlGO.AddComponent<CanvasGroup>();

            levelUpText = CreateText(lvlGO.transform, "LevelUpTitle",
                "SKILL INCREASED", 14, TextAlignmentOptions.Center, new Color(0.9f, 0.8f, 0.3f));
            var ltRT = levelUpText.GetComponent<RectTransform>();
            ltRT.anchorMin = new Vector2(0, 0.5f);
            ltRT.anchorMax = new Vector2(1, 1);
            ltRT.offsetMin = ltRT.offsetMax = Vector2.zero;

            levelUpSkillText = CreateText(lvlGO.transform, "LevelUpSkill",
                "", 20, TextAlignmentOptions.Center, Color.white);
            var lsRT = levelUpSkillText.GetComponent<RectTransform>();
            lsRT.anchorMin = new Vector2(0, 0);
            lsRT.anchorMax = new Vector2(1, 0.55f);
            lsRT.offsetMin = lsRT.offsetMax = Vector2.zero;

            // --- Skill Progress Bar (very bottom center, Skyrim-style) ---
            var spGO = CreatePanel("SkillProgress", rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 30), new Vector2(300, 28), new Color(0, 0, 0, 0.5f));
            skillProgressGroup = spGO.AddComponent<CanvasGroup>();

            skillProgressName = CreateText(spGO.transform, "SkillName",
                "", 12, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));
            var snRT = skillProgressName.GetComponent<RectTransform>();
            snRT.anchorMin = new Vector2(0, 0.3f);
            snRT.anchorMax = new Vector2(0.5f, 1);
            snRT.offsetMin = new Vector2(8, 0);
            snRT.offsetMax = Vector2.zero;

            skillProgressLevel = CreateText(spGO.transform, "SkillLevel",
                "", 12, TextAlignmentOptions.Right, Color.white);
            var slRT = skillProgressLevel.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.5f, 0.3f);
            slRT.anchorMax = new Vector2(1, 1);
            slRT.offsetMin = Vector2.zero;
            slRT.offsetMax = new Vector2(-8, 0);

            // Progress fill track
            var trackGO = CreatePanel("ProgressTrack", spGO.GetComponent<RectTransform>(),
                new Vector2(0, 0), new Vector2(1, 0),
                Vector2.zero, Vector2.zero, new Color(0.2f, 0.2f, 0.2f, 0.6f));
            var trackRT = trackGO.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0.03f, 0.05f);
            trackRT.anchorMax = new Vector2(0.97f, 0.25f);
            trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

            var spFillGO = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            spFillGO.transform.SetParent(trackGO.transform, false);
            var spfRT = spFillGO.GetComponent<RectTransform>();
            spfRT.anchorMin = Vector2.zero;
            spfRT.anchorMax = Vector2.one;
            spfRT.offsetMin = spfRT.offsetMax = Vector2.zero;
            skillProgressFill = spFillGO.GetComponent<Image>();
            skillProgressFill.color = new Color(0.9f, 0.8f, 0.3f);
            skillProgressFill.type = Image.Type.Filled;
            skillProgressFill.fillMethod = Image.FillMethod.Horizontal;
            skillProgressFill.raycastTarget = false;

            Debug.Log("[HUDManager] UI built at runtime.");
        }

        // ─── Helpers ───────────────────────────────────────────────────

        private static GameObject CreatePanel(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.anchoredPosition = anchoredPos;
            r.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ─── Update Logic ──────────────────────────────────────────────

        private void UpdateCompass()
        {
            if (Camera.main == null || compassDirectionText == null) return;

            float cameraYaw = Camera.main.transform.eulerAngles.y;

            if (compassDirectionText != null)
            {
                string direction = GetCardinalDirection(cameraYaw);
                compassDirectionText.text = direction;
            }
        }

        private string GetCardinalDirection(float yaw)
        {
            yaw = (yaw + 360f) % 360f;
            if (yaw >= 337.5f || yaw < 22.5f) return "N";
            if (yaw >= 22.5f && yaw < 67.5f) return "NE";
            if (yaw >= 67.5f && yaw < 112.5f) return "E";
            if (yaw >= 112.5f && yaw < 157.5f) return "SE";
            if (yaw >= 157.5f && yaw < 202.5f) return "S";
            if (yaw >= 202.5f && yaw < 247.5f) return "SW";
            if (yaw >= 247.5f && yaw < 292.5f) return "W";
            if (yaw >= 292.5f && yaw < 337.5f) return "NW";
            return "N";
        }

        private void UpdateStaminaBar()
        {
            if (staminaFill == null || staminaBarGroup == null) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            float staminaPct = player.StaminaPercent;
            staminaFill.fillAmount = staminaPct;

            // Color: white when full, yellow when low, red when empty
            if (staminaPct > 0.5f)
                staminaFill.color = Color.Lerp(new Color(0.9f, 0.8f, 0.3f), Color.white, (staminaPct - 0.5f) * 2f);
            else
                staminaFill.color = Color.Lerp(Color.red, new Color(0.9f, 0.8f, 0.3f), staminaPct * 2f);

            // Fade in/out like Skyrim
            if (staminaPct < 0.99f)
            {
                staminaLastChangedTime = Time.time;
                SetGroupAlpha(staminaBarGroup, Mathf.Lerp(staminaBarGroup.alpha, 1f, staminaFadeSpeed * Time.deltaTime));
            }
            else if (Time.time - staminaLastChangedTime > staminaFadeDelay)
            {
                SetGroupAlpha(staminaBarGroup, Mathf.Lerp(staminaBarGroup.alpha, 0f, staminaFadeSpeed * Time.deltaTime));
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (interactionPromptGroup == null) return;

            var player = PlayerController.Instance;
            if (player == null || player.NearestNode == null || player.IsSkilling)
            {
                SetGroupAlpha(interactionPromptGroup,
                    Mathf.Lerp(interactionPromptGroup.alpha, 0f, 8f * Time.deltaTime));
                return;
            }

            string prompt = player.NearestNode.GetInteractionText();
            if (!string.IsNullOrEmpty(prompt))
            {
                if (interactionText != null) interactionText.text = prompt;
                if (interactionKeyText != null) interactionKeyText.text = "[E]";
                SetGroupAlpha(interactionPromptGroup,
                    Mathf.Lerp(interactionPromptGroup.alpha, 1f, 8f * Time.deltaTime));
            }
        }

        private void UpdateNotifications()
        {
            // XP notification fade out
            if (xpNotificationTimer > 0)
            {
                xpNotificationTimer -= Time.deltaTime;
                if (xpNotificationTimer <= 0)
                    SetGroupAlpha(xpNotificationGroup, 0f);
                else if (xpNotificationTimer < 0.5f)
                    SetGroupAlpha(xpNotificationGroup, xpNotificationTimer * 2f);
            }

            // Level up banner fade out
            if (levelUpTimer > 0)
            {
                levelUpTimer -= Time.deltaTime;
                if (levelUpTimer <= 0)
                    SetGroupAlpha(levelUpGroup, 0f);
                else if (levelUpTimer < 1f)
                    SetGroupAlpha(levelUpGroup, levelUpTimer);
            }

            // Skill progress bar
            if (skillProgressTimer > 0)
            {
                skillProgressTimer -= Time.deltaTime;
                if (skillProgressTimer <= 0)
                    SetGroupAlpha(skillProgressGroup, 0f);
                else if (skillProgressTimer < 0.5f)
                    SetGroupAlpha(skillProgressGroup, skillProgressTimer * 2f);
            }
        }

        private void UpdateClock()
        {
            if (clockText == null) return;
            if (GameManager.Instance != null)
                clockText.text = GameManager.Instance.GetTimeString();
        }

        private void HandleXPGained(SkillType skill, int amount, int totalXP)
        {
            if (amount <= 0) return; // Ignore zero-amount events from save loads

            if (xpNotificationText != null)
            {
                xpNotificationText.text = $"+{amount} {SkillConstants.GetSkillDisplayName(skill)} XP";
            }

            SetGroupAlpha(xpNotificationGroup, 1f);
            xpNotificationTimer = xpNotificationDuration;

            // Show skill progress bar
            ShowSkillProgress(skill);
        }

        private void HandleLevelUp(SkillType skill, int newLevel)
        {
            if (levelUpText != null)
                levelUpText.text = "SKILL INCREASED";
            if (levelUpSkillText != null)
                levelUpSkillText.text = $"{SkillConstants.GetSkillDisplayName(skill)} {newLevel}";

            SetGroupAlpha(levelUpGroup, 1f);
            levelUpTimer = levelUpDuration;
        }

        private void ShowSkillProgress(SkillType skill)
        {
            if (SkillManager.Instance == null) return;

            if (skillProgressName != null)
                skillProgressName.text = SkillConstants.GetSkillDisplayName(skill);
            if (skillProgressLevel != null)
                skillProgressLevel.text = SkillManager.Instance.GetLevel(skill).ToString();
            if (skillProgressFill != null)
                skillProgressFill.fillAmount = SkillManager.Instance.GetProgressToNextLevel(skill);

            SetGroupAlpha(skillProgressGroup, 1f);
            skillProgressTimer = 3f;
        }

        public void ShowNotification(string message)
        {
            if (xpNotificationText != null)
                xpNotificationText.text = message;
            SetGroupAlpha(xpNotificationGroup, 1f);
            xpNotificationTimer = xpNotificationDuration;
        }

        private void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
                group.alpha = alpha;
        }

        private void OnDestroy()
        {
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnXPGained -= HandleXPGained;
                SkillManager.Instance.OnLevelUp -= HandleLevelUp;
            }
        }
    }
}
