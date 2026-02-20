using UnityEngine;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Fishing skill action. Catch fish at fishing spots.
    /// </summary>
    public class FishingAction : SkillingAction
    {
        [Header("Fishing")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string castAnimTrigger = "Cast";
        [SerializeField] private string reelAnimTrigger = "Reel";

        private void Awake()
        {
            skillType = SkillType.Fishing;
            tickInterval = 3.0f; // Fishing is slightly slower
        }

        protected override void OnTickStart()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(castAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            int level = SkillManager.Instance.GetLevel(SkillType.Fishing);
            int spotLevel = requiredLevel;

            float successRate = Mathf.Clamp01((level - spotLevel + 20f) / 100f);
            successRate = Mathf.Max(successRate, 0.1f);

            return Random.value <= successRate;
        }

        protected override void OnSuccess()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(reelAnimTrigger);

            base.OnSuccess();
        }
    }
}
