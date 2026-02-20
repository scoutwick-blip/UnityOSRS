using UnityEngine;
using UnityEditor;
using RuneRealm.World;

namespace RuneRealm.Editor
{
    /// <summary>
    /// Custom editor tools for RuneRealm development.
    /// </summary>
    public class RuneRealmEditorTools : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("RuneRealm/Generate Terrain")]
        static void GenerateTerrain()
        {
            var generator = FindAnyObjectByType<TerrainGenerator>();
            if (generator != null)
            {
                generator.GenerateTerrain();
                Debug.Log("Terrain generated successfully!");
            }
            else
            {
                Debug.LogWarning("No TerrainGenerator found in scene.");
            }
        }

        [MenuItem("RuneRealm/Create Default Item Database")]
        static void CreateItemDatabase()
        {
            string path = "Assets/Resources/Items";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
            }

            CreateItem(path, "Normal Logs", 0, Inventory.ItemCategory.Log, 1, 4, 25);
            CreateItem(path, "Oak Logs", 1, Inventory.ItemCategory.Log, 15, 20, 37);
            CreateItem(path, "Willow Logs", 2, Inventory.ItemCategory.Log, 30, 4, 67);
            CreateItem(path, "Maple Logs", 3, Inventory.ItemCategory.Log, 45, 40, 100);
            CreateItem(path, "Yew Logs", 4, Inventory.ItemCategory.Log, 60, 160, 175);
            CreateItem(path, "Magic Logs", 5, Inventory.ItemCategory.Log, 75, 1018, 250);

            CreateItem(path, "Copper Ore", 10, Inventory.ItemCategory.Ore, 1, 4, 17);
            CreateItem(path, "Tin Ore", 11, Inventory.ItemCategory.Ore, 1, 4, 17);
            CreateItem(path, "Iron Ore", 12, Inventory.ItemCategory.Ore, 15, 28, 35);
            CreateItem(path, "Coal", 13, Inventory.ItemCategory.Ore, 30, 114, 50);
            CreateItem(path, "Mithril Ore", 14, Inventory.ItemCategory.Ore, 55, 162, 80);
            CreateItem(path, "Adamantite Ore", 15, Inventory.ItemCategory.Ore, 70, 400, 95);
            CreateItem(path, "Runite Ore", 16, Inventory.ItemCategory.Ore, 85, 4536, 125);

            CreateItem(path, "Raw Shrimps", 20, Inventory.ItemCategory.Fish, 1, 1, 10);
            CreateItem(path, "Raw Trout", 21, Inventory.ItemCategory.Fish, 20, 20, 50);
            CreateItem(path, "Raw Lobster", 22, Inventory.ItemCategory.Fish, 40, 128, 90);
            CreateItem(path, "Raw Swordfish", 23, Inventory.ItemCategory.Fish, 50, 200, 100);
            CreateItem(path, "Raw Shark", 24, Inventory.ItemCategory.Fish, 76, 546, 110);

            CreateItem(path, "Bronze Bar", 30, Inventory.ItemCategory.Bar, 1, 4, 6);
            CreateItem(path, "Iron Bar", 31, Inventory.ItemCategory.Bar, 15, 56, 12);
            CreateItem(path, "Steel Bar", 32, Inventory.ItemCategory.Bar, 30, 150, 17);
            CreateItem(path, "Mithril Bar", 33, Inventory.ItemCategory.Bar, 50, 300, 25);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Item database created!");
        }

        static void CreateItem(string path, string name, int id, Inventory.ItemCategory category,
            int level, int value, int xp)
        {
            var item = ScriptableObject.CreateInstance<Inventory.ItemData>();
            item.itemName = name;
            item.itemId = id;
            item.category = category;
            item.generalStoreValue = value;
            item.description = $"A {name.ToLower()}.";

            string assetPath = $"{path}/{name.Replace(" ", "_")}.asset";
            AssetDatabase.CreateAsset(item, assetPath);
        }

        [MenuItem("RuneRealm/Setup Game Scene")]
        static void SetupGameScene()
        {
            // Create Game Manager
            if (FindAnyObjectByType<Core.GameManager>() == null)
            {
                var gmGO = new GameObject("GameManager");
                gmGO.AddComponent<Core.GameManager>();
                gmGO.AddComponent<Skills.SkillManager>();
                gmGO.AddComponent<Inventory.InventoryManager>();
                gmGO.AddComponent<Audio.AudioManager>();
            }

            // Create World
            if (FindAnyObjectByType<TerrainGenerator>() == null)
            {
                var worldGO = new GameObject("World");
                worldGO.AddComponent<TerrainGenerator>();
                worldGO.AddComponent<BiomeSystem>();
                worldGO.AddComponent<WeatherSystem>();
                worldGO.AddComponent<ZoneManager>();
                worldGO.AddComponent<ResourceSpawner>();
                worldGO.AddComponent<DayNightAmbience>();
            }

            // Create Game Bootstrapper
            if (FindAnyObjectByType<Core.GameBootstrapper>() == null)
            {
                var bootGO = new GameObject("GameBootstrapper");
                bootGO.AddComponent<Core.GameBootstrapper>();
            }

            // Create Directional Light (Sun)
            var existingLight = FindAnyObjectByType<Light>();
            if (existingLight == null)
            {
                var lightGO = new GameObject("Directional Light (Sun)");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.95f, 0.85f);
                light.shadows = LightShadows.Soft;
                lightGO.transform.rotation = Quaternion.Euler(50f, 170f, 0f);
            }

            Debug.Log("Game scene setup complete!");
        }
#endif
    }
}
