using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Survival Day/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Crafting Pattern")]
    [Tooltip("Danh sách các item theo đúng thứ tự của lưới chế tạo. Để trống (None) cho ô không cần item.")]
    public List<ItemData> craftingPattern;

    [Header("Result")]
    public ItemData resultItem;
    [Min(1)]
    public int resultQuantity = 1;
}
