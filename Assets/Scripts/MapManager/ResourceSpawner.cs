using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class ResourceSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private GameObject resourceNodePrefab;
    [SerializeField] private PerlinMapGenerator mapGenerator; // Added for world seed access

    [Header("Biome Configuration")]
    [SerializeField] private List<BiomeData> biomes;

    [Header("Spawn Settings")]
    [Range(0, 1)]
    [SerializeField] private float globalSpawnChance = 0.1f;

    // This is the new main function to be called by ChunkManager
    public IEnumerator SpawnResourcesForChunkCoroutine(ChunkData data, Transform parent)
    {
        // Create a deterministic random number generator based on world seed and chunk coordinates
        int chunkSeed = mapGenerator.seed + data.chunkCoord.GetHashCode();
        System.Random prng = new System.Random(chunkSeed);

        int chunkSize = data.biomeMap.GetLength(0);
        int startX = data.chunkCoord.x * chunkSize;
        int startY = data.chunkCoord.y * chunkSize;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                // Use the deterministic random generator
                if (prng.NextDouble() > globalSpawnChance)
                {
                    continue;
                }
                
                Biome biomeEnum = data.biomeMap[x, y];
                BiomeData currentBiomeData = FindBiomeData(biomeEnum);

                if (currentBiomeData != null)
                {
                    Vector3Int cellPos = new Vector3Int(startX + x, startY + y, 0);
                    // Pass the seeded generator and chunk coordinates to the spawn method
                    TrySpawnResource(currentBiomeData, cellPos, parent, prng, data.chunkCoord);
                }
            }
            yield return null; // Pause after each row
        }
    }

    private BiomeData FindBiomeData(Biome biomeEnum)
    {
        return biomes.FirstOrDefault(b => b.biomeType == biomeEnum);
    }

    private void TrySpawnResource(BiomeData biome, Vector3Int position, Transform parent, System.Random prng, Vector2Int chunkCoord)
    {
        // 1. Generate a unique, deterministic ID for this potential resource
        string resourceId = $"res_{chunkCoord.x}_{chunkCoord.y}_{position.x}_{position.y}";

        // 2. Check with the WorldStateManager if this resource has already been destroyed
        if (WorldStateManager.instance != null && WorldStateManager.instance.IsResourceDestroyed(resourceId))
        {
            return; // Don't spawn this resource
        }

        if (biome.resourceSpawns == null || biome.resourceSpawns.Count == 0) return;

        float totalWeight = biome.resourceSpawns.Sum(rs => rs.spawnWeight);
        if (totalWeight <= 0) return;

        // Use the deterministic random generator
        float randomValue = (float)(prng.NextDouble() * totalWeight);

        float currentWeight = 0;
        foreach (var resourceSpawn in biome.resourceSpawns)
        {
            currentWeight += resourceSpawn.spawnWeight;
            if (randomValue <= currentWeight)
            {
                Vector3 spawnPos = groundTilemap.GetCellCenterWorld(position);
                GameObject newNodeObject = Instantiate(resourceNodePrefab, spawnPos, Quaternion.identity, parent);
                
                ResourceNode node = newNodeObject.GetComponent<ResourceNode>();
                if (node != null)
                {
                    // 3. Assign the unique ID and initialize the node
                    node.uniqueId = resourceId;
                    node.Initialize(resourceSpawn.resourceData);
                }
                else
                {
                    Debug.LogError("resourceNodePrefab does not have a ResourceNode component!", resourceNodePrefab);
                }
                return;
            }
        }
    }
}

