using TMPro;
using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI;

public class UI_Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("Components")]
    [SerializeField] protected Image itemIcon;
    [SerializeField] protected TextMeshProUGUI quantityText;
    [SerializeField] protected GameObject selectionBorder;
    [SerializeField] private GameObject draggableItemPrefab; // Prefab cho vật phẩm kéo thả

    // --- Dữ liệu của Slot ---
    public InventoryItem currentItem { get; private set; }
    public SlotType slotType { get; set; }
    public int slotIndex { get; set; }

    // --- Biến static để theo dõi trạng thái kéo thả ---
    protected static UI_Slot sourceSlot; // Slot bắt đầu kéo
    protected static GameObject draggableInstance; // Đối tượng đang được kéo

    private void Awake()
    {
        ToggleSelection(false);

        if (itemIcon != null)
        {
            itemIcon.raycastTarget = false;
        }
        if (quantityText != null)
        {
            quantityText.raycastTarget = false;
        }
    }

    private void Start()
    {
    }

    public void ToggleSelection(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
        }
    }

    public void UpdateSlot(InventoryItem item)
    {
        currentItem = item; // Cập nhật item hiện tại của slot

        // --- SỬA BUG ---
        // Chỉ reset lại màu sắc nếu ô này KHÔNG PHẢI là ô gốc của một thao tác kéo thả đang diễn ra.
        // Điều này giữ cho hiệu ứng "mờ" không bị mất đi khi chia stack.
        if (sourceSlot != this)
        {
            if (item != null)
            {
                itemIcon.color = Color.white;
                quantityText.color = new Color(quantityText.color.r, quantityText.color.g, quantityText.color.b, 1f);
            }
        }
        // --- KẾT THÚC SỬA BUG ---

        if (item != null)
        {
            itemIcon.gameObject.SetActive(true);
            quantityText.gameObject.SetActive(true);

            itemIcon.sprite = item.data.sprite;
            // Chỉ hiển thị số lượng nếu > 1
            if (item.quantity > 1)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = item.quantity.ToString();
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            itemIcon.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
        }
    }

    // --- Giao diện Kéo và Thả ---

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null && draggableInstance == null) // Chỉ bắt đầu kéo nếu chưa có gì đang được kéo
        {
            // Debug.Log($"<color=green>[OnBeginDrag]</color> Bắt đầu kéo từ slot: <b>{gameObject.name}</b> | Item: <b>{currentItem.data.itemName}</b>");

            // Đánh dấu slot nguồn
            sourceSlot = this;

            // Tìm Canvas gốc để DraggableItem có thể hiển thị trên cùng
            Canvas topCanvas = GetComponentInParent<Canvas>();
            
            // Tạo instance của DraggableItem
            draggableInstance = Instantiate(draggableItemPrefab, topCanvas.transform);
            draggableInstance.transform.position = Input.mousePosition; // Đặt vị trí ngay tại con trỏ chuột
            
            // Lấy component DraggableItem và bắt đầu quá trình kéo
            DraggableItem draggableItem = draggableInstance.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                draggableItem.StartDrag(currentItem, this);
            }

            // Làm mờ item ở ô gốc để cho biết nó đang được kéo
            itemIcon.color = new Color(1f, 1f, 1f, 0.5f);
            quantityText.color = new Color(quantityText.color.r, quantityText.color.g, quantityText.color.b, 0.5f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // DraggableItem đã tự di chuyển theo chuột trong Update() của nó
    }

    // Phải là virtual để lớp con (UI_CraftingSlot) có thể override
    public virtual void OnDrop(PointerEventData eventData)
    {
        if (sourceSlot == null || sourceSlot == this || draggableInstance == null)
        {
            return; // Bỏ qua nếu không có gì để thả, hoặc thả vào chính nó
        }

        UI_Slot originalSourceSlot = sourceSlot; // Giữ lại tham chiếu đến ô gốc

        // --- SỬA LỖI CUỐI CÙNG: Xử lý trường hợp kéo-thả từ ô KẾT QUẢ ---
        var sourceAsCraftingSlot = sourceSlot as UI_CraftingSlot;
        if (sourceAsCraftingSlot != null && sourceAsCraftingSlot.slotType == SlotType.Result)
        {
            Debug.Log("<color=green><b>[OnDrop] Phát hiện lấy vật phẩm từ ô KẾT QUẢ. Kích hoạt chế tạo.</b></color>");
            
            // Tìm đến cửa sổ chế tạo và gọi hàm xử lý logic chế tạo chính xác.
            // Hàm này sẽ tự thêm vật phẩm vào inventory và trừ nguyên liệu.
            sourceAsCraftingSlot.GetComponentInParent<UI_CraftingWindow>()?.OnResultSlotClicked();

            // Dọn dẹp và kết thúc ngay tại đây.
            CleanUpDrag();
            return; 
        }
        // --- KẾT THÚC SỬA LỖI ---


        // --- Logic xử lý Drop mới ---
        var sourceCraftingSlot = sourceSlot as UI_CraftingSlot;

        if (sourceCraftingSlot != null)
        {
            // --- TRƯỜNG HỢP 1: Kéo từ ô CRAFTING sang INVENTORY/HOTBAR ---
            // Debug.Log($"<color=cyan>[OnDrop]</color> Nhận item từ ô crafting. Nguồn: <b>{sourceSlot.gameObject.name}</b>, Đích: <b>{gameObject.name}</b>");

            // Lấy item từ hai phía để hoán đổi
            InventoryItem itemFromCrafting = sourceCraftingSlot.currentItem;
            InventoryItem itemInThisInventorySlot = this.currentItem;

            // Đặt item từ ô crafting vào vị trí inventory này
            InventoryManager.instance.SetItem(this.slotType, this.slotIndex, itemFromCrafting);

            // Đặt item từ inventory (nếu có) ngược lại vào ô crafting
            sourceCraftingSlot.SetItem(itemInThisInventorySlot);
        }
        else
        {
            // --- TRƯỜNG HỢP 2: Kéo giữa các ô INVENTORY/HOTBAR (logic cũ) ---
            // Debug.Log($"<color=cyan>[OnDrop]</color> Di chuyển item trong inventory. Nguồn: <b>{sourceSlot.gameObject.name}</b>, Đích: <b>{gameObject.name}</b>");

            InventoryManager.instance.MoveItem(
                sourceSlot.slotType,
                sourceSlot.slotIndex,
                this.slotType,
                this.slotIndex
            );
        }

        // Dọn dẹp sau khi thả thành công
        CleanUpDrag();

        // --- SỬA BUG ---
        // Sau khi mọi thứ đã xong và sourceSlot đã bị xóa,
        // gọi UpdateSlot một lần cuối cho ô gốc để đảm bảo nó được khôi phục lại trạng thái bình thường.
        originalSourceSlot.UpdateSlot(originalSourceSlot.currentItem);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // OnEndDrag được gọi sau OnDrop.
        // Nếu draggableInstance vẫn còn tồn tại ở đây, có nghĩa là người dùng đã thả item ra ngoài một slot hợp lệ.
        if (draggableInstance != null)
        {
            UI_Slot originalSourceSlot = sourceSlot; // Giữ lại tham chiếu đến ô gốc
            
            // Dọn dẹp TRƯỚC
            CleanUpDrag();

            // --- SỬA BUG ---
            // Sau khi dọn dẹp (sourceSlot đã là null), gọi UpdateSlot để khôi phục lại trạng thái cho ô gốc
            if(originalSourceSlot != null)
            {
                originalSourceSlot.UpdateSlot(originalSourceSlot.currentItem);
            }
        }
    }

    /// <summary>
    /// Dọn dẹp các biến static và hủy đối tượng kéo thả.
    /// </summary>
    protected void CleanUpDrag()
    {
        if (draggableInstance != null)
        {
            Destroy(draggableInstance);
        }
        sourceSlot = null;
        draggableInstance = null;
    }

    /// <summary>
    /// Xử lý sự kiện click chuột, đặc biệt là chuột phải để chia stack.
    /// </summary>
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ hoạt động khi đang kéo một vật phẩm và người dùng nhấn chuột phải
        if (draggableInstance == null || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        // --- BẮT ĐẦU LOGIC CHIA STACK ---
        Debug.Log($"<color=yellow>[Right Click Drop]</color> vào slot: <b>{gameObject.name}</b>");

        InventoryItem sourceItem = sourceSlot.currentItem;
        InventoryItem destinationItem = this.currentItem;

        // Không thể chia stack nếu chỉ còn 1 item
        if (sourceItem.quantity <= 1) return;

        // Trường hợp 1: Ô đích trống
        if (destinationItem == null)
        {
            // Giảm 1 ở gốc
            sourceItem.quantity--;
            // Tạo 1 item mới ở đích
            InventoryManager.instance.SetItem(this.slotType, this.slotIndex, new InventoryItem(sourceItem.data, 1));
        }
        // Trường hợp 2: Ô đích có cùng loại item
        else if (destinationItem.data.id == sourceItem.data.id)
        {
            // (Trong tương lai có thể kiểm tra giới hạn stack của ô đích)
            // Giảm 1 ở gốc
            sourceItem.quantity--;
            // Tăng 1 ở đích
            destinationItem.quantity++;
            InventoryManager.instance.SetItem(this.slotType, this.slotIndex, destinationItem);
        }
        else
        {
            // Nếu ô đích có item khác loại, không làm gì cả
            return;
        }

        // Cập nhật lại data ở ô gốc
        InventoryManager.instance.SetItem(sourceSlot.slotType, sourceSlot.slotIndex, sourceItem);

        // Cập nhật lại số lượng trên icon đang kéo
        draggableInstance.GetComponent<DraggableItem>()?.UpdateQuantity(sourceItem.quantity);
    }
}
