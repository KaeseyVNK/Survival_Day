using UnityEngine;

// Đổi tên menu để phù hợp với hệ thống mới và đặt nó làm lớp cha
[CreateAssetMenu(fileName = "NewItemData", menuName = "Survival Day/Item Data/Item")]
public abstract class ItemData : ScriptableObject // Chuyển thành abstract
{
    [Header("Info")]
    public string id; // Đổi thành string để linh hoạt hơn, ví dụ: "item_wood", "tool_axe"
    public string itemName;
    public Sprite sprite;
}
