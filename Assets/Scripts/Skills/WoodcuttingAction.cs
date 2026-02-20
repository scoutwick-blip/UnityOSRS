using UnityEngine;
using RuneRealm.Player;
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
            int tier = EquipmentManager.Instance != null
                ? EquipmentManager.Instance.GetToolTier(SkillType.Woodcutting)
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
