using UnityEngine;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PerlinMapGenerator mapGenerator;
    [SerializeField] private ChunkManager chunkManager;

    [Header("Spawn Settings")]
    [SerializeField] private int searchRadiusInChunks = 10; // How far out to search for a safe spot

    private void Start()
    {
        // Auto-find references if they aren't assigned in the inspector
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (mapGenerator == null) mapGenerator = FindObjectOfType<PerlinMapGenerator>();
        if (chunkManager == null) chunkManager = FindObjectOfType<ChunkManager>();

        if (playerTransform == null || mapGenerator == null || chunkManager == null)
        {
            Debug.LogError("PlayerSpawner is missing critical references (Player, MapGenerator, or ChunkManager). Aborting spawn logic.");
            return;
        }

        StartCoroutine(FindSafeSpawnPointCoroutine());
    }

    private IEnumerator FindSafeSpawnPointCoroutine()
    {
        // Disable the ChunkManager so it doesn't start loading chunks around the initial (potentially unsafe) position
        chunkManager.enabled = false;
        Debug.Log("Searching for a safe spawn point...");

        // Spiraling search pattern to find land, starting from (0,0)
        int x = 0, y = 0, dx = 0, dy = -1;
        int maxSpiralSize = searchRadiusInChunks * 2 + 1;
        maxSpiralSize *= maxSpiralSize;

        for (int i = 0; i < maxSpiralSize; i++)
        {
            if (x >= -searchRadiusInChunks && x <= searchRadiusInChunks && y >= -searchRadiusInChunks && y <= searchRadiusInChunks)
            {
                Vector2Int chunkCoord = new Vector2Int(x, y);
                
                ChunkData chunkData = new ChunkData();
                // Generate this chunk's data in memory without rendering it yet
                yield return StartCoroutine(mapGenerator.GenerateChunkDataCoroutine(chunkCoord, chunkManager.chunkSize, result => chunkData = result));

                // Now search within this chunk's data for a safe cell (grass or sand)
                Vector3Int? safeCell = FindSafeCellInChunk(chunkData);

                if (safeCell.HasValue)
                {
                    // We found a safe spot!
                    Vector3 worldPos = mapGenerator.tilemap.GetCellCenterWorld(safeCell.Value);
                    playerTransform.position = worldPos;
                    Debug.Log($"<color=green>Safe spawn point found at {worldPos} in chunk {chunkCoord}. Moving player.</color>");

                    // Re-enable the ChunkManager. Its Update() loop will now take over and load chunks around the new safe position.
                    chunkManager.enabled = true;
                    yield break; // Exit the coroutine, our job is done.
                }
            }

            // This logic moves the search coordinates in an outward spiral
            if ((x == y) || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                (dx, dy) = (-dy, dx); // Swap and negate one to turn
            }
            x += dx;
            y += dy;
        }

        Debug.LogError($"Could not find a safe spawn point within a radius of {searchRadiusInChunks} chunks. Player will spawn at origin, which may be unsafe.");
        // If we failed, re-enable the chunk manager anyway to let the game proceed.
        chunkManager.enabled = true;
    }

    private Vector3Int? FindSafeCellInChunk(ChunkData chunkData)
    {
        int startX = chunkData.chunkCoord.x * chunkManager.chunkSize;
        int startY = chunkData.chunkCoord.y * chunkManager.chunkSize;

        // Iterate through all cells in the chunk data
        for (int y = 0; y < chunkManager.chunkSize; y++)
        {
            for (int x = 0; x < chunkManager.chunkSize; x++)
            {
                if (IsBiomeSafe(chunkData.biomeMap[x, y]))
                {
                    // Found a safe biome, return its global cell position
                    return new Vector3Int(startX + x, startY + y, 0);
                }
            }
        }
        return null; // No safe spot found in this chunk
    }

    private bool IsBiomeSafe(Biome biome)
    {
        // Define what constitutes a "safe" biome to spawn in
        return biome == Biome.Grass || biome == Biome.Sand;
    }
}
