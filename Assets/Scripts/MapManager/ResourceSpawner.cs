using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


public class ResourceSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private GameObject resourceHolder;
    [SerializeField] private GameObject resourceNodePrefab; // The base prefab for any resource
    
    [Header("Biome Configuration")]
    [SerializeField] private List<BiomeData> biomes;

    [Header("Spawn Settings")]
    [Range(0, 1)]
    [SerializeField] private float globalSpawnChance = 0.1f; // 10% chance for any tile to attempt spawning

    // We remove the Start() method to prevent automatic spawning.
    // public void Start()
    // {
    //     SpawnResources();
    // }

    public void SpawnResources()
    {
        // First, clear any previously spawned resources
        if (resourceHolder != null)
        {
            for (int i = resourceHolder.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(resourceHolder.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            resourceHolder = new GameObject("Spawned Resources");
        }
        
        BoundsInt bounds = groundTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                TileBase tile = groundTilemap.GetTile(new Vector3Int(x, y, 0));
                
                if (tile != null)
                {
                    // Roll the dice to see if we should spawn anything on this tile
                    if (Random.value > globalSpawnChance)
                    {
                        continue; // Unlucky, try next tile
                    }
                    
                    // Find the correct biome for this tile
                    BiomeData currentBiome = FindBiomeForTile(tile);
                    if (currentBiome != null)
                    {
                        // Try to spawn a resource based on the biome's rules
                        TrySpawnResource(currentBiome, new Vector3Int(x, y, 0));
                    }
                }
            }
        }
    }

    private BiomeData FindBiomeForTile(TileBase tile)
    {
        return biomes.FirstOrDefault(biome => biome.compatibleTiles.Contains(tile));
    }

    private void TrySpawnResource(BiomeData biome, Vector3Int position)
    {
        float totalWeight = biome.resourceSpawns.Sum(rs => rs.spawnWeight);
        float randomValue = Random.Range(0, totalWeight);

        float currentWeight = 0;
        foreach (var resourceSpawn in biome.resourceSpawns)
        {
            currentWeight += resourceSpawn.spawnWeight;
            if (randomValue <= currentWeight)
            {
                // Found the resource to spawn
                Vector3 spawnPos = groundTilemap.GetCellCenterWorld(position);
                
                // Instantiate from the base prefab
                GameObject newNodeObject = Instantiate(resourceNodePrefab, spawnPos, Quaternion.identity, resourceHolder.transform);
                
                // Get the ResourceNode component and initialize it with the correct data
                ResourceNode node = newNodeObject.GetComponent<ResourceNode>();
                if (node != null)
                {
                    node.Initialize(resourceSpawn.resourceData);
                }
                else
                {
                    Debug.LogError("resourceNodePrefab does not have a ResourceNode component!", resourceNodePrefab);
                }

                return; // Stop after spawning one resource on this tile
            }
        }
    }
}

