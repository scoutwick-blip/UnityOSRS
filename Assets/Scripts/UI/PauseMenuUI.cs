using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style pause menu with save, load, settings, and quit options.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private GameObject menuRoot;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Save Confirmation")]
        [SerializeField] private TextMeshProUGUI saveStatusText;
        [SerializeField] private float saveMessageDuration = 2f;

        private bool isOpen;
        private float saveMessageTimer;

        private void Start()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            SetupButtons();
            SetupSliders();

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        private void Update()
        {
            if (saveMessageTimer > 0)
            {
                saveMessageTimer -= Time.unscaledDeltaTime;
                if (saveMessageTimer <= 0 && saveStatusText != null)
                    saveStatusText.text = "";
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            if (saveButton != null)
                saveButton.onClick.AddListener(Save);
            if (loadButton != null)
                loadButton.onClick.AddListener(Load);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ToggleSettings);
            if (quitButton != null)
                quitButton.onClick.AddListener(Quit);
        }

        private void SetupSliders()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = 0.5f;
                musicVolumeSlider.onValueChanged.AddListener(v =>
                    Audio.AudioManager.Instance?.SetMusicVolume(v));
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = 0.8f;
                sfxVolumeSlider.onValueChanged.AddListener(v =>
                    Audio.AudioManager.Instance?.SetSFXVolume(v));
            }

            if (ambientVolumeSlider != null)
            {
                ambientVolumeSlider.value = 0.4f;
                ambientVolumeSlider.onValueChanged.AddListener(v =>
                    Audio.AudioManager.Instance?.SetAmbientVolume(v));
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(v =>
                    Screen.fullScreen = v);
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Paused)
                Show();
            else if (isOpen)
                Hide();
        }

        private void Show()
        {
            isOpen = true;
            if (menuRoot != null) menuRoot.SetActive(true);
            if (menuGroup != null) menuGroup.alpha = 1f;
        }

        private void Hide()
        {
            isOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void Resume()
        {
            GameManager.Instance?.SetGameState(GameState.Playing);
        }

        private void Save()
        {
            if (Player.PlayerController.Instance == null || Skills.SkillManager.Instance == null) return;

            var saveData = new SaveSystem.SaveData
            {
                player = Player.PlayerController.Instance.GetSaveData(),
                skills = Skills.SkillManager.Instance.GetSaveData(),
                inventory = Inventory.InventoryManager.Instance?.GetSaveData(),
                quests = NPCs.QuestManager.Instance?.GetSaveData(),
                world = new SaveSystem.WorldSaveData
                {
                    timeOfDay = GameManager.Instance?.TimeOfDay ?? 0.3f,
                    currentZone = World.ZoneManager.Instance?.CurrentZone?.zoneName ?? "Unknown"
                }
            };

            SaveSystem.Save(saveData);

            if (saveStatusText != null)
                saveStatusText.text = "Game Saved";
            saveMessageTimer = saveMessageDuration;
        }

        private void Load()
        {
            var saveData = SaveSystem.Load();
            if (saveData == null)
            {
                if (saveStatusText != null)
                    saveStatusText.text = "No save found";
                saveMessageTimer = saveMessageDuration;
                return;
            }

            Player.PlayerController.Instance?.LoadSaveData(saveData.player);
            Skills.SkillManager.Instance?.LoadSaveData(saveData.skills);

            Resume();
        }

        private void ToggleSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }
}
