using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

            // Ensure all singleton managers exist before anything else
            EnsureManagers();
        }

        private void Start()
        {
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

            // Skybox — keep reflections off so terrain/surfaces stay matte
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        }

        /// <summary>
        /// Creates singleton manager GameObjects if they don't already exist in the scene.
        /// </summary>
        private void EnsureManagers()
        {
            if (SkillManager.Instance == null)
            {
                var go = new GameObject("SkillManager");
                go.AddComponent<SkillManager>();
                DontDestroyOnLoad(go);
            }

            if (InventoryManager.Instance == null)
            {
                var go = new GameObject("InventoryManager");
                go.AddComponent<InventoryManager>();
                DontDestroyOnLoad(go);
            }

            if (EquipmentManager.Instance == null)
            {
                var go = new GameObject("EquipmentManager");
                go.AddComponent<EquipmentManager>();
                DontDestroyOnLoad(go);
            }
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
            Vector3 spawnPos = GetSpawnPositionOnTerrain();

            var existingPlayer = FindAnyObjectByType<PlayerController>();
            if (existingPlayer != null)
            {
                // Player already in scene, position at spawn
                existingPlayer.transform.position = spawnPos;
                existingPlayer.transform.eulerAngles = playerSpawnRotation;
                return;
            }

            if (playerPrefab != null)
            {
                GameObject player = Instantiate(playerPrefab, spawnPos,
                    Quaternion.Euler(playerSpawnRotation));
                player.name = "Player";
            }
            else
            {
                // Create a basic player
                CreateDefaultPlayer(spawnPos);
            }
        }

        private Vector3 GetSpawnPositionOnTerrain()
        {
            Vector3 pos = playerSpawnPosition;

            // Sample terrain height at spawn XZ so the player lands on top
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
                pos.y = terrainY + 2f; // small offset above surface
            }

            return pos;
        }

        private void CreateDefaultPlayer(Vector3 spawnPos)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = spawnPos;
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
            player.AddComponent<EquipmentManager>();
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
            if (hudCanvasPrefab != null)
            {
                Instantiate(hudCanvasPrefab);
                return;
            }

            // Build the entire UI at runtime when no prefab is available
            // Ensure EventSystem exists for input
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                var inputModule = esGO.AddComponent<StandaloneInputModule>();
                // Clear Submit/Cancel button names — the default Input Manager
                // may not have them configured, which causes ArgumentException
                inputModule.submitButton = "";
                inputModule.cancelButton = "";
            }

            // Main Canvas
            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // HUD layer (always visible in gameplay)
            var hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(canvasGO.transform, false);
            hudGO.AddComponent<RectTransform>().anchorMin = Vector2.zero;
            hudGO.GetComponent<RectTransform>().anchorMax = Vector2.one;
            hudGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            hudGO.AddComponent<HUDManager>();

            // Inventory panel
            var invGO = new GameObject("InventoryUI");
            invGO.transform.SetParent(canvasGO.transform, false);
            invGO.AddComponent<RectTransform>().anchorMin = Vector2.zero;
            invGO.GetComponent<RectTransform>().anchorMax = Vector2.one;
            invGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            invGO.AddComponent<InventoryUI>();

            // Skill menu panel
            var skillGO = new GameObject("SkillMenuUI");
            skillGO.transform.SetParent(canvasGO.transform, false);
            skillGO.AddComponent<RectTransform>().anchorMin = Vector2.zero;
            skillGO.GetComponent<RectTransform>().anchorMax = Vector2.one;
            skillGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            skillGO.AddComponent<SkillMenuUI>();

            Debug.Log("[GameBootstrapper] UI built at runtime.");
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
