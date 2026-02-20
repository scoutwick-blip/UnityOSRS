using System.Collections;
using UnityEngine;
using RuneRealm.Core;
using RuneRealm.Inventory;

namespace RuneRealm.Skills
{
    /// <summary>
    /// A harvestable resource in the world (tree, rock, fishing spot, etc.).
    /// Handles depletion, respawning, and visual state changes.
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Configuration")]
        [SerializeField] private string resourceName = "Resource";
        [SerializeField] private SkillType requiredSkill;
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private ItemData harvestedItem;
        [SerializeField] private int harvestsBeforeDepletion = 1;
        [SerializeField] private float respawnTimeSeconds = 30f;

        [Header("Visual")]
        [SerializeField] private GameObject activeModel;
        [SerializeField] private GameObject depletedModel;
        [SerializeField] private ParticleSystem harvestParticles;
        [SerializeField] private float interactionRadius = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip harvestSound;
        [SerializeField] private AudioClip depletedSound;
        [SerializeField] private AudioClip respawnSound;

        private int remainingHarvests;
        private bool isDepleted;
        private AudioSource audioSource;

        public string ResourceName => resourceName;
        public SkillType RequiredSkill => requiredSkill;
        public int RequiredLevel => requiredLevel;
        public ResourceType Type => resourceType;
        public bool IsDepleted => isDepleted;
        public float InteractionRadius => interactionRadius;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            ResetNode();
        }

        public ItemData GetHarvestedItem()
        {
            return harvestedItem;
        }

        public void Harvest()
        {
            if (isDepleted) return;

            remainingHarvests--;

            // Play effects
            if (harvestParticles != null)
                harvestParticles.Play();

            if (harvestSound != null && audioSource != null)
                audioSource.PlayOneShot(harvestSound);

            if (remainingHarvests <= 0)
            {
                Deplete();
            }
        }

        private void Deplete()
        {
            isDepleted = true;

            if (activeModel != null) activeModel.SetActive(false);
            if (depletedModel != null) depletedModel.SetActive(true);

            if (depletedSound != null && audioSource != null)
                audioSource.PlayOneShot(depletedSound);

            EventManager.Publish(GameEvents.ResourceDepleted, this);

            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(respawnTimeSeconds);
            Respawn();
        }

        private void Respawn()
        {
            ResetNode();

            if (respawnSound != null && audioSource != null)
                audioSource.PlayOneShot(respawnSound);

            EventManager.Publish(GameEvents.ResourceRespawned, this);
        }

        private void ResetNode()
        {
            isDepleted = false;
            remainingHarvests = harvestsBeforeDepletion;

            if (activeModel != null) activeModel.SetActive(true);
            if (depletedModel != null) depletedModel.SetActive(false);
        }

        public string GetInteractionText()
        {
            if (isDepleted) return "";

            int playerLevel = SkillManager.Instance != null
                ? SkillManager.Instance.GetLevel(requiredSkill)
                : 1;

            if (playerLevel < requiredLevel)
                return $"Requires {SkillConstants.GetSkillDisplayName(requiredSkill)} level {requiredLevel}";

            string verb = resourceType switch
            {
                ResourceType.Tree => "Chop",
                ResourceType.Rock => "Mine",
                ResourceType.FishingSpot => "Fish",
                ResourceType.Herb => "Pick",
                ResourceType.Crop => "Harvest",
                ResourceType.Furnace => "Smelt",
                ResourceType.Anvil => "Smith",
                ResourceType.CookingRange => "Cook",
                ResourceType.SpinningWheel => "Spin",
                ResourceType.Altar => "Pray",
                _ => "Interact"
            };

            return $"{verb} {resourceName}";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }

    public enum ResourceType
    {
        Tree,
        Rock,
        FishingSpot,
        Herb,
        Crop,
        Furnace,
        Anvil,
        CookingRange,
        SpinningWheel,
        Altar,
        RuneEssence,
        HunterTrap,
        AgilityObstacle
    }
}
