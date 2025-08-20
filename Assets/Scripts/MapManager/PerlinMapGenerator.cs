using UnityEngine;
using UnityEngine.Tilemaps;
using static MapUtilities;

public class PerlinMapGenerator : MonoBehaviour
{
    [Header("Grid/Tilemap")]
    public Tilemap tilemap;

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

    [Header("Tree Settings")]
    public TreeSettings treeSettings;

    [Header("Resource Settings")]
    public ResourceSettings resourceSettings;

    // Internal data
    private Biome[,] biomeMap;
    private float[,] heightMap;
    private System.Random prng;
    
    // Processors
    private BiomeProcessor biomeProcessor;
    private ResourceSpawner resourceSpawner;

    void Start()
    {
        prng = new System.Random(seed);
        biomeProcessor = new BiomeProcessor(width, height);
        resourceSpawner = new ResourceSpawner(width, height, tilemap);
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
        biomeMap = new Biome[width, height];
        heightMap = new float[width, height];

        // Bước 2: Tạo noise và phân loại biome ban đầu
        GenerateInitialBiomes();

        // Bước 3: Xử lý và làm mượt biomes
        biomeProcessor.ProcessBiomes(biomeMap, smoothingSettings);

        // Bước 4: Đặt tiles lên tilemap
        PlaceTiles();

        // Bước 5: Spawn resources và trees
        resourceSpawner.SpawnResources(biomeMap, heightMap, treeSettings, resourceSettings);

        // Bước 6: Optimize tilemap
        tilemap.CompressBounds();
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
                TileBase tileToSet = GetTileForBiome(biomeMap[x, y]);
                tilemap.SetTile((Vector3Int)cell, tileToSet);
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
