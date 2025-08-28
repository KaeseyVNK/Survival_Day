using UnityEngine;

public class Player_Interactor : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        DropItem item = other.GetComponent<DropItem>();
        if (item != null)
        {
            // Báo cho InventoryManager để thêm item vào túi đồ
            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.AddItem(item.itemData);
            }
            
            // Sau khi đã thêm vào data, gọi Pickup để item biến mất khỏi thế giới
            item.Pickup();
        }
    }
}
