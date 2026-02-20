using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using RuneRealm.Inventory;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style inventory UI with item list, detail view, and category tabs.
    /// Shows items in a clean scrollable list rather than grid.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private bool isOpen;

        [Header("Slot Grid")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("Item Detail")]
        [SerializeField] private CanvasGroup detailGroup;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemValueText;
        [SerializeField] private Image itemDetailIcon;

        [Header("Footer")]
        [SerializeField] private TextMeshProUGUI slotsUsedText;
        [SerializeField] private TextMeshProUGUI weightText;

        [Header("Category Tabs")]
        [SerializeField] private Button allTab;
        [SerializeField] private Button weaponsTab;
        [SerializeField] private Button armorTab;
        [SerializeField] private Button foodTab;
        [SerializeField] private Button resourcesTab;
        [SerializeField] private Button miscTab;

        private InventorySlotUI[] slotUIs;
        private int selectedSlot = -1;
        private ItemCategory? activeFilter;

        private void Start()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            CreateSlotGrid();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnSlotChanged += OnSlotChanged;
                InventoryManager.Instance.OnInventoryChanged += RefreshAll;
            }

            SetupCategoryTabs();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleMenu();
            }

            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMenu();
            }
        }

        public void ToggleMenu()
        {
            if (isOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            isOpen = true;
            if (menuRoot != null) menuRoot.SetActive(true);
            if (menuGroup != null) menuGroup.alpha = 1f;
            RefreshAll();

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.InMenu);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMenu()
        {
            isOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Playing);
        }

        private void CreateSlotGrid()
        {
            if (slotPrefab == null || slotContainer == null) return;

            slotUIs = new InventorySlotUI[InventoryManager.InventorySize];

            for (int i = 0; i < InventoryManager.InventorySize; i++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotContainer);
                var slotUI = slotGO.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                    slotUI = slotGO.AddComponent<InventorySlotUI>();

                slotUI.Initialize(i, this);
                slotUIs[i] = slotUI;
            }
        }

        private void SetupCategoryTabs()
        {
            if (allTab != null) allTab.onClick.AddListener(() => SetFilter(null));
            if (weaponsTab != null) weaponsTab.onClick.AddListener(() => SetFilter(ItemCategory.Weapon));
            if (armorTab != null) armorTab.onClick.AddListener(() => SetFilter(ItemCategory.Armor));
            if (foodTab != null) foodTab.onClick.AddListener(() => SetFilter(ItemCategory.Food));
            if (resourcesTab != null) resourcesTab.onClick.AddListener(() => SetFilter(ItemCategory.Resource));
            if (miscTab != null) miscTab.onClick.AddListener(() => SetFilter(ItemCategory.Misc));
        }

        private void SetFilter(ItemCategory? category)
        {
            activeFilter = category;
            RefreshAll();
        }

        public void SelectSlot(int index)
        {
            selectedSlot = index;
            UpdateDetailPanel(index);

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null)
                    slotUIs[i].SetSelected(i == index);
            }
        }

        private void UpdateDetailPanel(int slotIndex)
        {
            if (InventoryManager.Instance == null) return;

            var slot = InventoryManager.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                if (detailGroup != null) detailGroup.alpha = 0f;
                return;
            }

            if (detailGroup != null) detailGroup.alpha = 1f;

            if (itemNameText != null)
            {
                itemNameText.text = slot.item.itemName;
                itemNameText.color = GetRarityColor(slot.item.rarity);
            }

            if (itemDescriptionText != null)
                itemDescriptionText.text = slot.item.description;

            if (itemValueText != null)
                itemValueText.text = $"Value: {slot.item.generalStoreValue} gp";

            if (itemDetailIcon != null && slot.item.icon != null)
                itemDetailIcon.sprite = slot.item.icon;
        }

        private void OnSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < slotUIs.Length && slotUIs[index] != null)
                slotUIs[index].Refresh();
        }

        public void RefreshAll()
        {
            if (slotUIs == null) return;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null) slotUIs[i].Refresh();
            }

            UpdateFooter();
        }

        private void UpdateFooter()
        {
            if (InventoryManager.Instance == null) return;

            if (slotsUsedText != null)
                slotsUsedText.text = $"{InventoryManager.Instance.GetUsedSlots()}/{InventoryManager.InventorySize}";
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => Color.white,
                ItemRarity.Uncommon => new Color(0.3f, 0.85f, 0.3f),
                ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
                ItemRarity.Epic => new Color(0.6f, 0.3f, 0.9f),
                ItemRarity.Legendary => new Color(1f, 0.65f, 0f),
                _ => Color.white
            };
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnSlotChanged -= OnSlotChanged;
                InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
            }
        }
    }
}
