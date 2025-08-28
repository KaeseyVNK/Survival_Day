using UnityEngine;

public enum ToolType { Axe, Pickaxe, Shovel, Hoe, Sword }

[CreateAssetMenu(fileName = "NewToolItemData", menuName = "Survival Day/Item Data/Tool")]
public class ToolItemData : ItemData
{
    [Header("Tool Specific")]
    public ToolType toolType;
    public float durability;
    public float damage;
}
