using UnityEngine;
using UnityEngine.Tilemaps;
using static MapUtilities;

[System.Serializable]
public class TreeSettings
{
    public GameObject treePrefab;
    [Range(0,1)] public float treeProb = 0.08f;
    public float treeBiasHeightMin = 0.45f;
    public float treeBiasHeightMax = 0.80f;
}

[System.Serializable]
public class ResourceSettings
{
    public Transform resourceParent;
    
    [Header("Grass Resources")]
    public GameObject[] grassResourcePrefabs;
    [Range(0, 1)] public float grassResourceSpawnChance = 0.1f;

    [Header("Sand Resources")]
    public GameObject[] sandResourcePrefabs;
    [Range(0, 1)] public float sandResourceSpawnChance = 0.05f;
}

public class ResourceSpawner
{
    private int width, height;
    private Biome[,] biomeMap;
    private float[,] heightMap;
    private Tilemap tilemap;

    public ResourceSpawner(int mapWidth, int mapHeight, Tilemap tileMap)
    {
        width = mapWidth;
        height = mapHeight;
        tilemap = tileMap;
    }

    public void SpawnResources(Biome[,] inputBiomeMap, float[,] inputHeightMap, TreeSettings treeSettings, ResourceSettings resourceSettings)
    {
        biomeMap = inputBiomeMap;
        heightMap = inputHeightMap;

        ClearResources(resourceSettings.resourceParent);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                // Spawn trees
                if (biomeMap[x, y] == Biome.Grass && treeSettings.treePrefab != null)
                {
                    SpawnTree(x, y, cell, treeSettings, resourceSettings.resourceParent);
                }

                // Spawn other resources (với buffer để tránh spawn ở biên)
                if (x >= 3 && x < width - 3 && y >= 3 && y < height - 3)
                {
                    if (biomeMap[x, y] == Biome.Grass)
                    {
                        SpawnGrassResources(x, y, resourceSettings);
                    }
                    else if (biomeMap[x, y] == Biome.Sand)
                    {
                        SpawnSandResources(x, y, resourceSettings);
                    }
                }
            }
        }
    }

    private void SpawnTree(int x, int y, Vector2Int cell, TreeSettings settings, Transform parent)
    {
        float h = heightMap[x, y];
        if (h > settings.treeBiasHeightMin && h < settings.treeBiasHeightMax)
        {
            float plantNoise = Mathf.PerlinNoise((x + 1000.123f) * 0.37f, (y - 999.321f) * 0.41f);
            if (plantNoise < settings.treeProb)
            {
                Vector3 worldPos = tilemap.CellToWorld((Vector3Int)cell) + new Vector3(0.5f, 0.5f, 0f);
                Object.Instantiate(settings.treePrefab, worldPos, Quaternion.identity, parent);
            }
        }
    }

    private void SpawnGrassResources(int x, int y, ResourceSettings settings)
    {
        // Kiểm tra vùng cỏ phải đủ lớn (ít nhất 80% vùng 5x5 là cỏ)
        if (MapUtilities.IsLargeArea(biomeMap, x, y, Biome.Grass, 2, 0.8f, width, height) &&
            !MapUtilities.IsNearBiome(biomeMap, x, y, Biome.Water, 4, width, height) && 
            !MapUtilities.IsNearBiome(biomeMap, x, y, Biome.Sand, 4, width, height))
        {
            if (settings.grassResourcePrefabs.Length > 0 && Random.Range(0f, 1f) < settings.grassResourceSpawnChance)
                SpawnResource(settings.grassResourcePrefabs, x, y, settings.resourceParent);
        }
    }

    private void SpawnSandResources(int x, int y, ResourceSettings settings)
    {
        // Kiểm tra vùng cát phải đủ lớn (ít nhất 70% vùng 7x7 là cát)
        if (MapUtilities.IsLargeArea(biomeMap, x, y, Biome.Sand, 3, 0.7f, width, height) &&
            !MapUtilities.IsNearBiome(biomeMap, x, y, Biome.Water, 4, width, height))
        {
            if (settings.sandResourcePrefabs.Length > 0 && Random.Range(0f, 1f) < settings.sandResourceSpawnChance)
                SpawnResource(settings.sandResourcePrefabs, x, y, settings.resourceParent);
        }
    }

    private void SpawnResource(GameObject[] resourceArray, int x, int y, Transform parent)
    {
        // Kiểm tra biên map
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        // Kiểm tra tile hợp lệ
        Vector3Int cellPos = new Vector3Int(x, y, 0);
        TileBase currentTile = tilemap.GetTile(cellPos);
        if (currentTile == null)
            return;

        // Chỉ spawn trên cỏ hoặc cát
        Biome currentBiome = biomeMap[x, y];
        if (currentBiome != Biome.Grass && currentBiome != Biome.Sand)
            return;

        // Chọn prefab ngẫu nhiên và spawn
        GameObject resourcePrefab = resourceArray[Random.Range(0, resourceArray.Length)];
        Vector3 spawnPosition = tilemap.GetCellCenterWorld(cellPos);
        Object.Instantiate(resourcePrefab, spawnPosition, Quaternion.identity, parent);
    }

    private void ClearResources(Transform resourceParent)
    {
        if (resourceParent == null) return;
        for (int i = resourceParent.childCount - 1; i >= 0; i--)
        {
            Transform c = resourceParent.GetChild(i);
            if (Application.isPlaying) 
                Object.Destroy(c.gameObject);
            else 
                Object.DestroyImmediate(c.gameObject);
        }
    }
}
