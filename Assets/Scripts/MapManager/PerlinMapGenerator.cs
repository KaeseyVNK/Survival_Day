using UnityEngine;
using UnityEngine.Tilemaps;
using static MapUtilities;
using System.Collections.Generic; // Added for List

public class PerlinMapGenerator : MonoBehaviour
{
    [Header("Grid/Tilemap")]
    public Tilemap tilemap;
    public Tilemap waterTilemap;

    [Header("Size")]
    public int width = 128;
    public int height = 128;

    [Header("Noise")]
    public float scale = 25f;
    public int seed = 12345;
    public Vector2 worldOffset;
    public int octaves = 3;
    [Range(0f,1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Tiles (Assign RuleTiles here)")]
    public TileBase waterTile;
    public TileBase sandTile;
    public TileBase grassTile;
    public TileBase mountainTile;

    [Header("Thresholds (0..1)")]
    [Range(0,1)] public float waterLevel = 0.35f;
    [Range(0,1)] public float sandLevel  = 0.45f;
    [Range(0,1)] public float grassLevel = 0.75f;
    
    [Header("Smoothing Settings")]
    public BiomeSmoothingSettings smoothingSettings;

    // These are no longer needed with the new system
    // [Header("Tree Settings")]
    // public TreeSettings treeSettings;
    //
    // [Header("Resource Settings")]
    // public ResourceSettings resourceSettings;

    [Header("System References")]
    [SerializeField] private ResourceSpawner resourceSpawner; // Reference to the new spawner
    [SerializeField] private Transform playerTransform; // Reference to the player

    // Internal data
    private Biome[,] biomeMap;
    private float[,] heightMap;
    private System.Random prng;
    
    // Processors
    private BiomeProcessor biomeProcessor;
    // private ResourceSpawner resourceSpawner; // Old internal variable, removed

    void Start()
    {
        prng = new System.Random(seed);
        biomeProcessor = new BiomeProcessor(width, height);
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        Generate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Generate();
        }
    }

    // private void OnDrawGizmos()
    // {
    //     if (biomeProcessor == null || biomeProcessor.debug_pixelsToFill == null) return;

    //     Gizmos.color = new Color(1, 0, 0, 0.5f); // Màu đỏ, hơi trong suốt

    //     foreach (var pixel in biomeProcessor.debug_pixelsToFill)
    //     {
    //         Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(pixel.x - width / 2, pixel.y - height / 2, 0));
    //         Gizmos.DrawCube(worldPos + new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0), tilemap.cellSize);
    //     }
    // }

    void Generate()
    {
        // Bước 1: Xóa dữ liệu cũ
        tilemap.ClearAllTiles();
        waterTilemap.ClearAllTiles();
        biomeMap = new Biome[width, height];
        heightMap = new float[width, height];

        // Bước 2: Tạo noise và phân loại biome ban đầu
        GenerateInitialBiomes();

        // Bước 3: Xử lý và làm mượt biomes
        biomeProcessor.ProcessBiomes(biomeMap, smoothingSettings);

        // Bước 4: Đặt tiles lên tilemap
        PlaceTiles();

        // Bước 5: Spawn resources using the new system
        if (resourceSpawner != null)
        {
            resourceSpawner.SpawnResources();
        }
        else
        {
            Debug.LogWarning("ResourceSpawner reference is not set in PerlinMapGenerator. Resources will not be spawned.");
        }

        // Bước 6: Position the player in a safe spot
        PositionPlayerAtSafeLocation();

        // Bước 7: Optimize tilemap
        tilemap.CompressBounds();
    }

    private void PositionPlayerAtSafeLocation()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform reference is not set in PerlinMapGenerator. Player will not be positioned.");
            return;
        }

        List<Vector2Int> safeLocations = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Consider Grass and Sand as safe spawn points
                if (biomeMap[x, y] == Biome.Grass || biomeMap[x, y] == Biome.Sand)
                {
                    safeLocations.Add(new Vector2Int(x, y));
                }
            }
        }

        if (safeLocations.Count > 0)
        {
            // Pick a random safe location
            Vector2Int spawnCell = safeLocations[Random.Range(0, safeLocations.Count)];
            Vector3 spawnPosition = tilemap.GetCellCenterWorld((Vector3Int)spawnCell);
            playerTransform.position = spawnPosition;
            Debug.Log($"Player spawned at safe location: {spawnCell}");
        }
        else
        {
            Debug.LogError("Could not find any safe location to spawn the player! The map might be all water/mountains.");
            // As a fallback, spawn at the center
            playerTransform.position = tilemap.GetCellCenterWorld(new Vector3Int(width / 2, height / 2, 0));
        }
    }

    private void GenerateInitialBiomes()
    {
        float offX = (float) prng.Next(-100000, 100000);
        float offY = (float) prng.Next(-100000, 100000);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x + worldOffset.x + offX) / scale;
                float ny = (y + worldOffset.y + offY) / scale;

                float h = MapUtilities.FractalPerlin(nx, ny, octaves, persistence, lacunarity);
                h -= MapUtilities.IslandFalloff(x, y, width, height, 2.2f);
                heightMap[x, y] = h;

                if (h < waterLevel) biomeMap[x, y] = Biome.Water;
                else if (h < sandLevel) biomeMap[x, y] = Biome.Sand;
                else if (h < grassLevel) biomeMap[x, y] = Biome.Grass;
                else biomeMap[x, y] = Biome.Mountain;
            }
        }
    }

    private void PlaceTiles()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Biome biome = biomeMap[x, y];
                TileBase tileToSet = GetTileForBiome(biome);

                if (biome == Biome.Water)
                {
                    waterTilemap.SetTile((Vector3Int)cell, tileToSet);
                }
                else
                {
                    tilemap.SetTile((Vector3Int)cell, tileToSet);
                }
            }
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

    // Public method để regenerate từ editor hoặc runtime
    [ContextMenu("Regenerate Map")]
    public void RegenerateMap()
    {
        prng = new System.Random(seed);
        Generate();
    }
}
