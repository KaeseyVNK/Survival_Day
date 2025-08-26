using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel; // Tham chiếu đến panel chính của túi đồ
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    private List<UI_Slot> slotScripts = new List<UI_Slot>();
    private InventoryManager inventory;

    private void Start()
    {
        inventory = InventoryManager.instance;
        inventory.OnInventoryChanged += UpdateDisplay; // Lắng nghe sự kiện

        CreateSlots();
        UpdateDisplay(); // Cập nhật lần đầu

        inventoryPanel.SetActive(false); // Ẩn túi đồ khi bắt đầu game
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            // Bật/tắt panel túi đồ
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= UpdateDisplay; // Ngừng lắng nghe để tránh lỗi
        }
    }

    private void CreateSlots()
    {
        for (int i = 0; i < inventory.mainInventorySize; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            UI_Slot slotScript = newSlot.GetComponent<UI_Slot>();
            if (slotScript != null)
            {
                slotScript.slotType = SlotType.MainInventory;
                slotScript.slotIndex = i;
                slotScripts.Add(slotScript);
            }
        }
    }

    private void UpdateDisplay()
    {
        // Lặp qua tất cả các slot UI đã được tạo
        for (int i = 0; i < slotScripts.Count; i++)
        {
            // Đảm bảo rằng index không vượt quá giới hạn của mảng inventory
            if (i < inventory.mainInventory.Length)
            {
                // Cập nhật slot với item tương ứng (có thể là null)
                slotScripts[i].UpdateSlot(inventory.mainInventory[i]);
            }
            else
            {
                // Nếu có nhiều slot UI hơn item data (không nên xảy ra), hãy xóa nó
                slotScripts[i].UpdateSlot(null);
            }
        }
    }
}
