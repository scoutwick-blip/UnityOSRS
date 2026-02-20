using UnityEngine;
using System.Collections.Generic;
using RuneRealm.Core;

namespace RuneRealm.World
{
    /// <summary>
    /// Manages world zones/areas. Each zone represents a distinct area
    /// combining OSRS locations with Skyrim hold aesthetics.
    /// Handles zone transitions, music changes, and discovery.
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        public static ZoneManager Instance { get; private set; }

        [SerializeField] private Zone[] zones;
        [SerializeField] private Zone currentZone;

        public Zone CurrentZone => currentZone;
        public event System.Action<Zone, Zone> OnZoneChanged; // old, new

        private HashSet<string> discoveredZones = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Player.PlayerController.Instance == null) return;

            Vector3 playerPos = Player.PlayerController.Instance.transform.position;
            Zone newZone = GetZoneAt(playerPos);

            if (newZone != null && newZone != currentZone)
            {
                Zone oldZone = currentZone;
                currentZone = newZone;

                if (!discoveredZones.Contains(newZone.zoneName))
                {
                    discoveredZones.Add(newZone.zoneName);
                    UI.HUDManager.Instance?.ShowNotification($"Discovered: {newZone.zoneName}");
                }

                OnZoneChanged?.Invoke(oldZone, newZone);
                EventManager.Publish(GameEvents.ZoneEntered, newZone.zoneName);
            }
        }

        private Zone GetZoneAt(Vector3 position)
        {
            if (zones == null) return null;

            foreach (var zone in zones)
            {
                if (zone.bounds.Contains(position))
                    return zone;
            }

            return null;
        }
    }

    [System.Serializable]
    public class Zone
    {
        public string zoneName;
        public string description;
        public Bounds bounds;
        public BiomeType biomeType;
        public AudioClip zoneMusic;
        public int recommendedLevel;
        public bool isSafeZone;
        public Color mapColor;
    }
}
