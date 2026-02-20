using UnityEngine;
using RuneRealm.Inventory;

namespace RuneRealm.Skills
{
    /// <summary>
    /// Smithing skill action. Smelt ores into bars at furnaces,
    /// and smith bars into equipment at anvils.
    /// </summary>
    public class SmithingAction : SkillingAction
    {
        [Header("Smithing")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string smithAnimTrigger = "Smith";
        [SerializeField] private SmithingMode mode = SmithingMode.Smelting;

        [Header("Recipe")]
        [SerializeField] private SmithingRecipe currentRecipe;

        private void Awake()
        {
            skillType = SkillType.Smithing;
            tickInterval = 2.4f;
        }

        public void SetRecipe(SmithingRecipe recipe)
        {
            currentRecipe = recipe;
            requiredLevel = recipe.requiredLevel;
            baseXP = recipe.xpReward;
        }

        public override bool CanStart(ResourceNode node)
        {
            if (!base.CanStart(node)) return false;
            if (currentRecipe == null) return false;

            // Check if player has required materials
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
                playerAnimator.SetTrigger(smithAnimTrigger);
        }

        protected override bool RollSuccess()
        {
            // Smithing always succeeds if you have the level
            return true;
        }

        protected override void OnSuccess()
        {
            if (currentRecipe == null) return;

            // Remove materials
            foreach (var material in currentRecipe.requiredMaterials)
            {
                InventoryManager.Instance.RemoveItem(material.item, material.quantity);
            }

            // Add product
            InventoryManager.Instance.AddItem(currentRecipe.product, currentRecipe.productQuantity);

            // Grant XP
            SkillManager.Instance.AddXP(SkillType.Smithing, currentRecipe.xpReward);
        }
    }

    public enum SmithingMode
    {
        Smelting,
        Smithing
    }

    [System.Serializable]
    public class SmithingRecipe
    {
        public string recipeName;
        public int requiredLevel;
        public MaterialRequirement[] requiredMaterials;
        public ItemData product;
        public int productQuantity = 1;
        public int xpReward;
        public SmithingMode mode;
    }

    [System.Serializable]
    public class MaterialRequirement
    {
        public ItemData item;
        public int quantity;
    }
}
