using UnityEngine;
using RuneRealm.Inventory;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Crafting skill action. Craft items from hides, gems, clay, etc.
    /// </summary>
    public class CraftingAction : SkillingAction
    {
        [Header("Crafting")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string craftAnimTrigger = "Craft";
        [SerializeField] private CraftingRecipe currentRecipe;

        private void Awake()
        {
            skillType = SkillType.Crafting;
            tickInterval = 2.0f;
        }

        public void SetRecipe(CraftingRecipe recipe)
        {
            currentRecipe = recipe;
            requiredLevel = recipe.requiredLevel;
            baseXP = recipe.xpReward;
        }

        public override bool CanStart(ResourceNode node)
        {
            if (currentRecipe == null) return false;
            if (!SkillManager.Instance.MeetsRequirement(SkillType.Crafting, currentRecipe.requiredLevel))
                return false;

            foreach (var material in currentRecipe.requiredMaterials)
            {
                if (!InventoryManager.Instance.HasItem(material.item, material.quantity))
                    return false;
            }
            return true;
        }

        protected override void OnTickStart()
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger(craftAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            return true; // Crafting always succeeds
        }

        protected override void OnSuccess()
        {
            if (currentRecipe == null) return;

            foreach (var material in currentRecipe.requiredMaterials)
            {
                InventoryManager.Instance.RemoveItem(material.item, material.quantity);
            }

            InventoryManager.Instance.AddItem(currentRecipe.product, currentRecipe.productQuantity);
            SkillManager.Instance.AddXP(SkillType.Crafting, currentRecipe.xpReward);
        }
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeName;
        public int requiredLevel;
        public MaterialRequirement[] requiredMaterials;
        public ItemData product;
        public int productQuantity = 1;
        public int xpReward;
        public CraftingCategory category;
    }

    public enum CraftingCategory
    {
        Leather,
        Gems,
        Pottery,
        Spinning,
        Glassblowing,
        Jewelry
    }
}
