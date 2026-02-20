using UnityEngine;
using RuneRealm.Skills;
using RuneRealm.Inventory;
using RuneRealm.Player;
using RuneRealm.World;
using RuneRealm.NPCs;
using RuneRealm.Audio;
using RuneRealm.UI;

namespace RuneRealm.Core
{
    /// <summary>
    /// Main game bootstrapper. Initializes all systems in the correct order
    /// and sets up the initial game scene.
    /// Attach this to a GameObject in your main scene to start the game.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject cameraPrefab;
        [SerializeField] private GameObject hudCanvasPrefab;
        [SerializeField] private GameObject directionalLightPrefab;

        [Header("Spawn")]
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(256, 50, 256);
        [SerializeField] private Vector3 playerSpawnRotation = Vector3.zero;

        [Header("World Generation")]
        [SerializeField] private bool generateTerrainOnStart = true;
        [SerializeField] private bool spawnResourcesOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugMode;
        [SerializeField] private bool skipToGameplay = true;

        private void Awake()
        {
            Debug.Log("[GameBootstrapper] Initializing RuneRealm...");

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;

            // Initialize render settings for Skyrim atmosphere
            InitializeRenderSettings();
        }

        private void Start()
        {
            // Systems are initialized through their own Awake() via scene hierarchy,
            // but we ensure everything is connected here.

            SetupWorld();
            SetupPlayer();
            SetupCamera();
            SetupUI();

            // Start the game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }

            Debug.Log("[GameBootstrapper] RuneRealm initialized successfully!");
            Debug.Log($"[GameBootstrapper] Total skills: {SkillConstants.TotalSkills}");
            Debug.Log($"[GameBootstrapper] Inventory slots: {InventoryManager.InventorySize}");

            if (enableDebugMode)
                EnableDebugMode();
        }

        private void InitializeRenderSettings()
        {
            // Skyrim-style atmospheric fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.fogColor = new Color(0.65f, 0.7f, 0.75f, 1f);

            // Ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.65f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);

            // Skybox
            RenderSettings.reflectionIntensity = 0.5f;
        }

        private void SetupWorld()
        {
            if (generateTerrainOnStart)
            {
                var terrainGen = FindAnyObjectByType<TerrainGenerator>();
                if (terrainGen != null)
                {
                    terrainGen.GenerateTerrain();
                    Debug.Log("[GameBootstrapper] Terrain generated.");

                    if (spawnResourcesOnStart)
                    {
                        var resourceSpawner = FindAnyObjectByType<ResourceSpawner>();
                        if (resourceSpawner != null)
                        {
                            var terrain = terrainGen.GetComponent<Terrain>();
                            if (terrain != null)
                            {
                                resourceSpawner.SpawnResources(
                                    terrain.terrainData,
                                    terrainGen.transform.position
                                );
                            }
                        }
                    }
                }
            }
        }

        private void SetupPlayer()
        {
            var existingPlayer = FindAnyObjectByType<PlayerController>();
            if (existingPlayer != null)
            {
                // Player already in scene, position at spawn
                existingPlayer.transform.position = playerSpawnPosition;
                existingPlayer.transform.eulerAngles = playerSpawnRotation;
                return;
            }

            if (playerPrefab != null)
            {
                GameObject player = Instantiate(playerPrefab, playerSpawnPosition,
                    Quaternion.Euler(playerSpawnRotation));
                player.name = "Player";
            }
            else
            {
                // Create a basic player
                CreateDefaultPlayer();
            }
        }

        private void CreateDefaultPlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = playerSpawnPosition;
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            // Character controller
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Player components
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerInteraction>();
            player.AddComponent<WoodcuttingAction>();
            player.AddComponent<MiningAction>();
            player.AddComponent<FishingAction>();
            player.AddComponent<CookingAction>();
            player.AddComponent<SmithingAction>();
            player.AddComponent<CraftingAction>();

            // Visual representation (capsule placeholder)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            Destroy(visual.GetComponent<Collider>());

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.4f, 0.35f, 0.3f);
            }
        }

        private void SetupCamera()
        {
            if (Camera.main != null)
            {
                var existingCam = Camera.main.GetComponent<ThirdPersonCamera>();
                if (existingCam != null) return;
            }

            if (cameraPrefab != null)
            {
                Instantiate(cameraPrefab);
            }
            else
            {
                // Set up existing main camera or create one
                Camera cam = Camera.main;
                if (cam == null)
                {
                    GameObject camGO = new GameObject("Main Camera");
                    camGO.tag = "MainCamera";
                    cam = camGO.AddComponent<Camera>();
                    camGO.AddComponent<AudioListener>();
                }

                if (cam.GetComponent<ThirdPersonCamera>() == null)
                    cam.gameObject.AddComponent<ThirdPersonCamera>();

                // Camera post-processing feel
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 1500f;
                cam.fieldOfView = 65f;
            }
        }

        private void SetupUI()
        {
            // UI is typically set up in the scene already
            // But ensure HUD exists
            if (HUDManager.Instance == null && hudCanvasPrefab != null)
            {
                Instantiate(hudCanvasPrefab);
            }
        }

        private void EnableDebugMode()
        {
            Debug.Log("[GameBootstrapper] Debug mode enabled.");

            // Grant some starting XP for testing
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.AddXP(SkillType.Woodcutting, 500);
                SkillManager.Instance.AddXP(SkillType.Mining, 500);
                SkillManager.Instance.AddXP(SkillType.Fishing, 500);
                SkillManager.Instance.AddXP(SkillType.Cooking, 300);
            }
        }
    }
}
