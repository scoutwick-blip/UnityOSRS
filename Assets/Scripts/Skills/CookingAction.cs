using UnityEngine;
using RuneRealm.Inventory;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Cooking skill action. Cook raw food on ranges/fires.
    /// Can burn food if level is too low.
    /// </summary>
    public class CookingAction : SkillingAction
    {
        [Header("Cooking")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string cookAnimTrigger = "Cook";
        [SerializeField] private ItemData burntItem;
        [SerializeField] private int burnStopLevel = 99; // Level at which you stop burning this food

        private ItemData rawFoodToCook;

        private void Awake()
        {
            skillType = SkillType.Cooking;
            tickInterval = 2.0f;
        }

        public void SetRawFood(ItemData rawFood)
        {
            rawFoodToCook = rawFood;
        }

        protected override void OnTickStart()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(cookAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            int level = SkillManager.Instance.GetLevel(SkillType.Cooking);

            if (level >= burnStopLevel) return true;

            // Burn chance decreases with level
            float burnChance = Mathf.Clamp01(1f - ((float)(level - requiredLevel) / (burnStopLevel - requiredLevel)));
            return Random.value > burnChance;
        }

        protected override void OnSuccess()
        {
            // Successfully cooked - grant XP and cooked item
            SkillManager.Instance.AddXP(SkillType.Cooking, baseXP);

            if (rawFoodToCook != null)
            {
                InventoryManager.Instance.RemoveItem(rawFoodToCook, 1);
            }

            if (currentNode != null)
            {
                ItemData cookedItem = currentNode.GetHarvestedItem();
                if (cookedItem != null)
                    InventoryManager.Instance.AddItem(cookedItem, 1);
            }
        }

        protected override void OnFailure()
        {
            // Burned the food
            if (rawFoodToCook != null)
                InventoryManager.Instance.RemoveItem(rawFoodToCook, 1);

            if (burntItem != null)
                InventoryManager.Instance.AddItem(burntItem, 1);

            // Still get a small amount of XP for trying (OSRS-accurate: you don't, but for game feel)
            SkillManager.Instance.AddXP(SkillType.Cooking, 1);
        }
    }
}
