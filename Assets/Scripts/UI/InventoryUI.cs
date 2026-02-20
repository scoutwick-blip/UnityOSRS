using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using RuneRealm.Inventory;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style inventory UI with item grid, detail view, and category tabs.
    /// Self-builds all elements at runtime when no editor references are assigned.
    /// Toggle with I or Tab.
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
            BuildUIIfNeeded();

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
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }

            if (isOpen && kb.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
            }
        }

        // ─── Runtime UI Construction ───────────────────────────────────

        private void BuildUIIfNeeded()
        {
            if (menuRoot != null) return; // Already wired up

            var rt = GetComponent<RectTransform>();

            // Semi-transparent fullscreen overlay
            menuRoot = new GameObject("InventoryRoot");
            menuRoot.transform.SetParent(rt, false);
            var rootRT = menuRoot.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

            // Dim background
            var bgImg = menuRoot.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            bgImg.raycastTarget = true;

            menuGroup = menuRoot.AddComponent<CanvasGroup>();

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(menuRoot.transform, false);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1);
            titleRT.anchorMax = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -40);
            titleRT.sizeDelta = new Vector2(400, 40);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "INVENTORY";
            titleTMP.fontSize = 28;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(0.9f, 0.85f, 0.7f);
            titleTMP.raycastTarget = false;

            // Hint
            var hintGO = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGO.transform.SetParent(menuRoot.transform, false);
            var hintRT = hintGO.GetComponent<RectTransform>();
            hintRT.anchorMin = new Vector2(0.5f, 1);
            hintRT.anchorMax = new Vector2(0.5f, 1);
            hintRT.anchoredPosition = new Vector2(0, -70);
            hintRT.sizeDelta = new Vector2(400, 20);
            var hintTMP = hintGO.GetComponent<TextMeshProUGUI>();
            hintTMP.text = "Press I or Tab to close  |  Right-click to drop";
            hintTMP.fontSize = 12;
            hintTMP.alignment = TextAlignmentOptions.Center;
            hintTMP.color = new Color(0.6f, 0.6f, 0.6f);
            hintTMP.raycastTarget = false;

            // Slot grid container (centered panel)
            var gridPanel = new GameObject("GridPanel", typeof(RectTransform), typeof(Image));
            gridPanel.transform.SetParent(menuRoot.transform, false);
            var gpRT = gridPanel.GetComponent<RectTransform>();
            gpRT.anchorMin = new Vector2(0.5f, 0.5f);
            gpRT.anchorMax = new Vector2(0.5f, 0.5f);
            gpRT.anchoredPosition = new Vector2(-100, 0);
            gpRT.sizeDelta = new Vector2(340, 500);
            gridPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.7f);

            var gridLayout = gridPanel.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(70, 70);
            gridLayout.spacing = new Vector2(4, 4);
            gridLayout.padding = new RectOffset(8, 8, 8, 8);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            slotContainer = gridPanel.transform;

            // Create slot prefab template (not parented — used as template for Instantiate)
            slotPrefab = CreateSlotPrefabTemplate();

            // Detail panel (right side)
            var detailPanelGO = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
            detailPanelGO.transform.SetParent(menuRoot.transform, false);
            var dpRT = detailPanelGO.GetComponent<RectTransform>();
            dpRT.anchorMin = new Vector2(0.5f, 0.5f);
            dpRT.anchorMax = new Vector2(0.5f, 0.5f);
            dpRT.anchoredPosition = new Vector2(200, 0);
            dpRT.sizeDelta = new Vector2(240, 300);
            detailPanelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.7f);

            detailGroup = detailPanelGO.AddComponent<CanvasGroup>();
            detailGroup.alpha = 0f;

            itemNameText = CreateAlignedText(detailPanelGO.transform, "ItemName", "",
                20, TextAlignmentOptions.Center, new Color(0.9f, 0.85f, 0.4f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, 30));

            itemDescriptionText = CreateAlignedText(detailPanelGO.transform, "ItemDesc", "",
                14, TextAlignmentOptions.TopLeft, new Color(0.75f, 0.75f, 0.75f),
                new Vector2(0, 0.3f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero);
            itemDescriptionText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
            itemDescriptionText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);

            itemValueText = CreateAlignedText(detailPanelGO.transform, "ItemValue", "",
                14, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f),
                new Vector2(0, 0), new Vector2(1, 0.15f), Vector2.zero, Vector2.zero);

            // Footer (slot count)
            var footerGO = new GameObject("Footer", typeof(RectTransform), typeof(TextMeshProUGUI));
            footerGO.transform.SetParent(menuRoot.transform, false);
            var fRT = footerGO.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0.5f, 0);
            fRT.anchorMax = new Vector2(0.5f, 0);
            fRT.anchoredPosition = new Vector2(-100, 40);
            fRT.sizeDelta = new Vector2(340, 25);
            slotsUsedText = footerGO.GetComponent<TextMeshProUGUI>();
            slotsUsedText.text = "0/28";
            slotsUsedText.fontSize = 14;
            slotsUsedText.alignment = TextAlignmentOptions.Center;
            slotsUsedText.color = new Color(0.6f, 0.6f, 0.6f);
            slotsUsedText.raycastTarget = false;

            Debug.Log("[InventoryUI] UI built at runtime.");
        }

        private GameObject CreateSlotPrefabTemplate()
        {
            // Create a hidden template — it will be Instantiated into the grid
            var slotGO = new GameObject("SlotTemplate", typeof(RectTransform));
            slotGO.SetActive(false); // Hidden template

            // Background
            var bg = slotGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.4f);
            bg.raycastTarget = true;

            // Icon child
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.enabled = false;
            iconImg.raycastTarget = false;

            // Quantity text child
            var qtyGO = new GameObject("Quantity", typeof(RectTransform), typeof(TextMeshProUGUI));
            qtyGO.transform.SetParent(slotGO.transform, false);
            var qtyRT = qtyGO.GetComponent<RectTransform>();
            qtyRT.anchorMin = new Vector2(0.5f, 0);
            qtyRT.anchorMax = new Vector2(1, 0.35f);
            qtyRT.offsetMin = qtyRT.offsetMax = Vector2.zero;
            var qtyTMP = qtyGO.GetComponent<TextMeshProUGUI>();
            qtyTMP.text = "";
            qtyTMP.fontSize = 12;
            qtyTMP.alignment = TextAlignmentOptions.BottomRight;
            qtyTMP.color = Color.white;
            qtyTMP.raycastTarget = false;

            // Add InventorySlotUI and wire references via the component
            var slotUI = slotGO.AddComponent<InventorySlotUI>();
            slotUI.SetupRuntimeReferences(iconImg, qtyTMP, bg, null);

            // Keep template in this transform, hidden
            slotGO.transform.SetParent(transform, false);

            return slotGO;
        }

        // ─── Helpers ───────────────────────────────────────────────────

        private static TextMeshProUGUI CreateAlignedText(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ─── Menu Toggle ──────────────────────────────────────────────

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
                slotGO.SetActive(true); // Un-hide the clone
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
            if (slotUIs != null && index >= 0 && index < slotUIs.Length && slotUIs[index] != null)
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
