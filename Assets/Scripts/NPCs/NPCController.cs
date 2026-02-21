using UnityEngine;
using UnityEngine.AI;
using RuneRealm.Core;
using RuneRealm.Player;

namespace RuneRealm.NPCs
{
    /// <summary>
    /// Base NPC controller. Handles wandering AI, player interaction,
    /// and dialogue triggering. Combines OSRS NPC feel with Skyrim AI.
    /// </summary>
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("NPC Info")]
        [SerializeField] private string npcName = "Villager";
        [SerializeField] private string title;
        [SerializeField] private NPCType npcType = NPCType.Villager;
        [SerializeField] private int combatLevel = 1;

        [Header("Behavior")]
        [SerializeField] private NPCBehavior behavior = NPCBehavior.Wander;
        [SerializeField] private float wanderRadius = 15f;
        [SerializeField] private float wanderInterval = 5f;
        [SerializeField] private float idleTime = 3f;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Transform homePoint;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private DialogueData dialogue;

        [Header("Visual")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform nameTagAnchor;

        private NavMeshAgent agent;
        private float wanderTimer;
        private int currentPatrolIndex;
        private bool isInteracting;
        private NPCState currentState = NPCState.Idle;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimTalk = Animator.StringToHash("Talk");

        public string NPCName => npcName;
        public string Title => title;
        public NPCType Type => npcType;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.stoppingDistance = 0.5f;
        }

        private void Update()
        {
            if (isInteracting) return;
            if (agent == null || !agent.isOnNavMesh) return;

            switch (behavior)
            {
                case NPCBehavior.Wander:
                    UpdateWander();
                    break;
                case NPCBehavior.Patrol:
                    UpdatePatrol();
                    break;
                case NPCBehavior.Stationary:
                    // Just stand there
                    break;
                case NPCBehavior.Follow:
                    UpdateFollow();
                    break;
            }

            UpdateAnimation();
        }

        private void UpdateWander()
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // Pick random point to wander to
                Vector3 randomPoint = homePoint != null ? homePoint.position : transform.position;
                randomPoint += Random.insideUnitSphere * wanderRadius;
                randomPoint.y = transform.position.y;

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(randomPoint, out navHit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                    currentState = NPCState.Walking;
                }

                wanderTimer = wanderInterval + Random.Range(-1f, 1f);
            }

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentState = NPCState.Idle;
            }
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                currentState = NPCState.Walking;
            }
        }

        private void UpdateFollow()
        {
            if (PlayerController.Instance == null) return;

            float dist = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
            if (dist > 5f)
            {
                agent.SetDestination(PlayerController.Instance.transform.position);
                currentState = NPCState.Walking;
            }
            else
            {
                agent.ResetPath();
                currentState = NPCState.Idle;
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;
            animator.SetFloat(AnimSpeed, agent.velocity.magnitude);
        }

        // IInteractable implementation
        public string GetInteractionPrompt()
        {
            string prompt = npcName;
            if (!string.IsNullOrEmpty(title))
                prompt += $" ({title})";

            return $"Talk to {prompt}";
        }

        public void Interact(GameObject interactor)
        {
            if (isInteracting) return;

            isInteracting = true;
            currentState = NPCState.Talking;

            // Face the player
            Vector3 lookDir = (interactor.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            agent.ResetPath();

            if (animator != null)
                animator.SetTrigger(AnimTalk);

            // Open dialogue
            if (dialogue != null)
                DialogueManager.Instance?.StartDialogue(this, dialogue);
        }

        public bool CanInteract(GameObject interactor)
        {
            return !isInteracting;
        }

        public void EndInteraction()
        {
            isInteracting = false;
            currentState = NPCState.Idle;
        }
    }

    public enum NPCType
    {
        Villager,
        Shopkeeper,
        QuestGiver,
        Banker,
        SkillMaster,
        Guard,
        Wanderer
    }

    public enum NPCBehavior
    {
        Wander,
        Patrol,
        Stationary,
        Follow
    }

    public enum NPCState
    {
        Idle,
        Walking,
        Talking,
        Working
    }
}
