using UnityEngine;
using System.Collections.Generic;
using RuneRealm.Skills;
using RuneRealm.Inventory;

namespace RuneRealm.World
{
    /// <summary>
    /// Spawns resource nodes throughout the world based on biome configuration.
    /// Creates procedural resource objects when no prefabs are assigned.
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
        private Dictionary<string, ItemData> itemCache = new Dictionary<string, ItemData>();

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
            if (resourceSets == null || resourceSets.Length == 0)
                InitializeDefaultResources();

            if (resourceSets == null || resourceSets.Length == 0) return;

            System.Random rng = new System.Random(42);

            foreach (var set in resourceSets)
            {
                int spawnCount = 0;
                int maxAttempts = maxResourcesPerType * 10;

                for (int attempt = 0; attempt < maxAttempts && spawnCount < maxResourcesPerType; attempt++)
                {
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

                    if (!CheckSpacing(worldPos)) continue;

                    GameObject obj;
                    if (set.prefabVariants != null && set.prefabVariants.Length > 0)
                    {
                        GameObject prefab = set.prefabVariants[rng.Next(set.prefabVariants.Length)];
                        if (prefab == null) continue;
                        obj = Instantiate(prefab, worldPos,
                            Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), transform);
                    }
                    else
                    {
                        obj = CreateProceduralResource(set, worldPos,
                            Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
                    }

                    if (obj == null) continue;

                    var node = obj.GetComponent<ResourceNode>();
                    if (node == null) node = obj.AddComponent<ResourceNode>();

                    var item = GetOrCreateItem(set.resourceName, set.skill);
                    int harvests = set.skill == SkillType.Woodcutting ? 3 : 1;
                    node.Initialize(set.resourceName, set.skill, set.requiredLevel,
                        GetResourceType(set.skill), item, harvests, 30f);

                    spawnedResources.Add(obj);
                    spawnCount++;
                }
            }

