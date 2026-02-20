using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style compass/minimap showing direction and nearby points of interest.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        [Header("Minimap Camera")]
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private float cameraHeight = 100f;
        [SerializeField] private float cameraSize = 50f;
        [SerializeField] private bool rotateWithPlayer = true;

        [Header("UI")]
        [SerializeField] private RawImage minimapDisplay;
        [SerializeField] private Image playerMarker;
        [SerializeField] private TextMeshProUGUI zoneNameText;

        [Header("Markers")]
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private Transform markerContainer;
        [SerializeField] private Sprite resourceMarkerSprite;
        [SerializeField] private Sprite npcMarkerSprite;
        [SerializeField] private Sprite questMarkerSprite;

        private RenderTexture minimapRT;
        private Transform playerTransform;

        private void Start()
        {
            SetupMinimapCamera();

            if (Player.PlayerController.Instance != null)
                playerTransform = Player.PlayerController.Instance.transform;
        }

        private void SetupMinimapCamera()
        {
            if (minimapCamera == null)
            {
                GameObject camGO = new GameObject("Minimap Camera");
                minimapCamera = camGO.AddComponent<Camera>();
                minimapCamera.orthographic = true;
                minimapCamera.orthographicSize = cameraSize;
                minimapCamera.clearFlags = CameraClearFlags.SolidColor;
                minimapCamera.backgroundColor = new Color(0.15f, 0.15f, 0.12f, 1f);
                minimapCamera.cullingMask = LayerMask.GetMask("Default", "Terrain", "Water");
                camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            minimapRT = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = minimapRT;

            if (minimapDisplay != null)
                minimapDisplay.texture = minimapRT;
        }

        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                if (Player.PlayerController.Instance != null)
                    playerTransform = Player.PlayerController.Instance.transform;
                return;
            }

            // Follow player
            Vector3 newPos = playerTransform.position;
            newPos.y = playerTransform.position.y + cameraHeight;
            minimapCamera.transform.position = newPos;

            // Rotate with player direction
            if (rotateWithPlayer)
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, playerTransform.eulerAngles.y, 0f);
            }

            // Update zone name
            if (zoneNameText != null && World.ZoneManager.Instance?.CurrentZone != null)
            {
                zoneNameText.text = World.ZoneManager.Instance.CurrentZone.zoneName;
            }
        }

        private void OnDestroy()
        {
            if (minimapRT != null)
                minimapRT.Release();
        }
    }
}
