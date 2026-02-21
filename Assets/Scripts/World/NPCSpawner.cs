using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using RuneRealm.NPCs;

namespace RuneRealm.World
{
    /// <summary>
    /// Spawns NPCs throughout the world. Creates procedural NPCs when no prefabs exist.
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
            public string title;
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
            if (spawnPoints == null || spawnPoints.Length == 0)
                InitializeDefaultNPCs();

            if (spawnPoints == null) return;

            foreach (var point in spawnPoints)
            {
                SpawnNPC(point);
            }

            Debug.Log($"[NPCSpawner] Spawned {spawnedNPCs.Count} NPCs.");
        }

        /// <summary>
        /// Offset all spawn positions relative to a base position (e.g. player spawn).
        /// Call this BEFORE SpawnAllNPCs.
        /// </summary>
        public void SetBasePosition(Vector3 basePos, float terrainY)
        {
            if (spawnPoints == null) InitializeDefaultNPCs();
            if (spawnPoints == null) return;

            foreach (var p in spawnPoints)
            {
                p.position = new Vector3(
                    basePos.x + p.position.x,
                    terrainY + 1f,
                    basePos.z + p.position.z
                );
            }
        }

        private void InitializeDefaultNPCs()
        {
            spawnPoints = new NPCSpawnPoint[]
            {
                new NPCSpawnPoint
                {
                    npcName = "Guide",
                    title = "Lumbridge Guide",
                    type = NPCType.QuestGiver,
                    position = new Vector3(8f, 0f, 5f),
                    behavior = NPCBehavior.Stationary,
                    wanderRadius = 3f,
                    dialogue = CreateDialogue("Guide", new string[]
                    {
                        "Welcome to RuneRealm, adventurer! I am the Lumbridge Guide.",
                        "You can chop trees, mine rocks, and catch fish to train your skills. Press E near a resource to begin.",
                        "Use WASD to move, hold Shift to sprint, and press Space to jump. Press ESC for the pause menu.",
                        "Good luck on your journey!",
                    })
                },
                new NPCSpawnPoint
                {
                    npcName = "Hans",
                    title = "Wanderer",
                    type = NPCType.Wanderer,
                    position = new Vector3(-10f, 0f, 12f),
                    behavior = NPCBehavior.Wander,
                    wanderRadius = 15f,
                    dialogue = CreateDialogue("Hans", new string[]
                    {
                        "Hello there! I'm Hans. I've been walking around here for as long as I can remember.",
                        "They say this land was once home to great kingdoms. Now it's mostly trees and rocks.",
                        "Have you tried mining those copper and tin rocks? Smithing is a fine trade!",
                    })
                },
                new NPCSpawnPoint
                {
                    npcName = "Doric",
                    title = "Dwarf Miner",
                    type = NPCType.SkillMaster,
                    position = new Vector3(20f, 0f, -8f),
                    behavior = NPCBehavior.Stationary,
                    wanderRadius = 0f,
                    dialogue = CreateDialogue("Doric", new string[]
                    {
                        "Aye, I'm Doric. I know my ores!",
                        "Copper and tin are easy to find in the lowlands. Smelt them together to make bronze.",
                        "If you want iron, head for the highlands. But you'll need level 15 Mining.",
                        "Keep at it, and one day you'll be mining runite!",
                    })
                },
                new NPCSpawnPoint
                {
                    npcName = "Willow",
                    title = "Herbalist",
                    type = NPCType.Villager,
                    position = new Vector3(-5f, 0f, -15f),
                    behavior = NPCBehavior.Wander,
                    wanderRadius = 8f,
                    dialogue = CreateDialogue("Willow", new string[]
                    {
                        "Oh, hello dear! I'm Willow, the village herbalist.",
                        "The forests around here are teeming with life. Normal trees are everywhere, but oak trees require more skill.",
                        "I hear there are willow trees near the water. They make lovely fishing rods too!",
                    })
                },
                new NPCSpawnPoint
                {
                    npcName = "Bob",
                    title = "Fisherman",
                    type = NPCType.SkillMaster,
                    position = new Vector3(15f, 0f, 20f),
                    behavior = NPCBehavior.Stationary,
                    wanderRadius = 0f,
                    dialogue = CreateDialogue("Bob", new string[]
                    {
                        "Ahoy! Name's Bob. Best fisherman this side of Lumbridge!",
                        "If you want to fish, find the blue spots near the water's edge. Press E to start.",
                        "Shrimp is great for beginners. Cook them on a fire for some food!",
                        "Watch your inventory though - it only holds 28 items.",
                    })
                },
            };
        }

        private void SpawnNPC(NPCSpawnPoint point)
        {
            GameObject npcGO;
            if (point.customPrefab != null)
            {
                npcGO = Instantiate(point.customPrefab, point.position,
                    Quaternion.Euler(0, point.rotation, 0), transform);
            }
            else if (defaultNPCPrefab != null)
            {
                npcGO = Instantiate(defaultNPCPrefab, point.position,
                    Quaternion.Euler(0, point.rotation, 0), transform);
            }
            else
            {
                npcGO = CreateProceduralNPC(point);
            }

            npcGO.name = $"NPC_{point.npcName}";

            // Configure NPC controller via reflection (fields are serialized private)
            var controller = npcGO.GetComponent<NPCController>();
            if (controller == null) controller = npcGO.AddComponent<NPCController>();

            var t = typeof(NPCController);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("npcName", flags)?.SetValue(controller, point.npcName);
            t.GetField("title", flags)?.SetValue(controller, point.title ?? "");
            t.GetField("npcType", flags)?.SetValue(controller, point.type);
            t.GetField("behavior", flags)?.SetValue(controller, point.behavior);
            t.GetField("wanderRadius", flags)?.SetValue(controller, point.wanderRadius);
            t.GetField("dialogue", flags)?.SetValue(controller, point.dialogue);

            spawnedNPCs.Add(npcGO);
        }

        private GameObject CreateProceduralNPC(NPCSpawnPoint point)
        {
            var root = new GameObject(point.npcName);
            root.transform.position = point.position;
            root.transform.rotation = Quaternion.Euler(0, point.rotation, 0);
            root.transform.SetParent(transform);

            // Body capsule
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            Object.Destroy(body.GetComponent<Collider>());

            Color bodyColor = point.type switch
            {
                NPCType.QuestGiver => new Color(0.8f, 0.7f, 0.2f),
                NPCType.SkillMaster => new Color(0.2f, 0.6f, 0.3f),
                NPCType.Guard => new Color(0.5f, 0.5f, 0.55f),
                NPCType.Shopkeeper => new Color(0.6f, 0.3f, 0.6f),
                _ => new Color(0.5f, 0.35f, 0.25f),
            };
            var bodyRend = body.GetComponent<Renderer>();
            if (bodyRend != null) bodyRend.material.color = bodyColor;

            // Head sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 2.0f, 0);
            head.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            Object.Destroy(head.GetComponent<Collider>());
            var headRend = head.GetComponent<Renderer>();
            if (headRend != null) headRend.material.color = new Color(0.85f, 0.7f, 0.55f);

            // Collider for interaction detection
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.3f;
            col.center = new Vector3(0, 0.9f, 0);

            // NavMeshAgent for AI movement — only add if NavMesh exists
            if (NavMesh.SamplePosition(root.transform.position, out _, 10f, NavMesh.AllAreas))
            {
                var agent = root.AddComponent<NavMeshAgent>();
                agent.speed = 2f;
                agent.angularSpeed = 180f;
                agent.stoppingDistance = 0.5f;
                agent.radius = 0.3f;
                agent.height = 1.8f;
            }

            return root;
        }

        private DialogueData CreateDialogue(string npcName, string[] lines)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.dialogueName = $"{npcName}_dialogue";

            var nodes = new DialogueNode[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                nodes[i] = new DialogueNode
                {
                    text = lines[i],
                    nextNodeIndex = (i < lines.Length - 1) ? i + 1 : -1,
                    choices = null
                };
            }
            data.nodes = nodes;
            return data;
        }
    }
}