            Debug.Log($"[ResourceSpawner] Spawned {spawnedResources.Count} resource nodes.");
        }

        private void InitializeDefaultResources()
        {
            resourceSets = new ResourcePrefabSet[]
            {
                new ResourcePrefabSet
                {
                    resourceName = "Normal Tree", skill = SkillType.Woodcutting,
                    requiredLevel = 1, spawnWeight = 1f,
                    minHeight = 0.08f, maxHeight = 0.55f, maxSteepness = 0.35f
                },
                new ResourcePrefabSet
                {
                    resourceName = "Oak Tree", skill = SkillType.Woodcutting,
                    requiredLevel = 15, spawnWeight = 0.6f,
                    minHeight = 0.1f, maxHeight = 0.5f, maxSteepness = 0.3f
                },
                new ResourcePrefabSet
                {
                    resourceName = "Willow Tree", skill = SkillType.Woodcutting,
                    requiredLevel = 30, spawnWeight = 0.3f,
                    minHeight = 0.05f, maxHeight = 0.25f, maxSteepness = 0.2f
                },
                new ResourcePrefabSet
                {
                    resourceName = "Copper Rock", skill = SkillType.Mining,
                    requiredLevel = 1, spawnWeight = 0.8f,
                    minHeight = 0.15f, maxHeight = 0.65f, maxSteepness = 0.5f
                },
                new ResourcePrefabSet
                {
                    resourceName = "Tin Rock", skill = SkillType.Mining,
                    requiredLevel = 1, spawnWeight = 0.8f,
                    minHeight = 0.15f, maxHeight = 0.65f, maxSteepness = 0.5f
                },
                new ResourcePrefabSet
                {
                    resourceName = "Iron Rock", skill = SkillType.Mining,
                    requiredLevel = 15, spawnWeight = 0.4f,
                    minHeight = 0.3f, maxHeight = 0.75f, maxSteepness = 0.5f
                },
                new ResourcePrefabSet
                {
                    resourceName = "Shrimp Spot", skill = SkillType.Fishing,
                    requiredLevel = 1, spawnWeight = 0.5f,
                    minHeight = 0.02f, maxHeight = 0.1f, maxSteepness = 0.15f
                },
            };
        }

        private GameObject CreateProceduralResource(ResourcePrefabSet set, Vector3 pos, Quaternion rot)
        {
            var root = new GameObject(set.resourceName);
            root.transform.position = pos;
            root.transform.rotation = rot;
            root.transform.SetParent(transform);

            if (set.skill == SkillType.Woodcutting)
                BuildProceduralTree(root, set.resourceName);
            else if (set.skill == SkillType.Mining)
                BuildProceduralRock(root, set.resourceName);
            else if (set.skill == SkillType.Fishing)
                BuildProceduralFishingSpot(root);

            var col = root.AddComponent<SphereCollider>();
            col.radius = 1.5f;
            col.center = set.skill == SkillType.Woodcutting
                ? new Vector3(0, 2f, 0) : new Vector3(0, 0.5f, 0);

            return root;
        }

        private void BuildProceduralTree(GameObject root, string treeName)
        {
            float trunkHeight = 3f;
            float trunkRadius = 0.3f;
            Color trunkColor = new Color(0.35f, 0.22f, 0.1f);
            float foliageSize = 2.5f;
            Color foliageColor = new Color(0.15f, 0.45f, 0.12f);

            if (treeName.Contains("Oak"))
            {
                trunkHeight = 4f; trunkRadius = 0.5f;
                trunkColor = new Color(0.3f, 0.18f, 0.08f);
                foliageSize = 3.5f; foliageColor = new Color(0.12f, 0.38f, 0.1f);
            }
            else if (treeName.Contains("Willow"))
            {
                trunkHeight = 3.5f; trunkRadius = 0.35f;
                trunkColor = new Color(0.4f, 0.3f, 0.15f);
                foliageSize = 3f; foliageColor = new Color(0.2f, 0.5f, 0.15f);
            }

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform, false);
            Object.Destroy(trunk.GetComponent<Collider>());
            trunk.transform.localPosition = new Vector3(0, trunkHeight / 2f, 0);
            trunk.transform.localScale = new Vector3(trunkRadius * 2f, trunkHeight / 2f, trunkRadius * 2f);
            SetColor(trunk, trunkColor);

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.transform.SetParent(root.transform, false);
            Object.Destroy(foliage.GetComponent<Collider>());
            foliage.transform.localPosition = new Vector3(0, trunkHeight + foliageSize * 0.3f, 0);
            foliage.transform.localScale = Vector3.one * foliageSize;
            SetColor(foliage, foliageColor);
        }

        private void BuildProceduralRock(GameObject root, string rockName)
        {
            float size = 1.2f;
            Color rockColor = new Color(0.5f, 0.45f, 0.4f);

            if (rockName.Contains("Copper"))
                rockColor = new Color(0.6f, 0.35f, 0.15f);
            else if (rockName.Contains("Tin"))
                rockColor = new Color(0.55f, 0.55f, 0.55f);
            else if (rockName.Contains("Iron"))
            { rockColor = new Color(0.35f, 0.2f, 0.15f); size = 1.4f; }

            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.transform.SetParent(root.transform, false);
            Object.Destroy(rock.GetComponent<Collider>());
            rock.transform.localPosition = new Vector3(0, size * 0.4f, 0);
            rock.transform.localScale = new Vector3(size, size * 0.7f, size * 0.9f);
            rock.transform.localRotation = Quaternion.Euler(0, 30f, 5f);
            SetColor(rock, rockColor);
        }

        private void BuildProceduralFishingSpot(GameObject root)
        {
            var spot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spot.transform.SetParent(root.transform, false);
            Object.Destroy(spot.GetComponent<Collider>());
            spot.transform.localPosition = new Vector3(0, 0.2f, 0);
            spot.transform.localScale = new Vector3(1f, 0.3f, 1f);
            SetColor(spot, new Color(0.2f, 0.5f, 0.8f, 0.7f));
        }

        private void SetColor(GameObject obj, Color color)
        {
            var rend = obj.GetComponent<Renderer>();
            if (rend != null) rend.material.color = color;
        }

        private ItemData GetOrCreateItem(string resourceName, SkillType skill)
        {
            if (itemCache.TryGetValue(resourceName, out var cached)) return cached;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = GetItemName(resourceName);
            item.description = $"Gathered from {resourceName}.";
            item.itemId = resourceName.GetHashCode();
            item.isStackable = true;
            item.maxStackSize = 28;
            item.category = skill switch
            {
                SkillType.Woodcutting => ItemCategory.Log,
                SkillType.Mining => ItemCategory.Ore,
                SkillType.Fishing => ItemCategory.Fish,
                _ => ItemCategory.Resource
            };

            itemCache[resourceName] = item;
            return item;
        }

        private string GetItemName(string resourceName)
        {
            if (resourceName.Contains("Tree")) return resourceName.Replace(" Tree", " Logs");
            if (resourceName.Contains("Rock")) return resourceName.Replace(" Rock", " Ore");
            if (resourceName.Contains("Spot")) return resourceName.Replace(" Spot", "");
            return resourceName;
        }

        private ResourceType GetResourceType(SkillType skill)
        {
            return skill switch
            {
                SkillType.Woodcutting => ResourceType.Tree,
                SkillType.Mining => ResourceType.Rock,
                SkillType.Fishing => ResourceType.FishingSpot,
                _ => ResourceType.Tree
            };
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
