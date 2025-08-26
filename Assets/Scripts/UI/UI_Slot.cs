using TMPro;
using UnityEngine;
using UnityEngine.EventSystems; // Thêm thư viện Event Systems
using UnityEngine.UI;

// Định nghĩa SlotType đã được chuyển sang file InventoryManager.cs để tránh trùng lặp

public class UI_Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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
        if (item != null)
        {
            // Luôn reset màu sắc về trạng thái ban đầu khi cập nhật slot
            itemIcon.color = Color.white;
            quantityText.color = new Color(quantityText.color.r, quantityText.color.g, quantityText.color.b, 1f);

            itemIcon.gameObject.SetActive(true);
            quantityText.gameObject.SetActive(true);

            itemIcon.sprite = item.data.sprite;
            quantityText.text = item.quantity.ToString();
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // OnEndDrag được gọi sau OnDrop.
        // Nếu draggableInstance vẫn còn tồn tại ở đây, có nghĩa là người dùng đã thả item ra ngoài một slot hợp lệ.
        if (draggableInstance != null)
        {
            // Phục hồi lại trạng thái của slot gốc
            if(sourceSlot != null)
            {
                sourceSlot.UpdateSlot(sourceSlot.currentItem);
            }
            CleanUpDrag();
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
}
