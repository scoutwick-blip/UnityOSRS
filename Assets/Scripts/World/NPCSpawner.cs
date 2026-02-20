using UnityEngine;
using System.Collections.Generic;
using RuneRealm.NPCs;

namespace RuneRealm.World
{
    /// <summary>
    /// Spawns NPCs throughout the world at predefined locations.
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        [SerializeField] private NPCSpawnPoint[] spawnPoints;
        [SerializeField] private GameObject defaultNPCPrefab;

        private List<GameObject> spawnedNPCs = new List<GameObject>();

        [System.Serializable]
        public class NPCSpawnPoint
        {
            public string npcName;
            public NPCType type;
            public Vector3 position;
            public float rotation;
            public NPCBehavior behavior;
            public float wanderRadius = 10f;
            public GameObject customPrefab;
            public DialogueData dialogue;
        }

        public void SpawnAllNPCs()
        {
            if (spawnPoints == null) return;

            foreach (var point in spawnPoints)
            {
                SpawnNPC(point);
            }

            Debug.Log($"[NPCSpawner] Spawned {spawnedNPCs.Count} NPCs.");
        }

        private void SpawnNPC(NPCSpawnPoint point)
        {
            GameObject prefab = point.customPrefab != null ? point.customPrefab : defaultNPCPrefab;
            if (prefab == null) return;

            GameObject npcGO = Instantiate(prefab, point.position,
                Quaternion.Euler(0, point.rotation, 0), transform);
            npcGO.name = $"NPC_{point.npcName}";

            var controller = npcGO.GetComponent<NPCController>();
            if (controller == null)
                controller = npcGO.AddComponent<NPCController>();

            spawnedNPCs.Add(npcGO);
        }
    }
}
