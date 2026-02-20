using System.IO;
using UnityEngine;

namespace RuneRealm.Core
{
    /// <summary>
    /// Handles saving and loading game data to/from JSON files.
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SaveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
        private const string SaveFileName = "runerealm_save.json";

        [System.Serializable]
        public class SaveData
        {
            public Skills.SkillSaveData[] skills;
            public Inventory.InventorySaveData inventory;
            public PlayerSaveData player;
            public WorldSaveData world;
            public QuestSaveData[] quests;
            public string saveTimestamp;
        }

        [System.Serializable]
        public class PlayerSaveData
        {
            public float posX, posY, posZ;
            public float rotY;
            public float stamina;
        }

        [System.Serializable]
        public class WorldSaveData
        {
            public float timeOfDay;
            public string currentZone;
        }

        [System.Serializable]
        public class QuestSaveData
        {
            public string questId;
            public int stage;
            public bool completed;
        }

        public static void Save(SaveData data)
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            data.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(SaveDirectory, SaveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveSystem] Game saved to {path}");
        }

        public static SaveData Load()
        {
            string path = Path.Combine(SaveDirectory, SaveFileName);
            if (!File.Exists(path))
            {
                Debug.Log("[SaveSystem] No save file found, starting fresh.");
                return null;
            }

            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveSystem] Game loaded from {path}");
            return data;
        }

        public static bool SaveExists()
        {
            return File.Exists(Path.Combine(SaveDirectory, SaveFileName));
        }

        public static void DeleteSave()
        {
            string path = Path.Combine(SaveDirectory, SaveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
        }
    }
}
