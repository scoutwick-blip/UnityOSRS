using UnityEngine;

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
            return 6f; // Default to iron pickaxe bonus
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
