using UnityEngine;
using RuneRealm.Inventory;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Firemaking skill. Burn logs to create fires for cooking and warmth.
    /// Higher level logs grant more XP.
    /// </summary>
    public class FiremakingAction : SkillingAction
    {
        [Header("Firemaking")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string firemakeAnimTrigger = "Firemake";
        [SerializeField] private GameObject firePrefab;
        [SerializeField] private float fireDuration = 60f;

        private ItemData logsToBurn;

        private void Awake()
        {
            skillType = SkillType.Firemaking;
            tickInterval = 3.0f;
        }

        public void SetLogs(ItemData logs)
        {
            logsToBurn = logs;
        }

        protected override void OnTickStart()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(firemakeAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            int level = SkillManager.Instance.GetLevel(SkillType.Firemaking);
            float chance = Mathf.Clamp01(0.5f + (level - requiredLevel) * 0.02f);
            return Random.value <= chance;
        }

        protected override void OnSuccess()
        {
            // Remove logs from inventory
            if (logsToBurn != null)
            {
                InventoryManager.Instance.RemoveItem(logsToBurn, 1);
            }

            // Spawn fire
            if (firePrefab != null)
            {
                Vector3 firePos = transform.position + transform.forward * 1.5f;
                GameObject fire = Instantiate(firePrefab, firePos, Quaternion.identity);
                Destroy(fire, fireDuration);
            }

            // Grant XP
            SkillManager.Instance.AddXP(SkillType.Firemaking, baseXP);

            // Stop after one log
            StopSkilling();
        }
    }
}
