using UnityEngine;
using UnityEngine.UI;
using TMPro; // Thêm thư viện TextMeshPro

// Script này sẽ được gắn vào một đối tượng UI (một Image) được tạo ra khi bắt đầu kéo một vật phẩm.
// Nó sẽ hiển thị hình ảnh của vật phẩm và đi theo con trỏ chuột.
public class DraggableItem : MonoBehaviour
{
    // Loại bỏ toàn bộ phần static instance
    // public static DraggableItem instance;

    // Thành phần Image để hiển thị sprite của vật phẩm.
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI quantityText; // Thêm tham chiếu đến Text
    // Transform của parent ban đầu, dùng để quay về nếu việc kéo thả bị hủy.
    // private Transform parentAfterDrag; // Không cần thiết nữa
    public UI_Slot sourceSlot { get; private set; } // Slot gốc nơi vật phẩm được kéo đi

    private void Awake()
    {
        // Thiết lập tham chiếu tĩnh và lấy thành phần Image.
        // instance = this; // Loại bỏ
        image = GetComponent<Image>();
        // Tự động tìm TextMeshProUGUI con nếu chưa được gán
        if (quantityText == null)
        {
            quantityText = GetComponentInChildren<TextMeshProUGUI>();
        }
        // Ban đầu, tắt raycast để nó không cản trở việc phát hiện các ô slot bên dưới.
        image.raycastTarget = false;
        if(quantityText != null)
        {
            quantityText.raycastTarget = false;
        }
    }

    void Update()
    {
        // Cập nhật vị trí của hình ảnh theo vị trí của con trỏ chuột trong mỗi frame.
        transform.position = Input.mousePosition;
    }

    // Bắt đầu quá trình kéo
    public void StartDrag(InventoryItem item, UI_Slot fromSlot)
    {
        sourceSlot = fromSlot;
        // Hiện hình ảnh và đặt sprite cho nó.
        image.enabled = true;
        image.sprite = item.data.sprite;
        UpdateQuantity(item.quantity);
        // Parent sẽ được quản lý bởi UI_Slot khi khởi tạo
    }

    /// <summary>
    /// Cập nhật số lượng hiển thị trên icon đang kéo
    /// </summary>
    public void UpdateQuantity(int quantity)
    {
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = quantity.ToString();
            }
            else
            {
                // Ẩn số lượng nếu chỉ còn 1
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    // Kết thúc quá trình kéo
    public void EndDrag()
    {
        // Việc hủy đối tượng sẽ được UI_Slot quản lý
        Destroy(gameObject);
    }
}
