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
        for (int i = 0; i < inventory.sizeInventory; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            slotScripts.Add(newSlot.GetComponent<UI_Slot>());
        }
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < slotScripts.Count; i++)
        {
            if (i < inventory.inventory.Count)
            {
                // Nếu có vật phẩm ở vị trí này, cập nhật slot
                slotScripts[i].UpdateSlot(inventory.inventory[i]);
            }
            else
            {
                // Nếu không, xóa thông tin slot
                slotScripts[i].UpdateSlot(null);
            }
        }
    }
}
