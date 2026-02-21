using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
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
        private static bool bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            bootstrapped = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (bootstrapped) return;
            if (FindAnyObjectByType<GameBootstrapper>() != null) return;

            Debug.Log("[GameBootstrapper] No bootstrapper in scene — auto-creating.");
            var go = new GameObject("GameBootstrapper");
            go.AddComponent<GameBootstrapper>();
        }

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

        // Cached terrain reference — Terrain.activeTerrain can be null if the
        // terrain component hasn't registered yet, so we grab it directly.
        private Terrain generatedTerrain;

        private void Awake()
        {
            // Prevent duplicate bootstrappers
            if (bootstrapped)
            {
                Debug.LogWarning("[GameBootstrapper] Duplicate detected — destroying.");
                Destroy(gameObject);
                return;
            }
            bootstrapped = true;

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
            // --- TERRAIN ---
            // Always ensure we have a terrain. Try generating, then fallback.
            SetupWorld();

            Debug.Log($"[GameBootstrapper] After SetupWorld — " +
                $"generatedTerrain: {generatedTerrain != null}, " +
                $"activeTerrain: {Terrain.activeTerrain != null}");

            SetupPlayer();
            SetupCamera();
            SetupUI();
            SetupLighting();

            // Non-critical systems — failures must not break the game
            try { SetupWeather(); }
            catch (System.Exception e) { Debug.LogWarning($"[GameBootstrapper] Weather setup failed: {e.Message}"); }

            try { SetupNPCs(); }
            catch (System.Exception e) { Debug.LogWarning($"[GameBootstrapper] NPC setup failed: {e.Message}"); }

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
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }

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

            // Non-critical managers — wrap so core game still works if these fail
            try
            {
                if (QuestManager.Instance == null)
                {
                    var go = new GameObject("QuestManager");
                    go.AddComponent<QuestManager>();
                    DontDestroyOnLoad(go);
                }

                if (BiomeSystem.Instance == null)
                {
                    var go = new GameObject("BiomeSystem");
                    go.AddComponent<BiomeSystem>();
                }

                if (DialogueManager.Instance == null)
                {
                    var go = new GameObject("DialogueManager");
                    go.AddComponent<DialogueManager>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameBootstrapper] Non-critical manager init failed: {e.Message}");
            }
        }

        private void SetupWorld()
        {
            // ----- Step 1: Check for ANY existing terrain first -----
            generatedTerrain = Terrain.activeTerrain;
            if (generatedTerrain == null)
                generatedTerrain = FindAnyObjectByType<Terrain>();

            if (generatedTerrain != null)
            {
                Debug.Log($"[GameBootstrapper] Found existing terrain: {generatedTerrain.name}");
            }
            else
            {
                // ----- Step 2: Generate terrain from scratch -----
                Debug.Log("[GameBootstrapper] No terrain in scene — generating procedurally...");
                try
                {
                    var terrainGen = FindAnyObjectByType<TerrainGenerator>();
                    if (terrainGen == null)
                    {
                        var terrainGO = new GameObject("TerrainGenerator");
                        terrainGen = terrainGO.AddComponent<TerrainGenerator>();
                    }

                    terrainGen.GenerateTerrain();

                    generatedTerrain = terrainGen.GetComponent<Terrain>();
                    if (generatedTerrain == null)
                        generatedTerrain = FindAnyObjectByType<Terrain>();

                    Debug.Log($"[GameBootstrapper] Terrain generation complete. " +
                        $"Found: {generatedTerrain != null}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GameBootstrapper] GenerateTerrain threw: {e}");
                    // Still try to find anything that was partially created
                    generatedTerrain = FindAnyObjectByType<Terrain>();
                }
            }

            // ----- Step 3: Last resort — create a flat emergency terrain -----
            if (generatedTerrain == null)
            {
                Debug.LogWarning("[GameBootstrapper] All terrain methods failed — creating emergency terrain.");
                generatedTerrain = CreateEmergencyTerrain();
            }

            // Ensure terrain is enabled
            generatedTerrain.gameObject.SetActive(true);
            generatedTerrain.enabled = true;
            Debug.Log($"[GameBootstrapper] Terrain ready: {generatedTerrain.name}, " +
                $"pos={generatedTerrain.transform.position}, " +
                $"size={generatedTerrain.terrainData.size}");

            // Bake NavMesh on terrain for NPC pathfinding (non-critical)
            try { BakeNavMesh(); }
            catch (System.Exception e) { Debug.LogWarning($"[GameBootstrapper] NavMesh bake failed: {e.Message}"); }

            // Spawn resources on terrain
            if (generatedTerrain != null && generatedTerrain.terrainData != null)
            {
                try
                {
                    var resourceSpawner = FindAnyObjectByType<ResourceSpawner>();
                    if (resourceSpawner == null)
                    {
                        var rsGO = new GameObject("ResourceSpawner");
                        resourceSpawner = rsGO.AddComponent<ResourceSpawner>();
                    }

                    resourceSpawner.SpawnResources(
                        generatedTerrain.terrainData,
                        generatedTerrain.transform.position
                    );
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[GameBootstrapper] Resource spawning failed: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a minimal flat Unity Terrain as a last-resort ground surface.
        /// Uses a proper Terrain component so all terrain-dependent systems still work.
        /// </summary>
        private Terrain CreateEmergencyTerrain()
        {
            var td = new TerrainData();
            td.heightmapResolution = 33;
            td.size = new Vector3(512, 50, 512);

            // Set a uniform height so the surface isn't at Y=0
            float[,] heights = new float[33, 33];
            float normalizedHeight = 0.5f; // 50 * 0.5 = Y offset of 25
            for (int x = 0; x < 33; x++)
                for (int z = 0; z < 33; z++)
                    heights[x, z] = normalizedHeight;
            td.SetHeights(0, 0, heights);

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "EmergencyTerrain";
            go.transform.position = Vector3.zero;

            var terrain = go.GetComponent<Terrain>();

            // Assign a working material
            bool urpActive = GraphicsSettings.defaultRenderPipeline != null;
            string[] shaderNames = urpActive
                ? new[] { "Universal Render Pipeline/Terrain/Lit", "Nature/Terrain/Standard" }
                : new[] { "Nature/Terrain/Standard", "Standard" };

            foreach (var name in shaderNames)
            {
                var shader = Shader.Find(name);
                if (shader != null)
                {
                    terrain.materialTemplate = new Material(shader);
                    break;
                }
            }

            // Paint it green with a procedural terrain layer
            var tex = new Texture2D(2, 2);
            var green = new Color(0.28f, 0.38f, 0.15f);
            tex.SetPixels(new[] { green, green, green, green });
            tex.Apply();

            var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(10, 10) };
            td.terrainLayers = new[] { layer };

            float[,,] alphamaps = new float[td.alphamapWidth, td.alphamapHeight, 1];
            for (int x = 0; x < td.alphamapWidth; x++)
                for (int z = 0; z < td.alphamapHeight; z++)
                    alphamaps[x, z, 0] = 1f;
            td.SetAlphamaps(0, 0, alphamaps);

            Debug.Log($"[GameBootstrapper] Emergency terrain created at surface Y={normalizedHeight * td.size.y}");
            return terrain;
        }

        private void BakeNavMesh()
        {
            // Use Unity.AI.Navigation NavMeshSurface to bake at runtime
            var terrain = generatedTerrain ?? Terrain.activeTerrain;
            if (terrain == null) return;

            var surfaceType = System.Type.GetType(
                "Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                Debug.LogWarning("[GameBootstrapper] NavMeshSurface type not found. NPCs may not pathfind.");
                return;
            }

            var surface = terrain.gameObject.GetComponent(surfaceType);
            if (surface == null)
                surface = terrain.gameObject.AddComponent(surfaceType);

            // Call BuildNavMesh()
            var buildMethod = surfaceType.GetMethod("BuildNavMesh");
            if (buildMethod != null)
            {
                buildMethod.Invoke(surface, null);
                Debug.Log("[GameBootstrapper] NavMesh baked on terrain.");
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
            var terrain = generatedTerrain ?? Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;
                pos.y = terrainY + 2f; // small offset above surface
                Debug.Log($"[GameBootstrapper] Spawn on terrain at Y={pos.y} (terrain height={terrainY})");
            }
            else
            {
                Debug.LogWarning($"[GameBootstrapper] No terrain for spawn height, using default Y={pos.y}");
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
            // Note: EquipmentManager is NOT added here — it's already a singleton
            // created by EnsureManagers(). Adding it here would trigger the singleton
            // guard's Destroy(gameObject) and destroy the entire Player.
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

            Debug.Log($"[GameBootstrapper] Player created at {spawnPos}");
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            ThirdPersonCamera tpc = null;

            if (cam != null)
                tpc = cam.GetComponent<ThirdPersonCamera>();

            if (cameraPrefab != null && tpc == null)
            {
                var go = Instantiate(cameraPrefab);
                cam = go.GetComponentInChildren<Camera>();
                if (cam != null) tpc = cam.GetComponent<ThirdPersonCamera>();
            }

            if (cam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            if (tpc == null)
                tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();

            // Camera settings
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = 65f;

            // If URP is active, ensure the camera has UniversalAdditionalCameraData
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                var urpCamDataType = System.Type.GetType(
                    "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpCamDataType != null && cam.GetComponent(urpCamDataType) == null)
                    cam.gameObject.AddComponent(urpCamDataType);
            }

            // Immediately position camera behind the player so it doesn't
            // sit at the origin waiting for ThirdPersonCamera.LateUpdate()
            var player = PlayerController.Instance;
            if (player != null)
            {
                tpc.SetTarget(player.transform);
                Vector3 targetPos = player.transform.position + new Vector3(0f, 1.6f, 0f);
                Vector3 behind = targetPos + Vector3.back * 5f;
                cam.transform.position = behind;
                cam.transform.LookAt(targetPos);
                Debug.Log($"[GameBootstrapper] Camera positioned at {behind}, looking at player at {player.transform.position}");
            }
            else
            {
                Debug.LogWarning("[GameBootstrapper] PlayerController.Instance is null during SetupCamera!");
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
                esGO.AddComponent<InputSystemUIInputModule>();
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

            // Dialogue panel (for NPC conversations)
            BuildDialogueUI(canvasGO.transform);

            Debug.Log("[GameBootstrapper] UI built at runtime.");
        }

        private void BuildDialogueUI(Transform canvasParent)
        {
            var dm = DialogueManager.Instance;
            if (dm == null) return;

            // Dialogue panel — bottom of screen
            var panelGO = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasParent, false);
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.1f, 0.02f);
            panelRT.anchorMax = new Vector2(0.9f, 0.28f);
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            var panelImg = panelGO.GetComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.85f);
            panelImg.raycastTarget = true;

            // NPC name
            var nameGO = new GameObject("NPCName", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            nameGO.transform.SetParent(panelGO.transform, false);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.02f, 0.75f);
            nameRT.anchorMax = new Vector2(0.5f, 0.98f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
            var nameTMP = nameGO.GetComponent<TMPro.TextMeshProUGUI>();
            nameTMP.text = "NPC";
            nameTMP.fontSize = 18;
            nameTMP.color = new Color(0.9f, 0.8f, 0.3f);
            nameTMP.fontStyle = TMPro.FontStyles.Bold;

            // Dialogue text
            var textGO = new GameObject("DialogueText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textGO.transform.SetParent(panelGO.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.02f, 0.05f);
            textRT.anchorMax = new Vector2(0.98f, 0.72f);
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;
            var textTMP = textGO.GetComponent<TMPro.TextMeshProUGUI>();
            textTMP.text = "";
            textTMP.fontSize = 16;
            textTMP.color = Color.white;

            // Choices container
            var choicesGO = new GameObject("Choices", typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            choicesGO.transform.SetParent(panelGO.transform, false);
            var choicesRT = choicesGO.GetComponent<RectTransform>();
            choicesRT.anchorMin = new Vector2(0.6f, 0.05f);
            choicesRT.anchorMax = new Vector2(0.98f, 0.72f);
            choicesRT.offsetMin = choicesRT.offsetMax = Vector2.zero;

            // Wire into DialogueManager
            var dmType = typeof(DialogueManager);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            dmType.GetField("dialoguePanel", flags)?.SetValue(dm, panelGO);
            dmType.GetField("npcNameText", flags)?.SetValue(dm, nameTMP);
            dmType.GetField("dialogueText", flags)?.SetValue(dm, textTMP);
            dmType.GetField("choicesContainer", flags)?.SetValue(dm, choicesGO.transform);

            panelGO.SetActive(false);
            Debug.Log("[GameBootstrapper] Dialogue UI built.");
        }

        private void SetupLighting()
        {
            // Create a directional light if none exists
            var existingLight = FindAnyObjectByType<Light>();
            Light sun;
            if (existingLight != null && existingLight.type == LightType.Directional)
            {
                sun = existingLight;
            }
            else
            {
                var lightGO = new GameObject("Directional Light");
                sun = lightGO.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.95f, 0.85f);
                sun.intensity = 1.2f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(50f, 170f, 0f);
                Debug.Log("[GameBootstrapper] Created directional light.");
            }

            // Wire it into GameManager for day/night cycle
            if (GameManager.Instance != null)
            {
                var gmType = typeof(GameManager);
                var field = gmType.GetField("directionalLight",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(GameManager.Instance, sun);
            }

            // Wire into WeatherSystem too
            if (WeatherSystem.Instance != null)
            {
                var wsType = typeof(WeatherSystem);
                var field = wsType.GetField("sunLight",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(WeatherSystem.Instance, sun);
            }

            // DayNightAmbience
            if (FindAnyObjectByType<DayNightAmbience>() == null)
            {
                var ambienceGO = new GameObject("DayNightAmbience");
                ambienceGO.AddComponent<DayNightAmbience>();
            }
        }

        private void SetupWeather()
        {
            if (WeatherSystem.Instance != null) return;

            var go = new GameObject("WeatherSystem");
            var ws = go.AddComponent<WeatherSystem>();

            // Create a rain particle system
            var rainGO = new GameObject("RainParticles");
            rainGO.transform.SetParent(go.transform);
            var rainPS = rainGO.AddComponent<ParticleSystem>();
            ConfigureRainParticles(rainPS);

            // Wire rain particles into weather system
            var wsType = typeof(WeatherSystem);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            wsType.GetField("rainParticles", flags)?.SetValue(ws, rainPS);

            // Start with clear weather initially
            rainPS.Stop();
            Debug.Log("[GameBootstrapper] WeatherSystem created.");
        }

        private void ConfigureRainParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.maxParticles = 5000;
            main.startLifetime = 2f;
            main.startSpeed = 15f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // Weather system controls this

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f);
            shape.position = new Vector3(0, 30f, 0);

            // Make rain follow the camera
            ps.gameObject.AddComponent<FollowCamera>();

            ps.Stop();
        }

        private void SetupNPCs()
        {
            var spawner = FindAnyObjectByType<NPCSpawner>();
            if (spawner == null)
            {
                var go = new GameObject("NPCSpawner");
                spawner = go.AddComponent<NPCSpawner>();
            }

            // Position NPCs near the player spawn
            var terrain = generatedTerrain ?? Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(playerSpawnPosition)
                                 + terrain.transform.position.y;
                spawner.SetBasePosition(playerSpawnPosition, terrainY);
            }

            spawner.SpawnAllNPCs();
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

    /// <summary>
    /// Simple helper to make a particle system follow the main camera.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
                transform.position = Camera.main.transform.position;
        }
    }
}
