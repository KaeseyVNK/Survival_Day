using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceItemData", menuName = "Survival Day/Item Data/Resource")]
public class ResourceItemData : ItemData
{
    [Header("Resource Specific")]
    [Tooltip("0 = none, 1 = axe, 2 = pickaxe, 3 = shovel")]
    public int requiredTool; 
}
