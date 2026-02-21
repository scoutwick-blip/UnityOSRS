using UnityEngine;
using UnityEngine.InputSystem;
using RuneRealm.Skills;
using RuneRealm.Core;

namespace RuneRealm.Player
{
    /// <summary>
    /// Handles player interaction with world objects, NPCs, and resource nodes.
    /// Shows contextual interaction prompts Skyrim-style.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float interactionRange = 4f;
        [SerializeField] private LayerMask interactableLayers;
        [SerializeField] private Transform eyePoint;

        private IInteractable currentTarget;
        private ResourceNode currentResourceNode;

        public IInteractable CurrentTarget => currentTarget;
        public string InteractionPrompt => currentTarget?.GetInteractionPrompt() ?? "";
        public bool HasTarget => currentTarget != null;

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            ScanForInteractables();

            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && currentTarget != null)
            {
                currentTarget.Interact(gameObject);
            }
        }

        private void ScanForInteractables()
        {
            currentTarget = null;
            currentResourceNode = null;

            Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up;

            // Use all layers if interactableLayers not configured (0 means "Nothing")
            Collider[] colliders = interactableLayers != 0
                ? Physics.OverlapSphere(transform.position, interactionRange, interactableLayers)
                : Physics.OverlapSphere(transform.position, interactionRange);
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    var interactable = col.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        nearestDist = dist;
                        currentTarget = interactable;
                    }

                    var node = col.GetComponent<ResourceNode>();
                    if (node != null && !node.IsDepleted)
                    {
                        currentResourceNode = node;
                    }
                }
            }
        }
    }

    public interface IInteractable
    {
        string GetInteractionPrompt();
        void Interact(GameObject interactor);
        bool CanInteract(GameObject interactor);
    }
}
