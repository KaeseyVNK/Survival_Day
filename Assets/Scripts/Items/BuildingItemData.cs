using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingItemData", menuName = "Survival Day/Item Data/Building")]
public class BuildingItemData : ItemData
{
    [Header("Building Specific")]
    public GameObject buildingPrefab;
}
