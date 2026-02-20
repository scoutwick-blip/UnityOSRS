using UnityEngine;
using System.Collections.Generic;
using RuneRealm.Skills;

namespace RuneRealm.World
{
    /// <summary>
    /// Spawns resource nodes throughout the world based on biome configuration.
    /// Places trees, rocks, fishing spots, and other harvestable resources.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 200f;
        [SerializeField] private int maxResourcesPerType = 50;
        [SerializeField] private float minSpacing = 5f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Resource Prefabs")]
        [SerializeField] private ResourcePrefabSet[] resourceSets;

        private List<GameObject> spawnedResources = new List<GameObject>();

        [System.Serializable]
        public class ResourcePrefabSet
        {
            public string resourceName;
            public SkillType skill;
            public int requiredLevel;
            public GameObject[] prefabVariants;
            public BiomeType[] validBiomes;
            public float spawnWeight = 1f;
            public float minHeight = 0.1f;
            public float maxHeight = 0.8f;
            public float maxSteepness = 0.3f;
        }

        public void SpawnResources(TerrainData terrainData, Vector3 terrainPosition)
        {
            if (resourceSets == null || resourceSets.Length == 0) return;

            System.Random rng = new System.Random(42);

            foreach (var set in resourceSets)
            {
                int spawnCount = 0;

                for (int attempt = 0; attempt < maxResourcesPerType * 10 && spawnCount < maxResourcesPerType; attempt++)
                {
                    // Random position on terrain
                    float nx = (float)rng.NextDouble();
                    float nz = (float)rng.NextDouble();

                    float height = terrainData.GetInterpolatedHeight(nx, nz) / terrainData.size.y;
                    float steepness = terrainData.GetSteepness(nx, nz) / 90f;

                    if (height < set.minHeight || height > set.maxHeight) continue;
                    if (steepness > set.maxSteepness) continue;

                    Vector3 worldPos = new Vector3(
                        nx * terrainData.size.x + terrainPosition.x,
                        terrainData.GetInterpolatedHeight(nx, nz) + terrainPosition.y,
                        nz * terrainData.size.z + terrainPosition.z
                    );

                    // Check spacing with existing resources
                    if (!CheckSpacing(worldPos)) continue;

                    // Spawn
                    if (set.prefabVariants != null && set.prefabVariants.Length > 0)
                    {
                        GameObject prefab = set.prefabVariants[rng.Next(set.prefabVariants.Length)];
                        if (prefab != null)
                        {
                            GameObject obj = Instantiate(prefab, worldPos,
                                Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), transform);

                            // Add resource node if not present
                            var node = obj.GetComponent<ResourceNode>();
                            if (node == null)
                            {
                                node = obj.AddComponent<ResourceNode>();
                            }

                            spawnedResources.Add(obj);
                            spawnCount++;
                        }
                    }
                }
            }

            Debug.Log($"[ResourceSpawner] Spawned {spawnedResources.Count} resource nodes.");
        }

        private bool CheckSpacing(Vector3 position)
        {
            foreach (var obj in spawnedResources)
            {
                if (obj != null && Vector3.Distance(obj.transform.position, position) < minSpacing)
                    return false;
            }
            return true;
        }
    }
}
