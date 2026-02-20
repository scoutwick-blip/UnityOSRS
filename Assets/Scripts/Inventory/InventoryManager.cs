using System.Collections.Generic;
using UnityEngine;
using RuneRealm.Core;

namespace RuneRealm.Inventory
{
    /// <summary>
    /// OSRS-style 28-slot inventory system.
    /// Supports stackable items, equipment, and item manipulation.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public const int InventorySize = 28;

        [SerializeField] private InventorySlot[] slots;

        public event System.Action<int, InventorySlot> OnSlotChanged;
        public event System.Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            slots = new InventorySlot[InventorySize];
            for (int i = 0; i < InventorySize; i++)
                slots[i] = new InventorySlot();
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            // If stackable, try to add to existing stack first
            if (item.isStackable)
            {
                for (int i = 0; i < InventorySize; i++)
                {
                    if (slots[i].item == item && slots[i].quantity < item.maxStackSize)
                    {
                        int spaceInStack = item.maxStackSize - slots[i].quantity;
                        int toAdd = Mathf.Min(quantity, spaceInStack);
                        slots[i].quantity += toAdd;
                        quantity -= toAdd;

                        OnSlotChanged?.Invoke(i, slots[i]);

                        if (quantity <= 0)
                        {
                            OnInventoryChanged?.Invoke();
                            EventManager.Publish(GameEvents.ItemAdded, item);
                            return true;
                        }
                    }
                }
            }

            // Add to empty slots
            while (quantity > 0)
            {
                int emptySlot = FindEmptySlot();
                if (emptySlot == -1)
                {
                    EventManager.Publish(GameEvents.InventoryFull);
                    OnInventoryChanged?.Invoke();
                    return false;
                }

                int toAdd = item.isStackable
                    ? Mathf.Min(quantity, item.maxStackSize)
                    : 1;

                slots[emptySlot].item = item;
                slots[emptySlot].quantity = toAdd;
                quantity -= toAdd;

                OnSlotChanged?.Invoke(emptySlot, slots[emptySlot]);
            }

            OnInventoryChanged?.Invoke();
            EventManager.Publish(GameEvents.ItemAdded, item);
            return true;
        }

        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;

            for (int i = InventorySize - 1; i >= 0 && remaining > 0; i--)
            {
                if (slots[i].item == item)
                {
                    int toRemove = Mathf.Min(remaining, slots[i].quantity);
                    slots[i].quantity -= toRemove;
                    remaining -= toRemove;

                    if (slots[i].quantity <= 0)
                        slots[i].Clear();

                    OnSlotChanged?.Invoke(i, slots[i]);
                }
            }

            if (remaining <= 0)
            {
                OnInventoryChanged?.Invoke();
                EventManager.Publish(GameEvents.ItemRemoved, item);
                return true;
            }

            return false;
        }

        public bool RemoveItemAtSlot(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= InventorySize) return false;
            if (slots[slotIndex].IsEmpty) return false;

            ItemData item = slots[slotIndex].item;
            slots[slotIndex].quantity -= quantity;
            if (slots[slotIndex].quantity <= 0)
                slots[slotIndex].Clear();

            OnSlotChanged?.Invoke(slotIndex, slots[slotIndex]);
            OnInventoryChanged?.Invoke();
            EventManager.Publish(GameEvents.ItemRemoved, item);
            return true;
        }

        public void SwapSlots(int from, int to)
        {
            if (from < 0 || from >= InventorySize || to < 0 || to >= InventorySize) return;

            var temp = slots[from];
            slots[from] = slots[to];
            slots[to] = temp;

            OnSlotChanged?.Invoke(from, slots[from]);
            OnSlotChanged?.Invoke(to, slots[to]);
            OnInventoryChanged?.Invoke();
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= InventorySize) return null;
            return slots[index];
        }

        public int GetItemCount(ItemData item)
        {
            int count = 0;
            for (int i = 0; i < InventorySize; i++)
            {
                if (slots[i].item == item)
                    count += slots[i].quantity;
            }
            return count;
        }

        public bool HasItem(ItemData item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }

        public bool IsInventoryFull()
        {
            return FindEmptySlot() == -1;
        }

        public int GetUsedSlots()
        {
            int count = 0;
            for (int i = 0; i < InventorySize; i++)
            {
                if (!slots[i].IsEmpty) count++;
            }
            return count;
        }

        public int GetFreeSlots()
        {
            return InventorySize - GetUsedSlots();
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < InventorySize; i++)
            {
                if (slots[i].IsEmpty) return i;
            }
            return -1;
        }

        public InventorySaveData GetSaveData()
        {
            var data = new InventorySaveData();
            data.slotItemIds = new int[InventorySize];
            data.slotQuantities = new int[InventorySize];

            for (int i = 0; i < InventorySize; i++)
            {
                data.slotItemIds[i] = slots[i].item != null ? slots[i].item.itemId : -1;
                data.slotQuantities[i] = slots[i].quantity;
            }

            return data;
        }
    }

    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public bool IsEmpty => item == null || quantity <= 0;

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public int[] slotItemIds;
        public int[] slotQuantities;
    }
}
