using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RuneRealm.Core
{
    /// <summary>
    /// Central game manager singleton. Manages game state, time of day, and global systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Playing;

        [Header("Time of Day")]
        [SerializeField] private float dayLengthInMinutes = 20f;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private Gradient directionalColorGradient;
        [SerializeField] private AnimationCurve lightIntensityCurve;
        [SerializeField] private float currentTimeOfDay = 0.3f; // Start at morning

        [Header("World Settings")]
        [SerializeField] private float worldScale = 1f;

        public GameState CurrentState => currentState;
        public float TimeOfDay => currentTimeOfDay;
        public float WorldScale => worldScale;
        public bool IsPaused => currentState == GameState.Paused;

        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<float> OnTimeOfDayChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGradients();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                UpdateTimeOfDay();
                UpdateLighting();
            }

            HandleGlobalInput();
        }

        private void InitializeGradients()
        {
            if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            {
                ambientColorGradient = new Gradient();
                ambientColorGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0f),    // Midnight
                        new GradientColorKey(new Color(0.4f, 0.28f, 0.2f), 0.23f),    // Dawn
                        new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 0.35f),    // Morning
                        new GradientColorKey(new Color(0.85f, 0.85f, 0.9f), 0.5f),    // Noon
                        new GradientColorKey(new Color(0.8f, 0.6f, 0.3f), 0.73f),     // Sunset
                        new GradientColorKey(new Color(0.15f, 0.1f, 0.2f), 0.85f),    // Dusk
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1f),     // Midnight
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            if (directionalColorGradient == null || directionalColorGradient.colorKeys.Length == 0)
            {
                directionalColorGradient = new Gradient();
                directionalColorGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.25f), 0f),
                        new GradientColorKey(new Color(1f, 0.55f, 0.2f), 0.23f),
                        new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.35f),
                        new GradientColorKey(new Color(1f, 0.98f, 0.92f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.15f), 0.73f),
                        new GradientColorKey(new Color(0.3f, 0.15f, 0.3f), 0.85f),
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.25f), 1f),
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            if (lightIntensityCurve == null || lightIntensityCurve.keys.Length == 0)
            {
                lightIntensityCurve = new AnimationCurve(
                    new Keyframe(0f, 0.05f),
                    new Keyframe(0.2f, 0.05f),
                    new Keyframe(0.3f, 0.8f),
                    new Keyframe(0.5f, 1.2f),
                    new Keyframe(0.7f, 0.8f),
                    new Keyframe(0.8f, 0.05f),
                    new Keyframe(1f, 0.05f)
                );
            }
        }

        private void UpdateTimeOfDay()
        {
            float daySpeed = 1f / (dayLengthInMinutes * 60f);
            currentTimeOfDay += Time.deltaTime * daySpeed;
            if (currentTimeOfDay >= 1f)
                currentTimeOfDay -= 1f;

            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }

        private void UpdateLighting()
        {
            if (directionalLight != null)
            {
                float sunAngle = (currentTimeOfDay * 360f) - 90f;
                directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
                directionalLight.color = directionalColorGradient.Evaluate(currentTimeOfDay);
                directionalLight.intensity = lightIntensityCurve.Evaluate(currentTimeOfDay);
            }

            RenderSettings.ambientLight = ambientColorGradient.Evaluate(currentTimeOfDay);
            RenderSettings.fogColor = Color.Lerp(
                ambientColorGradient.Evaluate(currentTimeOfDay),
                directionalColorGradient.Evaluate(currentTimeOfDay),
                0.5f
            );
        }

        private void HandleGlobalInput()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (currentState == GameState.Playing)
                    SetGameState(GameState.Paused);
                else if (currentState == GameState.Paused)
                    SetGameState(GameState.Playing);
            }
        }

        public void SetGameState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            Time.timeScale = (newState == GameState.Paused) ? 0f : 1f;

            Cursor.lockState = (newState == GameState.Playing)
                ? CursorLockMode.Locked
                : CursorLockMode.None;
            Cursor.visible = newState != GameState.Playing;

            OnGameStateChanged?.Invoke(currentState);
        }

        public string GetTimeString()
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay * 24f);
            int minutes = Mathf.FloorToInt((currentTimeOfDay * 24f - hours) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }

        public bool IsNightTime()
        {
            return currentTimeOfDay < 0.22f || currentTimeOfDay > 0.82f;
        }
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        InMenu,
        Dialogue,
        Skilling
    }
}
