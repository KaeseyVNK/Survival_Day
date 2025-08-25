using UnityEngine;


[CreateAssetMenu(fileName = "NewDropItemData", menuName = "Survival Day/Drop Item Data")]
public class DropItemData : ScriptableObject
{
    [Header("Info")]
    public int id;
    
    public int requiredTool; // 0 = none, 1 = axe, 2 = pickaxe, 3 = shovel
    public string itemName;
    public Sprite sprite;

}
