using System.Collections;
using UnityEngine;
using RuneRealm.Core;
using RuneRealm.Inventory;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Base class for all skilling actions. Handles tick-based gathering with
    /// OSRS-style success rolls, animation timing, and resource depletion.
    /// </summary>
    public abstract class SkillingAction : MonoBehaviour
    {
        [Header("Skilling Settings")]
        [SerializeField] protected SkillType skillType;
        [SerializeField] protected int requiredLevel = 1;
        [SerializeField] protected float tickInterval = 2.4f; // OSRS game tick ~0.6s, we use 2.4s for pacing
        [SerializeField] protected int baseXP = 25;
        [SerializeField] protected float baseSuccessChance = 0.5f;

        protected bool isActive;
        protected Coroutine skillingCoroutine;
        protected ResourceNode currentNode;

        public SkillType Skill => skillType;
        public bool IsActive => isActive;

        public virtual bool CanStart(ResourceNode node)
        {
            if (node == null || node.IsDepleted) return false;
            if (!SkillManager.Instance.MeetsRequirement(skillType, requiredLevel)) return false;
            if (InventoryManager.Instance.IsInventoryFull()) return false;
            return true;
        }

        public virtual void StartSkilling(ResourceNode node)
        {
            if (!CanStart(node)) return;

            currentNode = node;
            isActive = true;
            skillingCoroutine = StartCoroutine(SkillingLoop());
            EventManager.Publish(GameEvents.SkillingStarted, skillType);
        }

        public virtual void StopSkilling()
        {
            if (!isActive) return;

            isActive = false;
            if (skillingCoroutine != null)
            {
                StopCoroutine(skillingCoroutine);
                skillingCoroutine = null;
            }
            currentNode = null;
            EventManager.Publish(GameEvents.SkillingStopped, skillType);
        }

        protected virtual IEnumerator SkillingLoop()
        {
            while (isActive && currentNode != null && !currentNode.IsDepleted)
            {
                if (InventoryManager.Instance.IsInventoryFull())
                {
                    EventManager.Publish(GameEvents.InventoryFull);
                    StopSkilling();
                    yield break;
                }

                // Play animation
                OnTickStart();

                yield return new WaitForSeconds(tickInterval);

                // Roll for success
                if (RollSuccess())
                {
                    OnSuccess();
                }
                else
                {
                    OnFailure();
                }

                EventManager.Publish(GameEvents.SkillingTick, skillType);
            }

            StopSkilling();
        }

        protected virtual bool RollSuccess()
        {
            int level = SkillManager.Instance.GetLevel(skillType);
            // Higher level = higher success chance, scaled between base and 95%
            float levelBonus = (level - requiredLevel) * 0.01f;
            float chance = Mathf.Clamp(baseSuccessChance + levelBonus, 0.1f, 0.95f);
            return Random.value <= chance;
        }

        protected virtual void OnSuccess()
        {
            if (currentNode == null) return;

            // Grant XP
            int xpAmount = CalculateXP();
            SkillManager.Instance.AddXP(skillType, xpAmount);

            // Give item
            ItemData harvestedItem = currentNode.GetHarvestedItem();
            if (harvestedItem != null)
            {
                InventoryManager.Instance.AddItem(harvestedItem, 1);
            }

            // Deplete resource
            currentNode.Harvest();

            EventManager.Publish(GameEvents.ResourceHarvested, currentNode);
        }

        protected virtual void OnFailure()
        {
            // Swing and miss - nothing happens, wait for next tick
        }

        protected virtual void OnTickStart()
        {
            // Override for animation triggers
        }

        protected virtual int CalculateXP()
        {
            return baseXP;
        }
    }
}
