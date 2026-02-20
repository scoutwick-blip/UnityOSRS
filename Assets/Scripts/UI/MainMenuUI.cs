using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RuneRealm.Core;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style main menu with smoky atmosphere and clean typography.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Camera")]
        [SerializeField] private Transform menuCamera;
        [SerializeField] private float cameraRotationSpeed = 2f;

        [Header("Background")]
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField] private AudioSource menuMusic;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (titleText != null)
            {
                titleText.text = "RUNEREALM";
                titleText.fontSize = 72;
            }

            if (subtitleText != null)
                subtitleText.text = "Skill. Explore. Conquer.";

            SetupButtons();

            // Check if save exists for continue button
            if (continueButton != null)
                continueButton.interactable = SaveSystem.SaveExists();
        }

        private void Update()
        {
            // Slow camera pan for cinematic feel
            if (menuCamera != null)
            {
                menuCamera.Rotate(Vector3.up, cameraRotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void SetupButtons()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(StartNewGame);
            if (continueButton != null)
                continueButton.onClick.AddListener(ContinueGame);
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }

        private void StartNewGame()
        {
            // Fade out and load main scene
            SceneManager.LoadScene("GameWorld");
        }

        private void ContinueGame()
        {
            SceneManager.LoadScene("GameWorld");
            // Save data will be loaded by GameBootstrapper
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
