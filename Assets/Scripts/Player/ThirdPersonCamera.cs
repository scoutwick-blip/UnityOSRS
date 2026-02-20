using UnityEngine;
using UnityEngine.InputSystem;
using RuneRealm.Core;

namespace RuneRealm.Player
{
    /// <summary>
    /// Skyrim-style third-person orbital camera with smooth follow,
    /// collision detection, and zoom control.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Orbit")]
        [SerializeField] private float mouseSensitivity = 3f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float orbitSmoothSpeed = 10f;

        [Header("Distance")]
        [SerializeField] private float defaultDistance = 5f;
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private float zoomSpeed = 3f;
        [SerializeField] private float zoomSmoothSpeed = 8f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float collisionSmoothSpeed = 15f;

        [Header("Skyrim Feel")]
        [SerializeField] private float headBobAmount = 0.02f;
        [SerializeField] private float headBobFrequency = 2f;
        [SerializeField] private bool enableHeadBob = true;

        private float yaw;
        private float pitch;
        private float currentDistance;
        private float desiredDistance;
        private float bobTimer;
        private bool hasSnappedToTarget;

        private void Start()
        {
            if (target == null)
            {
                var player = PlayerController.Instance;
                if (player != null)
                    target = player.transform;
            }

            desiredDistance = defaultDistance;
            currentDistance = defaultDistance;

            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = PlayerController.Instance;
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    return;
                }
            }
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            HandleInput();
            HandleZoom();
            UpdatePosition();
        }

        private void HandleInput()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            float mouseX = delta.x * 0.1f * mouseSensitivity;
            float mouseY = delta.y * 0.1f * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scrollInput = mouse.scroll.y.ReadValue() / 1200f;
            desiredDistance -= scrollInput * zoomSpeed;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        }

        private void UpdatePosition()
        {
            Vector3 targetPos = target.position + targetOffset;

            // Apply head bob when walking
            if (enableHeadBob && PlayerController.Instance != null &&
                !PlayerController.Instance.IsSkilling)
            {
                CharacterController cc = target.GetComponent<CharacterController>();
                if (cc != null && cc.velocity.magnitude > 0.5f)
                {
                    bobTimer += Time.deltaTime * headBobFrequency;
                    float bobOffset = Mathf.Sin(bobTimer) * headBobAmount;
                    targetPos.y += bobOffset;
                }
                else
                {
                    bobTimer = 0f;
                }
            }

            // Calculate desired position
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            Vector3 desiredPos = targetPos + direction * desiredDistance;

            // Collision detection
            float adjustedDistance = desiredDistance;
            RaycastHit hit;
            if (Physics.SphereCast(targetPos, collisionRadius, direction, out hit,
                desiredDistance, collisionLayers))
            {
                adjustedDistance = hit.distance - collisionRadius;
                adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
            }

            currentDistance = Mathf.Lerp(currentDistance, adjustedDistance, collisionSmoothSpeed * Time.deltaTime);

            Vector3 finalPosition = targetPos + direction * currentDistance;

            if (!hasSnappedToTarget)
            {
                transform.position = finalPosition;
                hasSnappedToTarget = true;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, finalPosition, orbitSmoothSpeed * Time.deltaTime);
            }
            transform.LookAt(targetPos);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public float GetYaw()
        {
            return yaw;
        }
    }
}
