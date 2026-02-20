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

        private void UpdateCompass()
        {
            if (Camera.main == null || compassBar == null) return;

            float cameraYaw = Camera.main.transform.eulerAngles.y;
            // Scroll the compass bar based on camera direction
            float normalizedYaw = cameraYaw / 360f;
            compassBar.anchoredPosition = new Vector2(-normalizedYaw * 720f, compassBar.anchoredPosition.y);

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
