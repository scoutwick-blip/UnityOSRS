using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RuneRealm.Inventory;

namespace RuneRealm.UI
{
    /// <summary>
    /// Individual inventory slot in the UI grid.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;

        private int slotIndex;
        private InventoryUI parentUI;
        private bool isSelected;

        private static Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
        private static Color filledColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        private static Color hoverColor = new Color(0.25f, 0.22f, 0.18f, 0.7f);
        private static Color selectedColor = new Color(0.35f, 0.3f, 0.2f, 0.85f);

        /// <summary>
        /// Allows runtime code to wire up references when building UI programmatically.
        /// </summary>
        public void SetupRuntimeReferences(Image icon, TextMeshProUGUI qty, Image bg, Image border)
        {
            iconImage = icon;
            quantityText = qty;
            backgroundImage = bg;
            borderImage = border;
        }

        public void Initialize(int index, InventoryUI ui)
        {
            slotIndex = index;
            parentUI = ui;
            Refresh();
        }

        public void Refresh()
        {
            if (InventoryManager.Instance == null) return;

            var slot = InventoryManager.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (quantityText != null) quantityText.text = "";
                if (backgroundImage != null) backgroundImage.color = emptyColor;
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = slot.item.icon;
                }

                if (quantityText != null)
                {
                    quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
                }

                if (backgroundImage != null)
                    backgroundImage.color = filledColor;
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (backgroundImage != null)
                backgroundImage.color = selected ? selectedColor : filledColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentUI != null)
                parentUI.SelectSlot(slotIndex);

            // Right click to drop
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryManager.Instance?.RemoveItemAtSlot(slotIndex, 1);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected && backgroundImage != null)
                backgroundImage.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected && backgroundImage != null)
            {
                var slot = InventoryManager.Instance?.GetSlot(slotIndex);
                backgroundImage.color = (slot != null && !slot.IsEmpty) ? filledColor : emptyColor;
            }
        }

        // Drag and drop for inventory rearrangement
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (iconImage != null)
                iconImage.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Visual feedback handled by drag cursor
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (iconImage != null)
                iconImage.raycastTarget = true;

            // Find target slot
            var target = eventData.pointerCurrentRaycast.gameObject;
            if (target != null)
            {
                var targetSlot = target.GetComponent<InventorySlotUI>();
                if (targetSlot != null && targetSlot != this)
                {
                    InventoryManager.Instance?.SwapSlots(slotIndex, targetSlot.slotIndex);
                }
            }
        }
    }
}
