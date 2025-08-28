using System.Collections.Generic;
using UnityEngine;

public class UI_CraftingWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private GameObject craftingBackground;
    
    [SerializeField] private List<UI_CraftingSlot> craftingGridSlots;
    [SerializeField] private UI_CraftingSlot resultSlot;
    [SerializeField] private Transform slotContainer; // Parent object cho các slot trong grid

    private void Awake()
    {
        // Gán loại và chỉ số cho các slot ngay từ đầu.
        // Đây là bước cực kỳ quan trọng đã bị bỏ sót.
        for (int i = 0; i < craftingGridSlots.Count; i++)
        {
            craftingGridSlots[i].slotType = SlotType.Crafting;
            craftingGridSlots[i].slotIndex = i;
        }

        if (resultSlot != null)
        {
            resultSlot.slotType = SlotType.Result;
            // index của result slot không quá quan trọng, có thể để là 0
            resultSlot.slotIndex = 0;
        }
    }

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện thay đổi từ mỗi slot trong lưới chế tạo
        foreach (var slot in craftingGridSlots)
        {
            slot.OnSlotChanged += HandleCraftingGridChanged;
        }

        // Ẩn cửa sổ khi bắt đầu
        craftingPanel.SetActive(false);
        craftingBackground.SetActive(false);

        // Đảm bảo CraftingManager tồn tại trong scene
        if (CraftingManager.instance == null)
        {
            Debug.LogError("Không tìm thấy CraftingManager instance trong scene!");
        }
    }

    private void Update()
    {
        // Thêm phím tắt để mở/đóng cửa sổ chế tạo, ví dụ: phím 'C'
        if (Input.GetKeyDown(KeyCode.C))
        {
            craftingPanel.SetActive(!craftingPanel.activeSelf);
            craftingBackground.SetActive(!craftingBackground.activeSelf);
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi
        foreach (var slot in craftingGridSlots)
        {
            slot.OnSlotChanged -= HandleCraftingGridChanged;
        }
    }

    /// <summary>
    /// Được gọi mỗi khi có một item được thêm hoặc bớt khỏi lưới chế tạo.
    /// Đây là nơi chúng ta sẽ kích hoạt logic kiểm tra công thức.
    /// </summary>
    private void HandleCraftingGridChanged(UI_CraftingSlot changedSlot)
    {
        // 1. Lấy pattern hiện tại từ craftingGridSlots.
        List<ItemData> currentPattern = new List<ItemData>(); // << THAY ĐỔI
        foreach (var slot in craftingGridSlots)
        {
            if (slot.currentItem != null)
            {
                currentPattern.Add(slot.currentItem.data);
            }
            else
            {
                currentPattern.Add(null); // Thêm null cho các ô trống
            }
        }

        // --- LOGIC MỚI ---
        // Tính toán chiều rộng của lưới. Giả định đây là lưới vuông.
        int gridWidth = (int)Mathf.Sqrt(craftingGridSlots.Count);
        if (gridWidth * gridWidth != craftingGridSlots.Count)
        {
            Debug.LogError("Lưới chế tạo không phải hình vuông! Chức năng kiểm tra công thức có thể hoạt động sai.");
        }

        // 2. Gọi CraftingManager để so sánh pattern này với tất cả các công thức.
        CraftingRecipe matchedRecipe = CraftingManager.instance.CheckRecipe(currentPattern, gridWidth);
        // --- KẾT THÚC LOGIC MỚI ---

        // 3. Nếu khớp, hiển thị kết quả trong resultSlot.
        if (matchedRecipe != null)
        {
            // Tạo một InventoryItem mới cho kết quả
            InventoryItem resultItem = new InventoryItem(matchedRecipe.resultItem, matchedRecipe.resultQuantity);
            resultSlot.UpdateSlot(resultItem);
        }
        else // 4. Nếu không khớp, làm trống resultSlot.
        {
            resultSlot.UpdateSlot(null);
        }
    }

    /// Phương thức này được gọi bởi Result Slot khi nó được nhấp vào.
    /// Nó sẽ kích hoạt quá trình chế tạo vật phẩm.

    public void OnResultSlotClicked()
    {
        InventoryItem resultItem = resultSlot.currentItem;
        if (resultItem == null)
        {
            // Debug.LogWarning("Ô kết quả được nhấp nhưng không có gì để chế tạo.");
            return;
        }

        // TODO: Kiểm tra xem túi đồ có đủ chỗ trống không trước khi chế tạo.
        // Hiện tại, chúng ta sẽ giả định là có thể thêm vào.

        // Thêm vật phẩm kết quả vào túi đồ của người chơi
        // Lặp lại theo số lượng kết quả
        for (int i = 0; i < resultItem.quantity; i++)
        {
            InventoryManager.instance.AddItem(resultItem.data);
        }

        Debug.Log($"Đã chế tạo {resultItem.quantity}x {resultItem.data.itemName}!");

        ConsumeIngredients();
    }

    /// Giảm số lượng của mỗi vật phẩm trong lưới chế tạo đi một.
    /// </summary>
    private void ConsumeIngredients()
    {
        // Sử dụng vòng lặp for thông thường để có thể truy cập và gán lại item một cách an toàn.
        for (int i = 0; i < craftingGridSlots.Count; i++)
        {
            UI_CraftingSlot slot = craftingGridSlots[i];
            
            if (slot.currentItem != null)
            {
                Debug.Log($"[CRAFTING] Tiêu thụ 1x '{slot.currentItem.data.itemName}' từ lưới chế tạo. Số lượng ban đầu: {slot.currentItem.quantity}");

                int remainingQuantity = slot.currentItem.quantity - 1;

                if (remainingQuantity <= 0)
                {
                    // Xóa hoàn toàn vật phẩm khỏi slot.
                    craftingGridSlots[i].UpdateSlot(null);
                }
                else
                {
                    // TẠO MỚI và GÁN LẠI một cách dứt khoát để phá vỡ mọi tham chiếu cũ.
                    InventoryItem updatedItem = new InventoryItem(slot.currentItem.data, remainingQuantity);
                    craftingGridSlots[i].UpdateSlot(updatedItem);
                }
            }
        }
        
        // Sau khi tất cả các nguyên liệu đã được xử lý xong,
        // gọi lại hàm kiểm tra công thức một lần duy nhất để cập nhật ô kết quả.
        HandleCraftingGridChanged(null);
    }
}
