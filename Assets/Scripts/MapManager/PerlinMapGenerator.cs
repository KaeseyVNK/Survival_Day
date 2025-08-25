using UnityEngine;
using UnityEngine.Tilemaps;
using static MapUtilities;
using System.Collections.Generic;
using System.Collections; // Required for Coroutines

// A simple container for one chunk's generated data
public struct ChunkData
{
    public Biome[,] biomeMap;
    public Vector2Int chunkCoord;

    public ChunkData(Vector2Int coord, int chunkSize)
    {
        chunkCoord = coord;
        biomeMap = new Biome[chunkSize, chunkSize];
    }
}


public class PerlinMapGenerator : MonoBehaviour
{
    [Header("Grid/Tilemap")]
    public Tilemap tilemap; // This might become a prefab or be managed by ChunkManager
    public Tilemap waterTilemap;

    [Header("Size")]
    // These now define the conceptual size of the world, not an array size
    public int worldWidth = 1024;
    public int worldHeight = 1024;

    [Header("Noise")]
    public float scale = 25f;
    public int seed = 12345;
    public Vector2 worldOffset;
    public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Tiles (Assign RuleTiles here)")]
    public TileBase waterTile;
    public TileBase sandTile;
    public TileBase grassTile;
    public TileBase mountainTile;

    [Header("Thresholds (0..1)")]
    [Range(0, 1)] public float waterLevel = 0.35f;
    [Range(0, 1)] public float sandLevel = 0.45f;
    [Range(0, 1)] public float grassLevel = 0.75f;

    [Header("Smoothing Settings")]
    public BiomeSmoothingSettings smoothingSettings;

    // References are no longer needed here, ChunkManager will handle them
    // [Header("System References")]
    // [SerializeField] private ResourceSpawner resourceSpawner;
    // [SerializeField] private Transform playerTransform;

    // Internal data is no longer stored for the whole world
    private System.Random prng;
    private float noiseOffsetX;
    private float noiseOffsetY;
    private BiomeProcessor biomeProcessor; // Add a reference to the processor

    void Awake()
    {
        // Initialize the random number generator once to ensure consistent world generation
        prng = new System.Random(seed);
        noiseOffsetX = (float)prng.Next(-100000, 100000);
        noiseOffsetY = (float)prng.Next(-100000, 100000);
        biomeProcessor = new BiomeProcessor(); // Initialize the processor
    }

    // This is the COROUTINE version of the function
    public IEnumerator GenerateChunkDataCoroutine(Vector2Int chunkCoord, int chunkSize, System.Action<ChunkData> onComplete)
    {
        int bufferSize = 2; // A buffer of 2 is safer for more complex smoothing
        int bufferedChunkSize = chunkSize + bufferSize * 2;
        Biome[,] bufferedBiomeMap = new Biome[bufferedChunkSize, bufferedChunkSize];

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        for (int yOffset = 0; yOffset < bufferedChunkSize; yOffset++)
        {
            for (int xOffset = 0; xOffset < bufferedChunkSize; xOffset++)
            {
                int globalX = startX + xOffset - bufferSize;
                int globalY = startY + yOffset - bufferSize;

                float nx = (globalX + worldOffset.x + noiseOffsetX) / scale;
                float ny = (globalY + worldOffset.y + noiseOffsetY) / scale;

                float h = MapUtilities.FractalPerlin(nx, ny, octaves, persistence, lacunarity);
                h -= MapUtilities.IslandFalloff(globalX, globalY, worldWidth, worldHeight, 2.2f);
                
                if (h < waterLevel) bufferedBiomeMap[xOffset, yOffset] = Biome.Water;
                else if (h < sandLevel) bufferedBiomeMap[xOffset, yOffset] = Biome.Sand;
                else if (h < grassLevel) bufferedBiomeMap[xOffset, yOffset] = Biome.Grass;
                else bufferedBiomeMap[xOffset, yOffset] = Biome.Mountain;
            }
        }
        
        // Wait a frame after initial generation
        yield return null;

        // Process the buffered map
        biomeProcessor.ProcessBiomes(ref bufferedBiomeMap, smoothingSettings);

        // Wait a frame after processing
        yield return null;

        // Extract the clean data from the center of the buffered map
        ChunkData finalChunkData = new ChunkData(chunkCoord, chunkSize);
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                finalChunkData.biomeMap[x, y] = bufferedBiomeMap[x + bufferSize, y + bufferSize];
            }
        }
        
        onComplete(finalChunkData);
    }

    public IEnumerator RenderChunkOnTilemapCoroutine(ChunkData data, int chunkSize)
    {
        int startX = data.chunkCoord.x * chunkSize;
        int startY = data.chunkCoord.y * chunkSize;
        
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                Vector3Int cellPos = new Vector3Int(startX + x, startY + y, 0);
                Biome biome = data.biomeMap[x, y];
                TileBase tileToSet = GetTileForBiome(biome);

                if (biome == Biome.Water)
                {
                    waterTilemap.SetTile(cellPos, tileToSet);
                    tilemap.SetTile(cellPos, null);
                }
                else
                {
                    tilemap.SetTile(cellPos, tileToSet);
                    waterTilemap.SetTile(cellPos, null);
                }
            }
            // After each row is rendered, pause and wait for the next frame
            yield return null;
        }
    }

    public IEnumerator ClearChunkOnTilemapCoroutine(Vector2Int chunkCoord, int chunkSize)
    {
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;
        
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                Vector3Int cellPos = new Vector3Int(startX + x, startY + y, 0);
                tilemap.SetTile(cellPos, null);
                waterTilemap.SetTile(cellPos, null);
            }
            // After each row is cleared, pause and wait for the next frame
            yield return null;
        }
    }

    private TileBase GetTileForBiome(Biome biome)
    {
        switch (biome)
        {
            case Biome.Water: return waterTile;
            case Biome.Sand: return sandTile;
            case Biome.Grass: return grassTile;
            case Biome.Mountain: return mountainTile;
            default: return null;
        }
    }
}
