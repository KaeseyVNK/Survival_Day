// File: Assets/Scripts/Resources/ResourceNodeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Survival Day/Resource Node Data")]
public class ResourceNodeData : ScriptableObject 
{
    [Header("Info")]
    public string resourceName;
    public Sprite sprite;
    public float maxHealth;
    public bool isTriggerable;

    [Header("Drop")]
    public DropItemData itemToDrop;
    public int minDropAmount = 1;
    public int maxDropAmount = 3;
}