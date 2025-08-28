using System.Linq;
using UnityEngine;

public class Player_Combat : Enity_Combat
{
    private InventoryManager inventoryManager;
    [SerializeField] private Player player;

    private void Start()
    {
        inventoryManager = InventoryManager.instance;

        player = GetComponent<Player>();

        if (inventoryManager == null)
        {
            Debug.LogError("Player_Combat requires an InventoryManager instance in the scene.", this);
        }
    }

    protected override void PerformAttack()
    {
        foreach (var target in GetDetectedColliders(whatIsAttackable))
        {
            ResourceNode resourceNode = target.GetComponent<ResourceNode>();
            if (resourceNode != null)
            {
                HandleResourceAttack(resourceNode);
                continue; // Chuyển sang mục tiêu tiếp theo sau khi xử lý
            }

            // Trong tương lai, có thể thêm logic tấn công kẻ thù ở đây
            IDamageable enemy = target.GetComponent<IDamageable>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void HandleResourceAttack(ResourceNode resourceNode)
    {
        // 1. Lấy item đang được chọn trên hotbar
        InventoryItem selectedItem = inventoryManager.hotbarItems[inventoryManager.selectedSlot];

        // 2. Lấy dữ liệu của tài nguyên
        ResourceItemData requiredResourceData = resourceNode.Data.itemToDrop;
        if (requiredResourceData == null) return;

        // 3. Kiểm tra xem tài nguyên có cần công cụ không
        // Giả sử requiredTool = 0 nghĩa là không cần công cụ
        if (requiredResourceData.requiredTool == 0)
        {
            resourceNode.TakeDamage(1); // Tay không thì gây 1 sát thương
            resourceNode.ChangeStatus();
            return;
        }

        // 4. Nếu tài nguyên CẦN công cụ, kiểm tra xem người chơi có đang cầm gì không
        if (selectedItem == null || !(selectedItem.data is ToolItemData))
        {
            FloatingTextManager.instance.Show("Cần công cụ!", player.transform.position + Vector3.up * 1.5f);
            return; // Người chơi không cầm công cụ
        }

        // 5. Kiểm tra xem công cụ có đúng loại không
        ToolItemData tool = selectedItem.data as ToolItemData;
        // Chuyển đổi từ ToolType enum (Axe=0, Pickaxe=1,...) sang int (1, 2,...) để so sánh
        int toolId = (int)tool.toolType + 1;

        if (toolId == requiredResourceData.requiredTool)
        {
            Debug.Log($"Đã dùng {tool.itemName} để khai thác {resourceNode.Data.resourceName}");
            resourceNode.TakeDamage(tool.damage); // Gây sát thương bằng sát thương của công cụ
            resourceNode.ChangeStatus(); // Kích hoạt hiệu ứng rung lắc
        }
        else
        {
            FloatingTextManager.instance.Show("Sai công cụ!", player.transform.position + Vector3.up * 0.5f);
        }
    }
}
