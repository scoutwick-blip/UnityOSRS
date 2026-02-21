using UnityEngine;
using UnityEngine.InputSystem;
using RuneRealm.Core;
using RuneRealm.Skills;

namespace RuneRealm.Player
{
    /// <summary>
    /// Skyrim-style third-person player controller with WASD movement,
    /// sprint, jump, and interaction with resource nodes.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 15f;
        [SerializeField] private float staminaRegenRate = 8f;
        [SerializeField] private float staminaRegenDelay = 2f;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 4f;
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private Transform cameraTransform;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isSprinting;
        private float currentStamina;
        private float lastSprintTime;
        private bool staminaExhausted;
        private ResourceNode nearestNode;
        private SkillingAction currentSkillingAction;
        private bool isSkilling;

        // Animation hashes
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimJump = Animator.StringToHash("Jump");
        private static readonly int AnimIsSkilling = Animator.StringToHash("IsSkilling");

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercent => currentStamina / maxStamina;
        public bool IsSkilling => isSkilling;
        public ResourceNode NearestNode => nearestNode;
        public bool IsSprinting => isSprinting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            characterController = GetComponent<CharacterController>();
            currentStamina = maxStamina;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.InMenu) return;

            GroundCheck();
            HandleMovement();
            HandleStamina();
            HandleInteraction();
            HandleSkillingInput();
            UpdateAnimator();
        }

        private void GroundCheck()
        {
            if (groundLayer != 0)
            {
                isGrounded = Physics.CheckSphere(
                    transform.position + Vector3.down * (characterController.height / 2f),
                    groundCheckDistance,
                    groundLayer
                );
            }
            else
            {
                // groundLayer not configured — fall back to CharacterController
                isGrounded = characterController.isGrounded;
            }

            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;
        }

        private static float ReadKeyAxis(Key positive, Key negative)
        {
            var kb = Keyboard.current;
            if (kb == null) return 0f;
            float val = 0f;
            if (kb[positive].isPressed) val += 1f;
            if (kb[negative].isPressed) val -= 1f;
            return val;
        }

        private void HandleMovement()
        {
            if (isSkilling) return;

            // Lazy-init camera reference (camera may not exist during Awake)
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            float horizontal = ReadKeyAxis(Key.D, Key.A)
                             + ReadKeyAxis(Key.RightArrow, Key.LeftArrow);
            float vertical   = ReadKeyAxis(Key.W, Key.S)
                             + ReadKeyAxis(Key.UpArrow, Key.DownArrow);
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            vertical   = Mathf.Clamp(vertical, -1f, 1f);

            Vector3 direction = Vector3.zero;

            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                direction = (camForward * vertical + camRight * horizontal).normalized;
            }
            else
            {
                direction = (Vector3.forward * vertical + Vector3.right * horizontal).normalized;
            }

            // Sprint — require 20% stamina to resume after exhaustion
            var kb = Keyboard.current;
            bool shiftHeld = kb != null && kb.leftShiftKey.isPressed;
            if (currentStamina <= 0f) staminaExhausted = true;
            if (staminaExhausted && currentStamina >= maxStamina * 0.2f) staminaExhausted = false;
            isSprinting = shiftHeld && !staminaExhausted && direction.magnitude > 0.1f;
            float speed = isSprinting ? sprintSpeed : walkSpeed;

            // Move
            if (direction.magnitude >= 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                characterController.Move(direction * speed * Time.deltaTime);
            }

            // Jump
            bool jumpPressed = kb != null && kb.spaceKey.wasPressedThisFrame;
            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                if (animator != null) animator.SetTrigger(AnimJump);
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);

            // Safety: prevent falling through terrain
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                if (transform.position.y < terrainY)
                {
                    transform.position = new Vector3(transform.position.x, terrainY + 0.1f, transform.position.z);
                    velocity.y = 0f;
                }
            }
        }

        private void HandleStamina()
        {
            if (isSprinting)
            {
                currentStamina = Mathf.Max(0, currentStamina - staminaDrainRate * Time.deltaTime);
                lastSprintTime = Time.time;
            }
            else if (Time.time - lastSprintTime > staminaRegenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            }

            EventManager.Publish(GameEvents.PlayerStaminaChanged, StaminaPercent);
        }

        private void HandleInteraction()
        {
            // Find nearest resource node
            nearestNode = FindNearestResourceNode();

            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && nearestNode != null && !isSkilling)
            {
                TryStartSkilling(nearestNode);
            }
        }

        private void HandleSkillingInput()
        {
            // Cancel skilling with movement or pressing E again
            if (isSkilling)
            {
                float h = ReadKeyAxis(Key.D, Key.A) + ReadKeyAxis(Key.RightArrow, Key.LeftArrow);
                float v = ReadKeyAxis(Key.W, Key.S) + ReadKeyAxis(Key.UpArrow, Key.DownArrow);

                var kb = Keyboard.current;
                bool ePressed = kb != null && kb.eKey.wasPressedThisFrame;
                if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f || ePressed)
                {
                    StopSkilling();
                }
            }
        }

        private ResourceNode FindNearestResourceNode()
        {
            // Use all layers if interactionLayer not configured (0 means "Nothing")
            Collider[] colliders = interactionLayer != 0
                ? Physics.OverlapSphere(transform.position, interactionRange, interactionLayer)
                : Physics.OverlapSphere(transform.position, interactionRange);
            ResourceNode nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                var node = col.GetComponent<ResourceNode>();
                if (node != null && !node.IsDepleted)
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = node;
                    }
                }
            }

            return nearest;
        }

        private void TryStartSkilling(ResourceNode node)
        {
            // Face the resource
            Vector3 lookDir = (node.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            // Find appropriate skilling action
            SkillingAction action = GetSkillingActionForNode(node);
            if (action != null && action.CanStart(node))
            {
                currentSkillingAction = action;
                action.StartSkilling(node);
                isSkilling = true;

                if (GameManager.Instance != null)
                    GameManager.Instance.SetGameState(GameState.Skilling);
            }
        }

        public void StopSkilling()
        {
            if (currentSkillingAction != null)
            {
                currentSkillingAction.StopSkilling();
                currentSkillingAction = null;
            }
            isSkilling = false;

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Playing);
        }

        private SkillingAction GetSkillingActionForNode(ResourceNode node)
        {
            // Get the appropriate action component based on resource type
            switch (node.RequiredSkill)
            {
                case SkillType.Woodcutting:
                    return GetComponent<WoodcuttingAction>();
                case SkillType.Mining:
                    return GetComponent<MiningAction>();
                case SkillType.Fishing:
                    return GetComponent<FishingAction>();
                case SkillType.Cooking:
                    return GetComponent<CookingAction>();
                case SkillType.Smithing:
                    return GetComponent<SmithingAction>();
                case SkillType.Crafting:
                    return GetComponent<CraftingAction>();
                default:
                    return null;
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            float speed = new Vector2(
                characterController.velocity.x,
                characterController.velocity.z
            ).magnitude;

            animator.SetFloat(AnimSpeed, speed);
            animator.SetBool(AnimIsGrounded, isGrounded);
            animator.SetBool(AnimIsSkilling, isSkilling);
        }

        public SaveSystem.PlayerSaveData GetSaveData()
        {
            return new SaveSystem.PlayerSaveData
            {
                posX = transform.position.x,
                posY = transform.position.y,
                posZ = transform.position.z,
                rotY = transform.eulerAngles.y,
                stamina = currentStamina
            };
        }

        public void LoadSaveData(SaveSystem.PlayerSaveData data)
        {
            if (data == null) return;
            characterController.enabled = false;
            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            transform.eulerAngles = new Vector3(0, data.rotY, 0);
            currentStamina = data.stamina;
            characterController.enabled = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
