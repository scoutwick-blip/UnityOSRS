using UnityEngine;
using System.Collections.Generic;
using RuneRealm.Player;

namespace RuneRealm.World
{
    /// <summary>
    /// Defines world biomes inspired by both Skyrim holds and OSRS regions.
    /// Each biome has unique resource availability, aesthetics, and atmosphere.
    /// </summary>
    public class BiomeSystem : MonoBehaviour
    {
        public static BiomeSystem Instance { get; private set; }

        [SerializeField] private BiomeDefinition[] biomes;
        [SerializeField] private float biomeTransitionWidth = 20f;
        [SerializeField] private float biomeCheckInterval = 2f;

        private BiomeDefinition currentBiome;
        private float biomeCheckTimer;

        public BiomeDefinition CurrentBiome => currentBiome;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (biomes == null || biomes.Length == 0)
                InitializeDefaultBiomes();
        }

        private void InitializeDefaultBiomes()
        {
            biomes = new BiomeDefinition[]
            {
                new BiomeDefinition
                {
                    biomeName = "Lumbridge Meadows",
                    biomeType = BiomeType.Temperate,
                    description = "Rolling green fields with gentle streams. A safe haven for new adventurers.",
                    fogColor = new Color(0.7f, 0.8f, 0.85f),
                    fogDensity = 0.003f,
                    ambientColor = new Color(0.65f, 0.7f, 0.65f),
                    grassColor = new Color(0.3f, 0.6f, 0.2f),
                    availableResources = new ResourceAvailability[]
                    {
                        new ResourceAvailability { resource = "Normal Tree", minLevel = 1, abundance = 0.8f },
                        new ResourceAvailability { resource = "Oak Tree", minLevel = 15, abundance = 0.4f },
                        new ResourceAvailability { resource = "Copper Rock", minLevel = 1, abundance = 0.6f },
                        new ResourceAvailability { resource = "Tin Rock", minLevel = 1, abundance = 0.6f },
                        new ResourceAvailability { resource = "Shrimp Spot", minLevel = 1, abundance = 0.5f },
                    }
                },
                new BiomeDefinition
                {
                    biomeName = "Falador Highlands",
                    biomeType = BiomeType.Highland,
                    description = "Windswept hills rich in ore deposits. A miner's paradise.",
                    fogColor = new Color(0.6f, 0.65f, 0.75f),
                    fogDensity = 0.005f,
                    ambientColor = new Color(0.55f, 0.6f, 0.65f),
                    grassColor = new Color(0.25f, 0.45f, 0.2f),
                    availableResources = new ResourceAvailability[]
                    {
                        new ResourceAvailability { resource = "Iron Rock", minLevel = 15, abundance = 0.7f },
                        new ResourceAvailability { resource = "Coal Rock", minLevel = 30, abundance = 0.5f },
                        new ResourceAvailability { resource = "Mithril Rock", minLevel = 55, abundance = 0.2f },
                        new ResourceAvailability { resource = "Willow Tree", minLevel = 30, abundance = 0.3f },
                    }
                },
                new BiomeDefinition
                {
                    biomeName = "Darkwood Forest",
                    biomeType = BiomeType.DenseForest,
                    description = "An ancient forest of towering trees, thick with undergrowth and mystery.",
                    fogColor = new Color(0.25f, 0.35f, 0.25f),
                    fogDensity = 0.01f,
                    ambientColor = new Color(0.3f, 0.4f, 0.25f),
                    grassColor = new Color(0.15f, 0.35f, 0.1f),
                    availableResources = new ResourceAvailability[]
                    {
                        new ResourceAvailability { resource = "Willow Tree", minLevel = 30, abundance = 0.6f },
                        new ResourceAvailability { resource = "Maple Tree", minLevel = 45, abundance = 0.4f },
                        new ResourceAvailability { resource = "Yew Tree", minLevel = 60, abundance = 0.15f },
                        new ResourceAvailability { resource = "Herb Patch", minLevel = 9, abundance = 0.3f },
                    }
                },
                new BiomeDefinition
                {
                    biomeName = "Frostpeak Mountains",
                    biomeType = BiomeType.Snowy,
                    description = "Snow-capped peaks hiding the rarest ores. Beware the cold.",
                    fogColor = new Color(0.8f, 0.85f, 0.9f),
                    fogDensity = 0.008f,
                    ambientColor = new Color(0.7f, 0.75f, 0.85f),
                    grassColor = new Color(0.6f, 0.65f, 0.6f),
                    availableResources = new ResourceAvailability[]
                    {
                        new ResourceAvailability { resource = "Adamantite Rock", minLevel = 70, abundance = 0.3f },
                        new ResourceAvailability { resource = "Runite Rock", minLevel = 85, abundance = 0.05f },
                        new ResourceAvailability { resource = "Magic Tree", minLevel = 75, abundance = 0.05f },
                    }
                },
                new BiomeDefinition
                {
                    biomeName = "Karamja Coastline",
                    biomeType = BiomeType.Tropical,
                    description = "Sun-baked shores teeming with exotic fish and tropical resources.",
                    fogColor = new Color(0.7f, 0.8f, 0.9f),
                    fogDensity = 0.002f,
                    ambientColor = new Color(0.8f, 0.75f, 0.6f),
                    grassColor = new Color(0.4f, 0.65f, 0.25f),
                    availableResources = new ResourceAvailability[]
                    {
                        new ResourceAvailability { resource = "Lobster Spot", minLevel = 40, abundance = 0.6f },
                        new ResourceAvailability { resource = "Swordfish Spot", minLevel = 50, abundance = 0.3f },
                        new ResourceAvailability { resource = "Teak Tree", minLevel = 35, abundance = 0.4f },
                        new ResourceAvailability { resource = "Mahogany Tree", minLevel = 50, abundance = 0.2f },
                    }
                },
                new BiomeDefinition
                {
                    biomeName = "Morytania Swamp",
                    biomeType = BiomeType.Swamp,
                    description = "A dark, foggy marshland. The air itself feels cursed.",
                    fogColor = new Color(0.2f, 0.25f, 0.2f),
                    fogDensity = 0.02f,
                    ambientColor = new Color(0.25f, 0.3f, 0.2f),
                    grassColor = new Color(0.2f, 0.3f, 0.15f),
                    availableResources = new ResourceAvailability[]
                    {
                        new ResourceAvailability { resource = "Snape Grass", minLevel = 1, abundance = 0.4f },
                        new ResourceAvailability { resource = "Mort Myre Fungus", minLevel = 1, abundance = 0.5f },
                    }
                },
            };
        }

