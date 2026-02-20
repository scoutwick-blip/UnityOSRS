using UnityEngine;
using RuneRealm.Utils;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Woodcutting skill action. Chop trees to gather logs.
    /// Success rate scales with level and axe tier.
    /// </summary>
    public class WoodcuttingAction : SkillingAction
    {
        [Header("Woodcutting")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string chopAnimTrigger = "Chop";

        private void Awake()
        {
            skillType = SkillType.Woodcutting;
        }

        protected override void OnTickStart()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(chopAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            int level = SkillManager.Instance.GetLevel(SkillType.Woodcutting);
            int treeLevel = requiredLevel;

            // OSRS-style formula: (level - treeLevel + toolBonus) / 256
            float toolBonus = GetToolBonus();
            float successRate = Mathf.Clamp01((level + toolBonus - treeLevel + 1f) / 128f);
            successRate = Mathf.Max(successRate, 0.05f);

            return Random.value <= successRate;
        }

        private float GetToolBonus()
        {
            // Tool tier: Bronze=1, Iron=6, Steel=11, Mithril=21, Adamant=31, Rune=41, Dragon=61
            // Default to bronze if no tool equipped
            return 6f;
        }

        protected override int CalculateXP()
        {
            return baseXP;
        }

        protected override void OnSuccess()
        {
            base.OnSuccess();
            // Tree-specific: shake effect
            if (currentNode != null)
            {
                var shaker = currentNode.GetComponent<TreeShakeEffect>();
                if (shaker != null) shaker.Shake();
            }
        }
    }
}
