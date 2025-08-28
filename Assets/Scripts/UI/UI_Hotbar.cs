using System.Collections.Generic;
using UnityEngine;

public class UI_Hotbar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int hotbarSize = 5;

    [Header("References")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    private List<UI_Slot> hotbarSlots = new List<UI_Slot>();
    private InventoryManager inventoryManager;

    private void Start()
    {
        CreateHotbarSlots();

        inventoryManager = InventoryManager.instance;
        if (inventoryManager != null)
        {
            inventoryManager.OnHotbarChanged += UpdateHotbarSlots; // Thay đổi sự kiện
            inventoryManager.OnSelectedSlotChanged += UpdateSelection;
        }

        // Cập nhật giao diện lúc bắt đầu
        UpdateHotbarSlots();
        UpdateSelection(inventoryManager != null ? inventoryManager.selectedSlot : 0);
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnHotbarChanged -= UpdateHotbarSlots; // Thay đổi sự kiện
            inventoryManager.OnSelectedSlotChanged -= UpdateSelection;
        }
    }

    private void Update()
    {
        if (inventoryManager == null) return;

        // Lắng nghe input phím số để đổi slot
        for (int i = 0; i < hotbarSize; i++)
        {
            // KeyCode for '1' is 49, '2' is 50, etc.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                inventoryManager.ChangeSelectedSlot(i);
                Debug.Log("Selected slot: " + i);
                break; // Thoát khỏi vòng lặp sau khi tìm thấy phím được nhấn
            }
        }
    }

    private void CreateHotbarSlots()
    {
        if (slotPrefab == null || slotContainer == null)
        {
            Debug.LogError("Slot Prefab or Slot Container is not assigned in UI_Hotbar!");
            return;
        }

        for (int i = 0; i < hotbarSize; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            UI_Slot newSlotScript = newSlot.GetComponent<UI_Slot>();
            if (newSlotScript != null)
            {
                newSlotScript.slotType = SlotType.Hotbar;
                newSlotScript.slotIndex = i;
                hotbarSlots.Add(newSlotScript);
            }
            else
            {
                Debug.LogError("Slot Prefab does not have a UI_Slot component!");
            }
        }
    }

    private void UpdateHotbarSlots()
    {
        if (inventoryManager == null) return;

        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            if (i < inventoryManager.hotbarItems.Length)
            {
                hotbarSlots[i].UpdateSlot(inventoryManager.hotbarItems[i]);
            }
        }
    }

    private void UpdateSelection(int selectedSlotIndex)
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            hotbarSlots[i].ToggleSelection(i == selectedSlotIndex);
        }
    }
}
