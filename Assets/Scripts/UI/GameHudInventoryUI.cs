using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudInventoryUI : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("Inventory slot UI entries.")]
    [SerializeField] private InventorySlot[] inventorySlots;

    [Tooltip("Maximum slot count reserved for this inventory UI.")]
    [SerializeField] private int maxSlots = 12;

    [System.Serializable]
    public class InventorySlot
    {
        [Tooltip("Slot background image.")]
        public Image slotBackground;

        [Tooltip("Item icon image.")]
        public Image iconImage;

        [Tooltip("Item name text.")]
        public TextMeshProUGUI nameText;

        [Tooltip("Item amount text.")]
        public TextMeshProUGUI amountText;

        [Tooltip("Current item name in this slot.")]
        public string itemName;

        [Tooltip("Current item amount in this slot.")]
        public int amount;

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(itemName) || amount <= 0;
        }
    }

    private void Start()
    {
        InitializeSlots();
    }

    private void OnValidate()
    {
        maxSlots = Mathf.Max(0, maxSlots);
    }

    private void InitializeSlots()
    {
        if (inventorySlots == null)
        {
            return;
        }

        foreach (var slot in inventorySlots)
        {
            if (slot == null)
            {
                continue;
            }

            slot.itemName = "";
            slot.amount = 0;
            RefreshSlot(slot);
        }
    }

    public void AddItem(string itemName, Sprite icon, int amount)
    {
        if (inventorySlots == null)
        {
            return;
        }

        foreach (var slot in inventorySlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.itemName == itemName)
            {
                slot.amount += amount;
                RefreshSlot(slot);
                return;
            }
        }

        foreach (var slot in inventorySlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.IsEmpty())
            {
                slot.itemName = itemName;
                slot.amount = amount;
                if (slot.iconImage != null)
                {
                    slot.iconImage.sprite = icon;
                    slot.iconImage.enabled = true;
                }

                RefreshSlot(slot);
                return;
            }
        }

        Debug.LogWarning("[GameHudInventoryUI] No empty inventory slots are available.", this);
    }

    public void RemoveItem(string itemName, int amount)
    {
        if (inventorySlots == null)
        {
            return;
        }

        foreach (var slot in inventorySlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.itemName == itemName)
            {
                slot.amount -= amount;

                if (slot.amount <= 0)
                {
                    ClearSlot(slot);
                }
                else
                {
                    RefreshSlot(slot);
                }

                return;
            }
        }
    }

    public void ClearInventory()
    {
        if (inventorySlots == null)
        {
            return;
        }

        foreach (var slot in inventorySlots)
        {
            if (slot == null)
            {
                continue;
            }

            ClearSlot(slot);
        }
    }

    private void RefreshSlot(InventorySlot slot)
    {
        if (slot.nameText != null)
        {
            slot.nameText.text = slot.itemName;
        }

        if (slot.amountText != null)
        {
            slot.amountText.text = slot.amount > 0 ? $"x{slot.amount}" : "";
        }
    }

    private void ClearSlot(InventorySlot slot)
    {
        slot.itemName = "";
        slot.amount = 0;

        if (slot.iconImage != null)
        {
            slot.iconImage.sprite = null;
            slot.iconImage.enabled = false;
        }

        RefreshSlot(slot);
    }

    public int GetItemCount(string itemName)
    {
        if (inventorySlots == null)
        {
            return 0;
        }

        foreach (var slot in inventorySlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.itemName == itemName)
            {
                return slot.amount;
            }
        }

        return 0;
    }

    public bool HasItem(string itemName)
    {
        return GetItemCount(itemName) > 0;
    }
}
