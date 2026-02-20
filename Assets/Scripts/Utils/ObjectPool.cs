using System.Collections.Generic;
using UnityEngine;

namespace RuneRealm.Utils
{
    /// <summary>
    /// Generic object pool for efficient instantiation of frequently used objects
    /// like particles, resource nodes, etc.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class PoolConfig
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 10;
            public bool expandable = true;
        }

        [SerializeField] private PoolConfig[] pools;

        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, PoolConfig> configDictionary = new Dictionary<string, PoolConfig>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            if (pools == null) return;

            foreach (var config in pools)
            {
                var queue = new Queue<GameObject>();

                for (int i = 0; i < config.initialSize; i++)
                {
                    GameObject obj = Instantiate(config.prefab, transform);
                    obj.SetActive(false);
                    queue.Enqueue(obj);
                }

                poolDictionary[config.tag] = queue;
                configDictionary[config.tag] = config;
            }
        }

        public GameObject Get(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{tag}' does not exist.");
                return null;
            }

            var queue = poolDictionary[tag];

            if (queue.Count == 0)
            {
                if (configDictionary[tag].expandable)
                {
                    GameObject newObj = Instantiate(configDictionary[tag].prefab, transform);
                    newObj.SetActive(false);
                    queue.Enqueue(newObj);
                }
                else
                {
                    return null;
                }
            }

            GameObject obj = queue.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }

        public void Return(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag)) return;

            obj.SetActive(false);
            poolDictionary[tag].Enqueue(obj);
        }
    }
}
