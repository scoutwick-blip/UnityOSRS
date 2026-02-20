using UnityEngine;
using RuneRealm.Player;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Mining skill action. Mine rocks to gather ore.
    /// </summary>
    public class MiningAction : SkillingAction
    {
        [Header("Mining")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string mineAnimTrigger = "Mine";

        private void Awake()
        {
            skillType = SkillType.Mining;
        }

        protected override void OnTickStart()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(mineAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            int level = SkillManager.Instance.GetLevel(SkillType.Mining);
            int rockLevel = requiredLevel;

            float toolBonus = GetToolBonus();
            float successRate = Mathf.Clamp01((level + toolBonus - rockLevel + 1f) / 128f);
            successRate = Mathf.Max(successRate, 0.05f);

            return Random.value <= successRate;
        }

        private float GetToolBonus()
        {
            int tier = EquipmentManager.Instance != null
                ? EquipmentManager.Instance.GetToolTier(SkillType.Mining)
                : 0;
            return tier switch
            {
                1 => 1f,   // Bronze
                2 => 6f,   // Iron
                3 => 11f,  // Steel
                4 => 21f,  // Mithril
                5 => 31f,  // Adamant
                6 => 41f,  // Rune
                _ => 1f,   // No tool / unknown — bronze level
            };
        }

        protected override void OnSuccess()
        {
            base.OnSuccess();
            // Spawn rock particles/fragments
            if (currentNode != null)
            {
                var sparks = currentNode.GetComponentInChildren<ParticleSystem>();
                if (sparks != null) sparks.Play();
            }
        }
    }
}
