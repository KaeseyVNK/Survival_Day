using UnityEngine;
using UnityEngine.EventSystems;

// Lớp này gần giống với UI_Slot nhưng được chuyên biệt hóa cho việc chế tạo.
// Nó cần thông báo cho cửa sổ chế tạo chính mỗi khi nội dung của nó thay đổi.
// Thêm IPointerDownHandler để có thể bắt sự kiện click chuột
public class UI_CraftingSlot : UI_Slot, IPointerDownHandler, IPointerClickHandler
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
        if (sourceSlot == null || sourceSlot == this)
        {
            return;
        }

        UI_Slot originalSourceSlot = sourceSlot; // Giữ lại tham chiếu đến ô gốc

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

        // --- SỬA BUG ---
        // Tương tự như UI_Slot, gọi UpdateSlot một lần cuối cho ô gốc để khôi phục lại giao diện
        originalSourceSlot.UpdateSlot(originalSourceSlot.currentItem);
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

    // Ghi đè phương thức OnPointerClick để xử lý logic chia stack cho lưới chế tạo
    public override void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ hoạt động khi đang kéo một vật phẩm và người dùng nhấn chuột phải
        if (draggableInstance == null || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        InventoryItem sourceItem = sourceSlot.currentItem;
        
        // Không thể chia stack nếu chỉ còn 1 item
        if (sourceItem.quantity <= 1) return;

        // Trường hợp 1: Ô crafting này trống
        if (this.currentItem == null)
        {
            // Tạo một BẢN SAO MỚI với số lượng là 1 và đặt vào ô này
            SetItem(new InventoryItem(sourceItem.data, 1));
        }
        // Trường hợp 2: Ô crafting này có cùng loại item
        else if (this.currentItem.data.id == sourceItem.data.id)
        {
            // Tăng số lượng của item hiện tại lên 1
            this.currentItem.quantity++;
            // Cập nhật lại slot (quan trọng để kích hoạt OnSlotChanged)
            SetItem(this.currentItem);
        }
        else
        {
            // Nếu ô đích có item khác loại, không làm gì cả
            return;
        }

        // --- Cập nhật lại ô nguồn ---
        sourceItem.quantity--; // Giảm số lượng

        // Nếu nguồn cũng là một ô crafting
        if (sourceSlot is UI_CraftingSlot sourceCraftingSlot)
        {
            sourceCraftingSlot.SetItem(sourceItem);
        }
        // Nếu nguồn là từ inventory/hotbar
        else
        {
            InventoryManager.instance.SetItem(sourceSlot.slotType, sourceSlot.slotIndex, sourceItem);
        }
        
        // Cập nhật lại số lượng trên icon đang kéo
        draggableInstance.GetComponent<DraggableItem>()?.UpdateQuantity(sourceItem.quantity);

        // Nếu stack nguồn đã hết, kết thúc kéo thả
        if (sourceItem.quantity <= 0)
        {
            InventoryManager.instance.SetItem(sourceSlot.slotType, sourceSlot.slotIndex, null);
            CleanUpDrag();
        }
    }
}
