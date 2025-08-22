using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


[System.Serializable]
public class ResourceSpawnChance
{
    public ResourceNodeData resourceData;
    [Tooltip("Higher weight means more likely to spawn")]
    public float spawnWeight;
}

[CreateAssetMenu(fileName = "NewBiomeData", menuName = "Survival Day/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Biome Info")]
    public string biomeName;
    
    [Tooltip("The types of tiles this biome can spawn resources on.")]
    public List<TileBase> compatibleTiles;
    
    [Tooltip("List of resources that can spawn in this biome and their weights.")]
    public List<ResourceSpawnChance> resourceSpawns;
}
