using UnityEngine;
using UnityEngine.EventSystems;

// Lớp này gần giống với UI_Slot nhưng được chuyên biệt hóa cho việc chế tạo.
// Nó cần thông báo cho cửa sổ chế tạo chính mỗi khi nội dung của nó thay đổi.
// Thêm IPointerDownHandler để có thể bắt sự kiện click chuột
public class UI_CraftingSlot : UI_Slot, IPointerDownHandler
{
    // Thêm một sự kiện để thông báo cho Crafting Window khi slot này thay đổi
    public event System.Action<UI_CraftingSlot> OnSlotChanged;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // Logic cũ đã chặn việc kéo-thả khỏi ô kết quả.
        // Logic mới của chúng ta ở UI_Slot.OnDrop đã xử lý việc này,
        // vì vậy chúng ta chỉ cần gọi thẳng đến logic của lớp cha để bắt đầu kéo như bình thường.
        base.OnBeginDrag(eventData);
    }

    // Ghi đè phương thức OnDrop để xử lý logic riêng cho crafting
    public override void OnDrop(PointerEventData eventData)
    {
        // Debug.Log($"<color=orange>[OnDrop]</color> Đã nhận sự kiện thả vào slot: <b>{gameObject.name}</b>");

        if (sourceSlot == null || sourceSlot == this)
        {
            // Debug.LogWarning("[OnDrop] Bỏ qua: Nguồn không hợp lệ hoặc thả vào chính nó.");
            return;
        }

        // --- LOGIC HOÁN ĐỔI VÀ SAO CHÉP MỚI ---
        
        InventoryItem itemComingIn = sourceSlot.currentItem; // Vật phẩm đang được kéo đến
        InventoryItem itemGoingOut = this.currentItem;     // Vật phẩm có sẵn ở ô này

        // --- Xử lý vật phẩm được thả VÀO ô này ---
        if (itemComingIn != null)
        {
            // Nếu vật phẩm đến từ bên ngoài lưới chế tạo (túi đồ/hotbar)...
            if (!(sourceSlot is UI_CraftingSlot))
            {
                // ...thì tạo một BẢN SAO độc lập cho nó để tránh lỗi tham chiếu.
                // Debug.Log($"[OnDrop] Tạo bản sao cho '{itemComingIn.data.itemName}' từ inventory.");
                this.SetItem(new InventoryItem(itemComingIn.data, itemComingIn.quantity));
            }
            else // Nếu vật phẩm chỉ di chuyển giữa các ô chế tạo, chỉ cần di chuyển tham chiếu.
            {
                this.SetItem(itemComingIn);
            }
        }
        else // Nếu kéo một ô trống vào, thì dọn dẹp ô này.
        {
            this.SetItem(null);
        }

        // --- Xử lý vật phẩm bị đẩy RA khỏi ô này ---
        if (sourceSlot is UI_CraftingSlot sourceCraftingSlot)
        {
            // Nếu nguồn cũng là một ô chế tạo, thì hoán đổi vật phẩm về cho nó.
            sourceCraftingSlot.SetItem(itemGoingOut);
        }
        else
        {
            // Nếu nguồn là túi đồ/hotbar, cập nhật nó thông qua InventoryManager.
            InventoryManager.instance.SetItem(sourceSlot.slotType, sourceSlot.slotIndex, itemGoingOut);
        }
        
        CleanUpDrag();
    }

    // Một phương thức mới để cập nhật slot mà không cần kéo/thả (ví dụ: khi load game)
    public void SetItem(InventoryItem item)
    {
        UpdateSlot(item);
        OnSlotChanged?.Invoke(this);
    }

    // Phương thức này sẽ được gọi khi người chơi nhấp chuột vào slot
    public void OnPointerDown(PointerEventData eventData)
    {
        // Chúng ta chỉ quan tâm đến chuột trái, trên ô kết quả, và khi có vật phẩm
        if (eventData.button == PointerEventData.InputButton.Left && slotType == SlotType.Result && currentItem != null)
        {
            Debug.Log("<color=purple>[OnPointerDown]</color> Đã nhấp vào ô kết quả!");
            
            // Tìm đến cửa sổ chế tạo cha và gọi hàm để thực hiện chế tạo
            GetComponentInParent<UI_CraftingWindow>()?.OnResultSlotClicked();
        }
    }
}
