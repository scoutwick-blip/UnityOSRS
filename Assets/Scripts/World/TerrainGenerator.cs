using UnityEngine;

namespace RuneRealm.World
{
    /// <summary>
    /// Procedural terrain generator creating Skyrim-style landscapes.
    /// Uses layered Perlin noise for realistic terrain with mountains, valleys, and plains.
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int terrainWidth = 513;
        [SerializeField] private int terrainLength = 513;
        [SerializeField] private int terrainHeight = 200;
        [SerializeField] private float terrainScale = 1f;

        [Header("Noise Layers")]
        [SerializeField] private NoiseLayer[] noiseLayers;
        [SerializeField] private int seed = 42;

        [Header("Biome Painting")]
        [SerializeField] private TerrainLayer[] terrainLayers; // Grass, Rock, Snow, Sand, Dirt
        [SerializeField] private float snowHeight = 0.7f;
        [SerializeField] private float rockSteepness = 0.5f;
        [SerializeField] private float sandHeight = 0.05f;

        [Header("Detail")]
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] rockPrefabs;
        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private float treeSpawnChance = 0.02f;
        [SerializeField] private float rockSpawnChance = 0.005f;
        [SerializeField] private float minTreeHeight = 0.15f;
        [SerializeField] private float maxTreeHeight = 0.6f;

        [Header("Water")]
        [SerializeField] private GameObject waterPlanePrefab;
        [SerializeField] private float waterLevel = 0.08f;

        private Terrain terrain;
        private TerrainData terrainData;

        [System.Serializable]
        public class NoiseLayer
        {
            public string name = "Layer";
            public float frequency = 0.01f;
            public float amplitude = 1f;
            public int octaves = 4;
            public float persistence = 0.5f;
            public float lacunarity = 2f;
            public bool useRidged;
        }

        private void Awake()
        {
            if (noiseLayers == null || noiseLayers.Length == 0)
            {
                noiseLayers = new NoiseLayer[]
                {
                    new NoiseLayer { name = "Continental", frequency = 0.003f, amplitude = 0.5f, octaves = 3, persistence = 0.5f, lacunarity = 2f },
                    new NoiseLayer { name = "Mountains", frequency = 0.008f, amplitude = 0.35f, octaves = 5, persistence = 0.55f, lacunarity = 2.2f, useRidged = true },
                    new NoiseLayer { name = "Hills", frequency = 0.02f, amplitude = 0.1f, octaves = 4, persistence = 0.5f, lacunarity = 2f },
                    new NoiseLayer { name = "Detail", frequency = 0.05f, amplitude = 0.05f, octaves = 3, persistence = 0.45f, lacunarity = 2f },
                };
            }
        }

        public void GenerateTerrain()
        {
            SetupTerrain();
            GenerateHeightmap();
            PaintTextures();
            PlaceWater();
            PlaceVegetation();
        }

        private void SetupTerrain()
        {
            terrain = GetComponent<Terrain>();
            if (terrain == null)
                terrain = gameObject.AddComponent<Terrain>();

            var collider = GetComponent<TerrainCollider>();
            if (collider == null)
                collider = gameObject.AddComponent<TerrainCollider>();

            terrainData = new TerrainData();
            terrainData.heightmapResolution = terrainWidth;
            terrainData.size = new Vector3(terrainWidth * terrainScale, terrainHeight, terrainLength * terrainScale);

            if (terrainLayers != null && terrainLayers.Length > 0)
                terrainData.terrainLayers = terrainLayers;

            terrain.terrainData = terrainData;
            collider.terrainData = terrainData;
        }

        private void GenerateHeightmap()
        {
            float[,] heights = new float[terrainWidth, terrainLength];
            System.Random rng = new System.Random(seed);
            float offsetX = (float)rng.NextDouble() * 10000f;
            float offsetZ = (float)rng.NextDouble() * 10000f;

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int z = 0; z < terrainLength; z++)
                {
                    float height = 0f;

                    foreach (var layer in noiseLayers)
                    {
                        height += SampleNoise(x + offsetX, z + offsetZ, layer);
                    }

                    // Normalize
                    height = Mathf.Clamp01(height);

                    // Apply edge falloff to create island/bounded area
                    float edgeFalloff = GetEdgeFalloff(x, z);
                    height *= edgeFalloff;

                    heights[x, z] = height;
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        private float SampleNoise(float x, float z, NoiseLayer layer)
        {
            float value = 0f;
            float frequency = layer.frequency;
            float amplitude = layer.amplitude;
            float maxValue = 0f;

            for (int o = 0; o < layer.octaves; o++)
            {
                float sampleX = x * frequency;
                float sampleZ = z * frequency;

                float noise = Mathf.PerlinNoise(sampleX, sampleZ);

                if (layer.useRidged)
                {
                    noise = 1f - Mathf.Abs(noise * 2f - 1f);
                    noise *= noise; // Sharpen ridges
                }

                value += noise * amplitude;
                maxValue += amplitude;

                frequency *= layer.lacunarity;
                amplitude *= layer.persistence;
            }

            return value / maxValue * layer.amplitude;
        }

        private float GetEdgeFalloff(int x, int z)
        {
            float nx = (float)x / terrainWidth * 2f - 1f;
            float nz = (float)z / terrainLength * 2f - 1f;
            float distFromCenter = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(nz));

            float falloffStart = 0.7f;
            if (distFromCenter < falloffStart)
                return 1f;

            float t = (distFromCenter - falloffStart) / (1f - falloffStart);
            return Mathf.Clamp01(1f - t * t);
        }

        private void PaintTextures()
        {
            if (terrainLayers == null || terrainLayers.Length < 3) return;

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int layerCount = terrainData.alphamapLayers;

            float[,,] alphamaps = new float[alphamapWidth, alphamapHeight, layerCount];

            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int z = 0; z < alphamapHeight; z++)
                {
                    float normalizedX = (float)x / alphamapWidth;
                    float normalizedZ = (float)z / alphamapHeight;

                    float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) / terrainHeight;
                    float steepness = terrainData.GetSteepness(normalizedX, normalizedZ) / 90f;

                    // Default: grass
                    float grass = 1f;
                    float rock = 0f;
                    float snow = 0f;
                    float sand = 0f;
                    float dirt = 0f;

                    // Sand at low elevation
                    if (height < sandHeight)
                    {
                        sand = 1f - (height / sandHeight);
                        grass = 1f - sand;
                    }

                    // Rock on steep slopes
                    if (steepness > rockSteepness)
                    {
                        float rockBlend = (steepness - rockSteepness) / (1f - rockSteepness);
                        rock = rockBlend;
                        grass *= (1f - rockBlend);
                    }

                    // Snow at high elevation
                    if (height > snowHeight)
                    {
                        float snowBlend = (height - snowHeight) / (1f - snowHeight);
                        snow = snowBlend;
                        grass *= (1f - snowBlend);
                        rock *= (1f - snowBlend * 0.5f);
                    }

                    // Dirt at transition zones
                    if (height > 0.1f && height < 0.3f)
                    {
                        dirt = 0.2f * (1f - steepness);
                        grass -= dirt * 0.5f;
                    }

                    // Normalize
                    float total = grass + rock + snow + sand + dirt;
                    if (total > 0)
                    {
                        if (layerCount > 0) alphamaps[x, z, 0] = grass / total;
                        if (layerCount > 1) alphamaps[x, z, 1] = rock / total;
                        if (layerCount > 2) alphamaps[x, z, 2] = snow / total;
                        if (layerCount > 3) alphamaps[x, z, 3] = sand / total;
                        if (layerCount > 4) alphamaps[x, z, 4] = dirt / total;
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        private void PlaceWater()
        {
            if (waterPlanePrefab == null) return;

            float waterWorldHeight = waterLevel * terrainHeight;
            Vector3 waterPos = new Vector3(
                terrainWidth * terrainScale / 2f,
                waterWorldHeight,
                terrainLength * terrainScale / 2f
            );

            GameObject water = Instantiate(waterPlanePrefab, waterPos, Quaternion.identity, transform);
            water.transform.localScale = new Vector3(
                terrainWidth * terrainScale / 10f,
                1f,
                terrainLength * terrainScale / 10f
            );
        }

        private void PlaceVegetation()
        {
            if (treePrefabs == null || treePrefabs.Length == 0) return;

            System.Random rng = new System.Random(seed + 1);

            for (int x = 0; x < terrainWidth; x += 4)
            {
                for (int z = 0; z < terrainLength; z += 4)
                {
                    float normalizedX = (float)x / terrainWidth;
                    float normalizedZ = (float)z / terrainLength;
                    float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) / terrainHeight;
                    float steepness = terrainData.GetSteepness(normalizedX, normalizedZ) / 90f;

                    // Trees in valid height range, not too steep
                    if (height > minTreeHeight && height < maxTreeHeight && steepness < 0.3f)
                    {
                        if ((float)rng.NextDouble() < treeSpawnChance)
                        {
                            Vector3 worldPos = new Vector3(
                                x * terrainScale + (float)rng.NextDouble() * 4f,
                                terrainData.GetInterpolatedHeight(normalizedX, normalizedZ),
                                z * terrainScale + (float)rng.NextDouble() * 4f
                            );

                            GameObject treePrefab = treePrefabs[rng.Next(treePrefabs.Length)];
                            if (treePrefab != null)
                            {
                                GameObject tree = Instantiate(treePrefab, worldPos, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), transform);
                                float scale = 0.8f + (float)rng.NextDouble() * 0.4f;
                                tree.transform.localScale = Vector3.one * scale;
                            }
                        }
                    }

                    // Rocks scattered more broadly
                    if (rockPrefabs != null && rockPrefabs.Length > 0 && (float)rng.NextDouble() < rockSpawnChance)
                    {
                        if (height > waterLevel && steepness < 0.6f)
                        {
                            Vector3 worldPos = new Vector3(
                                x * terrainScale,
                                terrainData.GetInterpolatedHeight(normalizedX, normalizedZ),
                                z * terrainScale
                            );

                            GameObject rockPrefab = rockPrefabs[rng.Next(rockPrefabs.Length)];
                            if (rockPrefab != null)
                            {
                                Instantiate(rockPrefab, worldPos,
                                    Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), transform);
                            }
                        }
                    }
                }
            }
        }
    }
}
