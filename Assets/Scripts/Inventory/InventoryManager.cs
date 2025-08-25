using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Lớp này định nghĩa một ô trong túi đồ
[System.Serializable]
public class InventoryItem
{
    public DropItemData data;
    public int quantity;

    public InventoryItem(DropItemData itemData)
    {
        data = itemData;
        quantity = 1;
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

    public event System.Action OnInventoryChanged;
    public event System.Action<int> OnSelectedSlotChanged;


    public int sizeInventory = 10;
    public int selectedSlot = 0;

    public List<InventoryItem> inventory = new List<InventoryItem>();

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
        }
    }

    public void AddItem(DropItemData itemData)
    {
        // Kiểm tra xem item có thể cộng dồn và đã có trong túi đồ chưa
        InventoryItem existingItem = inventory.FirstOrDefault(item => item.data.id == itemData.id);

        if (existingItem != null)
        {
            existingItem.AddToStack();
            Debug.Log($"Đã thêm 1 {itemData.itemName} vào túi. Tổng cộng: {existingItem.quantity}");
        }
        else
        {
            // Chỉ kiểm tra giới hạn khi thêm một loại vật phẩm MỚI
            if (inventory.Count < sizeInventory)
            {
                inventory.Add(new InventoryItem(itemData));
                Debug.Log($"Đã thêm vật phẩm mới: {itemData.itemName}");
            }
            else
            {
                Debug.Log("Túi đồ đã đầy! Không thể thêm vật phẩm mới.");
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public void ChangeSelectedSlot(int newValue)
    {
        selectedSlot = newValue;
        OnSelectedSlotChanged?.Invoke(selectedSlot);
    }
}