        private void Update()
        {
            biomeCheckTimer -= Time.deltaTime;
            if (biomeCheckTimer > 0) return;
            biomeCheckTimer = biomeCheckInterval;

            var player = PlayerController.Instance;
            if (player == null) return;

            var biome = GetBiomeAt(player.transform.position);
            if (biome != null && biome != currentBiome)
            {
                currentBiome = biome;
                ApplyBiomeAtmosphere(biome);
                Debug.Log($"[BiomeSystem] Entered biome: {biome.biomeName}");
            }
        }

        public BiomeDefinition GetBiomeAt(Vector3 worldPosition)
        {
            if (biomes == null || biomes.Length == 0) return null;

            // Use noise-based biome selection
            float biomeNoise = Mathf.PerlinNoise(
                worldPosition.x * 0.002f + 500f,
                worldPosition.z * 0.002f + 500f
            );

            // Add height influence
            float heightInfluence = worldPosition.y / 200f; // Normalized height

            int biomeIndex = Mathf.FloorToInt(biomeNoise * biomes.Length);
            biomeIndex = Mathf.Clamp(biomeIndex, 0, biomes.Length - 1);

            // High elevation forces snowy biome
            if (heightInfluence > 0.6f)
            {
                for (int i = 0; i < biomes.Length; i++)
                {
                    if (biomes[i].biomeType == BiomeType.Snowy)
                        return biomes[i];
                }
            }

            return biomes[biomeIndex];
        }

        public void ApplyBiomeAtmosphere(BiomeDefinition biome)
        {
            if (biome == null) return;

            RenderSettings.fogColor = biome.fogColor;
            RenderSettings.fogDensity = biome.fogDensity;
            RenderSettings.ambientLight = biome.ambientColor;
            RenderSettings.fog = true;
        }
    }

    [System.Serializable]
    public class BiomeDefinition
    {
        public string biomeName;
        public BiomeType biomeType;
        public string description;
        public Color fogColor;
        public float fogDensity;
        public Color ambientColor;
        public Color grassColor;
        public ResourceAvailability[] availableResources;
    }

    [System.Serializable]
    public class ResourceAvailability
    {
        public string resource;
        public int minLevel;
        [Range(0f, 1f)] public float abundance;
    }

    public enum BiomeType
    {
        Temperate,
        Highland,
        DenseForest,
        Snowy,
        Tropical,
        Swamp,
        Desert,
        Volcanic
    }
}
