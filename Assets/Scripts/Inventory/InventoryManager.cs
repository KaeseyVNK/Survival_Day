using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public enum SlotType { MainInventory, Hotbar, Crafting, Result }

// Lớp này định nghĩa một ô trong túi đồ
[System.Serializable]
public class InventoryItem
{
    public DropItemData data;
    public int quantity;

    public InventoryItem(DropItemData data)
    {
        this.data = data;
        this.quantity = 1;
    }

    public InventoryItem(DropItemData data, int quantity)
    {
        this.data = data;
        this.quantity = quantity;
    }

    public void AddToStack()
    {
        quantity++;
    }

    public void RemoveFromStack()
    {
        quantity--;
    }
}


public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    // --- Events ---
    public event System.Action OnInventoryChanged; // Cho túi đồ chính
    public event System.Action OnHotbarChanged;    // Cho thanh hotbar
    public event System.Action<int> OnSelectedSlotChanged;

    // --- Inventory Settings ---
    [Header("Inventory Settings")]
    public int mainInventorySize = 20;
    public int hotbarSize = 5;

    // --- Player Hotbar ---
    public int selectedSlot = 0;
    public InventoryItem[] hotbarItems; // Dùng mảng để có các ô cố định, có thể là null

    // --- Main Inventory ---
    public InventoryItem[] mainInventory; // Thay đổi từ List sang Array


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Khởi tạo mảng hotbar và main inventory với kích thước đã định
            hotbarItems = new InventoryItem[hotbarSize];
            mainInventory = new InventoryItem[mainInventorySize];
        }
    }

    public void AddItem(DropItemData itemData)
    {
        // --- Giai đoạn 1: Cố gắng cộng dồn (Stack) ---

        // Ưu tiên 1: Cộng dồn vào Hotbar
        for (int i = 0; i < hotbarItems.Length; i++)
        {
            if (hotbarItems[i] != null && hotbarItems[i].data.id == itemData.id)
            {
                hotbarItems[i].AddToStack();
                Debug.Log($"Đã cộng dồn {itemData.itemName} trên hotbar. Tổng cộng: {hotbarItems[i].quantity}");
                OnHotbarChanged?.Invoke();
                return;
            }
        }

        // Ưu tiên 2: Cộng dồn vào Túi đồ chính (Array)
        for (int i = 0; i < mainInventory.Length; i++)
        {
            if (mainInventory[i] != null && mainInventory[i].data.id == itemData.id)
            {
                mainInventory[i].AddToStack();
                Debug.Log($"Đã cộng dồn {itemData.itemName} trong túi đồ. Tổng cộng: {mainInventory[i].quantity}");
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        // --- Giai đoạn 2: Thêm vào ô trống ---

        // Ưu tiên 3: Thêm vào ô trống trên Hotbar
        for (int i = 0; i < hotbarItems.Length; i++)
        {
            if (hotbarItems[i] == null)
            {
                hotbarItems[i] = new InventoryItem(itemData);
                Debug.Log($"Đã thêm vật phẩm mới '{itemData.itemName}' vào hotbar.");
                OnHotbarChanged?.Invoke();
                return;
            }
        }

        // Ưu tiên 4: Thêm vào túi đồ chính (Array)
        for (int i = 0; i < mainInventory.Length; i++)
        {
            if (mainInventory[i] == null)
            {
                mainInventory[i] = new InventoryItem(itemData);
                Debug.Log($"Hotbar đầy. Đã thêm '{itemData.itemName}' vào túi đồ chính.");
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        // Nếu tất cả đều thất bại
        Debug.Log("Túi đồ và hotbar đã đầy! Không thể thêm vật phẩm mới.");
    }

    public void ChangeSelectedSlot(int newValue)
    {
        selectedSlot = newValue;
        OnSelectedSlotChanged?.Invoke(selectedSlot);
    }

    // --- Drag and Drop Logic ---

    /// <summary>
    /// Di chuyển một item từ vị trí này sang vị trí khác trong data model
    /// </summary>
    public void MoveItem(SlotType sourceType, int sourceIndex, SlotType destType, int destIndex)
    {
        InventoryItem sourceItem = GetItem(sourceType, sourceIndex);
        if (sourceItem == null) return;

        InventoryItem destItem = GetItem(destType, destIndex);

        // Logic gộp stack
        if (destItem != null && sourceItem.data.id == destItem.data.id)
        {
            destItem.quantity += sourceItem.quantity;
            SetItem(sourceType, sourceIndex, null); // Xóa item ở vị trí gốc
        }
        else // Logic hoán đổi vị trí
        {
            SetItem(destType, destIndex, sourceItem);
            SetItem(sourceType, sourceIndex, destItem);
        }

        // Thông báo cho UI cập nhật
        if (sourceType == SlotType.Hotbar || destType == SlotType.Hotbar)
        {
            OnHotbarChanged?.Invoke();
        }
        if (sourceType == SlotType.MainInventory || destType == SlotType.MainInventory)
        {
            OnInventoryChanged?.Invoke();
        }

        
    }

    private InventoryItem GetItem(SlotType slotType, int index)
    {
        if (slotType == SlotType.Hotbar)
        {
            return (index >= 0 && index < hotbarItems.Length) ? hotbarItems[index] : null;
        }
        else
        {
            return (index >= 0 && index < mainInventory.Length) ? mainInventory[index] : null;
        }
    }

    public void SetItem(SlotType slotType, int index, InventoryItem item)
    {
        if (slotType == SlotType.Hotbar)
        {
            if (index >= 0 && index < hotbarItems.Length)
            {
                hotbarItems[index] = item;
                OnHotbarChanged?.Invoke();
            }
        }
        else
        {
            if (index >= 0 && index < mainInventory.Length)
            {
                mainInventory[index] = item;
                OnInventoryChanged?.Invoke();
            }
        }
    }
}
