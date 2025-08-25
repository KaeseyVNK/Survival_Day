using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class Chunk
{
    public ChunkData data;
    public GameObject resourceHolder;

    public Chunk(ChunkData chunkData)
    {
        data = chunkData;
        resourceHolder = new GameObject($"Resources [{data.chunkCoord.x}, {data.chunkCoord.y}]");
    }
}

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    public int chunkSize = 16;
    [Range(1, 10)]
    public int renderDistance = 2;

    [Header("References")]
    public Transform playerTransform;
    public PerlinMapGenerator mapGenerator;
    public ResourceSpawner resourceSpawner;

    private Vector2Int currentPlayerChunk;
    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    private HashSet<Vector2Int> chunksInProgress = new HashSet<Vector2Int>();

    void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (resourceSpawner != null)
        {
            resourceSpawner.transform.SetParent(this.transform);
        }
        
        UpdateChunks();
    }

    void Update()
    {
        Vector2Int playerChunkCoord = GetChunkCoordinateFromPosition(playerTransform.position);

        if (playerChunkCoord != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunkCoord;
            UpdateChunks();
        }
    }

    private void UpdateChunks()
    {
        List<Vector2Int> chunksToUnload = activeChunks.Keys
            .Where(c => Vector2Int.Distance(c, currentPlayerChunk) > renderDistance)
            .ToList();

        foreach (var chunkCoord in chunksToUnload)
        {
            UnloadChunk(chunkCoord);
        }
        
        for (int y = -renderDistance; y <= renderDistance; y++)
        {
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                Vector2Int chunkCoord = new Vector2Int(x, y) + currentPlayerChunk;
                
                if (!activeChunks.ContainsKey(chunkCoord))
                {
                    LoadChunk(chunkCoord);
                }
            }
        }
    }

    private void LoadChunk(Vector2Int chunkCoord)
    {
        if (chunksInProgress.Contains(chunkCoord)) return;
        StartCoroutine(LoadChunkProcess(chunkCoord));
    }

    private IEnumerator LoadChunkProcess(Vector2Int chunkCoord)
    {
        chunksInProgress.Add(chunkCoord);

        ChunkData chunkData = new ChunkData();
        yield return StartCoroutine(mapGenerator.GenerateChunkDataCoroutine(chunkCoord, chunkSize, result => chunkData = result));

        if (Vector2Int.Distance(chunkCoord, currentPlayerChunk) > renderDistance)
        {
            chunksInProgress.Remove(chunkCoord);
            yield break;
        }

        yield return StartCoroutine(mapGenerator.RenderChunkOnTilemapCoroutine(chunkData, chunkSize));

        Chunk newChunk = new Chunk(chunkData);
        newChunk.resourceHolder.transform.SetParent(this.transform);
        activeChunks.Add(chunkCoord, newChunk);

        if (resourceSpawner != null)
        {
            yield return StartCoroutine(resourceSpawner.SpawnResourcesForChunkCoroutine(chunkData, newChunk.resourceHolder.transform));
        }

        chunksInProgress.Remove(chunkCoord);
        
        RefreshNeighborChunks(chunkCoord);
    }

    private void UnloadChunk(Vector2Int chunkCoord)
    {
        if (chunksInProgress.Contains(chunkCoord) || !activeChunks.ContainsKey(chunkCoord)) return;
        StartCoroutine(UnloadChunkProcess(chunkCoord));
    }

    private IEnumerator UnloadChunkProcess(Vector2Int chunkCoord)
    {
        chunksInProgress.Add(chunkCoord);

        Chunk chunkToUnload = activeChunks[chunkCoord];
        Destroy(chunkToUnload.resourceHolder);

        activeChunks.Remove(chunkCoord);
        yield return StartCoroutine(mapGenerator.ClearChunkOnTilemapCoroutine(chunkCoord, chunkSize));
        chunksInProgress.Remove(chunkCoord);
        
        RefreshNeighborChunks(chunkCoord);
    }

    private void RefreshNeighborChunks(Vector2Int chunkCoord)
    {
        Vector2Int[] neighborOffsets = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var offset in neighborOffsets)
        {
            Vector2Int neighborCoord = chunkCoord + offset;
            if (activeChunks.ContainsKey(neighborCoord))
            {
                StartCoroutine(mapGenerator.RenderChunkOnTilemapCoroutine(activeChunks[neighborCoord].data, chunkSize));
            }
        }
    }

    private Vector2Int GetChunkCoordinateFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / mapGenerator.tilemap.cellSize.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / mapGenerator.tilemap.cellSize.y / chunkSize);
        return new Vector2Int(x, y);
    }
}
