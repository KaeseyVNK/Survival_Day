using UnityEngine;
using UnityEngine.Tilemaps;

public class PerlinMapGenerator_Old : MonoBehaviour
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
    [Range(0, 5)] public int smoothingIterations = 2;
    [Range(1, 8)] public int waterCleanupThreshold = 4;
    [Range(1, 8)] public int sandCleanupThreshold = 3;
    [Range(1, 8)] public int grassCleanupThreshold = 4;

    [Header("Props")]
    public GameObject treePrefab;
    [Range(0,1)] public float treeProb = 0.08f;
    public float treeBiasHeightMin = 0.45f;
    public float treeBiasHeightMax = 0.80f;

    [Header("Resource Generation")]
    public Transform resourceParent; // Tạo một object rỗng tên "Resources" và kéo vào đây

    [Header("Grass Resources")]
    public GameObject[] grassResourcePrefabs; // Prefabs chỉ mọc trên cỏ (hoa, bụi cây)
    [Range(0, 1)] public float grassResourceSpawnChance = 0.1f;

    [Header("Sand Resources")]
    public GameObject[] sandResourcePrefabs; // Prefabs chỉ mọc trên cát (vỏ sò, xương rồng nhỏ)
    [Range(0, 1)] public float sandResourceSpawnChance = 0.05f;


    enum Biome { Water, Sand, Grass, Mountain };
    Biome[,] biomeMap;
    float[,] heightMap;
    System.Random prng;

    void Start()
    {
        prng = new System.Random(seed);
        Generate();
    }

        void Generate()
    {
        tilemap.ClearAllTiles();
        ClearResources(); // xóa toàn bộ resource cũ trước khi sinh lại
        biomeMap = new Biome[width, height];
        heightMap = new float[width, height];
        float offX = (float)prng.Next(-100000, 100000);
        float offY = (float)prng.Next(-100000, 100000);
        // ... (các dòng kế tiếp giữ nguyên) ...

        // Pass 1: Determine biomes and height based on noise
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x + worldOffset.x + offX) / scale;
                float ny = (y + worldOffset.y + offY) / scale;

                float h = FractalPerlin(nx, ny, octaves, persistence, lacunarity);
                h -= IslandFalloff(x, y, width, height, 2.2f);
                heightMap[x, y] = h;

                if (h < waterLevel) biomeMap[x, y] = Biome.Water;
                else if (h < sandLevel) biomeMap[x, y] = Biome.Sand;
                else if (h < grassLevel) biomeMap[x, y] = Biome.Grass;
                else biomeMap[x, y] = Biome.Mountain;
            }
        }

        // Pass 2: Clean up map by smoothing
        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
            MajorityFilter(1);                  // xóa điểm 1-pixel
            EnforceBeachBuffer(1);   

            // 2) Loại 1px râu ria & lạch 1px theo 4-neighbor
            OpenBiome(Biome.Grass,  Biome.Sand, iterations:1);   // xóa râu cỏ 1px
            OpenBiome(Biome.Water,  Biome.Sand, iterations:1);   // xóa vũng nước 1px

            // 3) Chặn NỐI CHÉO (diagonal bridge) gây ra “đường 1 pixel”
            RemoveDiagonalBridges(Biome.Grass, Biome.Sand);
            RemoveDiagonalBridges(Biome.Water, Biome.Sand);

            // >>> CẮT CẦU hẹp giữa 2 hồ <<<
            CutNarrowLandBridgesBetweenLakes(minGap: 4, iterations: 3);


            // 4) Làm dày đồng nhất & lấp khe mảnh 1px
            CloseBiome(Biome.Grass, Biome.Sand, iterations:1);
            CloseBiome(Biome.Water, Biome.Sand, iterations:1);

            // (tuỳ chọn) xóa đảo nhỏ nếu vẫn còn
            RemoveSmallIslands(Biome.Grass, 18, Biome.Sand);
            RemoveSmallIslands(Biome.Water, 18, Biome.Sand);


            MajorityFilter(1);   
        }

        // THÊM: Loại bỏ những vùng cỏ/cát quá nhỏ SAU KHI smoothing xong
        RemoveSmallGrassPatches(minSize: 15); // Xóa vùng cỏ < 15 ô
        RemoveSmallSandPatches(minSize: 10);  // Xóa vùng cát < 10 ô

        // Pass 3: Set tiles and props based on the final biome map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                TileBase tileToSet = GetTileForBiome(biomeMap[x, y]);

                tilemap.SetTile((Vector3Int)cell, tileToSet);

                // Place trees
                if (biomeMap[x, y] == Biome.Grass && treePrefab != null)
                {
                    float h = heightMap[x, y];
                    if (h > treeBiasHeightMin && h < treeBiasHeightMax)
                    {
                        float plantNoise = Mathf.PerlinNoise((x + 1000.123f) * 0.37f, (y - 999.321f) * 0.41f);
                        if (plantNoise < treeProb)
                        {
                            Vector3 worldPos = tilemap.CellToWorld((Vector3Int)cell) + new Vector3(0.5f, 0.5f, 0f);
                            Instantiate(treePrefab, worldPos, Quaternion.identity, transform);
                        }
                    }
                }

                // Spawn resources - CHỈ dựa vào biomeMap và kiểm tra biên chặt chẽ
                // THÊM KIỂM TRA BIÊN ĐẦU TIÊN
                if (x >= 3 && x < width - 3 && y >= 3 && y < height - 3) // Tăng buffer từ 1 lên 3
                {
                    if (biomeMap[x, y] == Biome.Grass)
                    {
                        // KIỂM TRA VÙNG CỎ PHẢI ĐỦ LỚN (ít nhất 3x3 toàn bộ là cỏ)
                        if (IsLargeGrassArea(x, y, 2) && // Kiểm tra vùng 5x5 có ít nhất 80% là cỏ
                            !IsNearBiome(x, y, Biome.Water, 4) && 
                            !IsNearBiome(x, y, Biome.Sand, 4))
                        {
                            if (grassResourcePrefabs.Length > 0 && Random.Range(0f, 1f) < grassResourceSpawnChance)
                                SpawnResource(grassResourcePrefabs, x, y);
                        }
                    }
                    else if (biomeMap[x, y] == Biome.Sand)
                    {
                        // Tương tự cho cát
                        if (IsLargeSandArea(x, y, 3) &&
                            !IsNearBiome(x, y, Biome.Water, 4))
                        {
                            if (sandResourcePrefabs.Length > 0 && Random.Range(0f, 1f) < sandResourceSpawnChance)
                                SpawnResource(sandResourcePrefabs, x, y);
                        }
                    }
                }
            }
        }
        
        // Compress bounds CHỈ MỘT LẦN sau khi hoàn tất
        tilemap.CompressBounds();
    }

    // Thêm hàm kiểm tra có đủ hàng xóm cùng loại không
    bool HasEnoughSameBiomeNeighbors(int x, int y, Biome targetBiome, int minRequired)
    {
        int count = 0;
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0 && j == 0) continue; // bỏ qua ô trung tâm
                
                int nx = x + i, ny = y + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (biomeMap[nx, ny] == targetBiome)
                        count++;
                }
            }
        }
        return count >= minRequired;
    }

    bool IsNearBiome(int x, int y, Biome b, int radius)
    {
        for (int j = -radius; j <= radius; j++)
        {
            for (int i = -radius; i <= radius; i++)
            {
                if (i == 0 && j == 0) continue;
                int nx = x + i, ny = y + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (biomeMap[nx, ny] == b) return true;
                }
            }
        }
        return false;
    }


    void ClearResources()
    {
        if (resourceParent == null) return;
        for (int i = resourceParent.childCount - 1; i >= 0; i--)
        {
            Transform c = resourceParent.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }
    }

    // Tạo một hàm trợ giúp để code gọn gàng hơn
    private void SpawnResource(GameObject[] resourceArray, int x, int y)
    {
        // KIỂM TRA BIÊN MAP TRƯỚC KHI SPAWN
        if (x < 0 || x >= width || y < 0 || y >= height)
        {          
            return;
        }

        // KIỂM TRA XEM CÓ PHẢI LÀ TILE HỢP LỆ KHÔNG
        Vector3Int cellPos = new Vector3Int(x, y, 0);
        TileBase currentTile = tilemap.GetTile(cellPos);
        if (currentTile == null)
        {
            return;
        }

        // CHỈ SPAWN TRÊN CỎ HOẶC CÁT, KHÔNG SPAWN TRÊN NƯỚC/NÚI
        Biome currentBiome = biomeMap[x, y];
        if (currentBiome != Biome.Grass && currentBiome != Biome.Sand)
        {
            return; 
        }

        // Chọn một prefab tài nguyên ngẫu nhiên từ mảng được cung cấp
        GameObject resourcePrefab = resourceArray[Random.Range(0, resourceArray.Length)];
        
        // Lấy vị trí trung tâm của ô tile
        Vector3 spawnPosition = tilemap.GetCellCenterWorld(cellPos);
        
        // Tạo instance của prefab
        Instantiate(resourcePrefab, spawnPosition, Quaternion.identity, resourceParent);
    }
    
    void SmoothMap()
    {
        Biome[,] oldBiomeMap = biomeMap.Clone() as Biome[,];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Count neighbors based on the original map state
                int waterNeighbors = GetSurroundingBiomeCount(x, y, Biome.Water, oldBiomeMap);
                int sandNeighbors = GetSurroundingBiomeCount(x, y, Biome.Sand, oldBiomeMap);
                int grassNeighbors = GetSurroundingBiomeCount(x, y, Biome.Grass, oldBiomeMap);

                Biome currentBiome = oldBiomeMap[x, y];

                if (currentBiome == Biome.Grass)
                {
                    // Rule: If a grass tile is isolated, turn it into sand.
                    if (grassNeighbors < grassCleanupThreshold)
                    {
                        biomeMap[x, y] = Biome.Sand;
                    }
                }
                else if (currentBiome == Biome.Sand)
                {
                    // Rule: If a sand tile is isolated AND NOT touching water, it's a dune. Turn it to grass.
                    // This preserves beaches but removes sand patches in fields.
                    bool isTouchingWater = (waterNeighbors > 0);
                    if (sandNeighbors < sandCleanupThreshold && !isTouchingWater)
                    {
                        biomeMap[x, y] = Biome.Grass;
                    }
                }
                else if (currentBiome == Biome.Water)
                {
                    // Rule: If a water tile is too isolated, it's a puddle. Turn it to sand.
                    if (waterNeighbors < waterCleanupThreshold)
                    {
                        biomeMap[x, y] = Biome.Sand;
                    }
                }
            }
        }
    }

    // Cập nhật hàm để nhận vào bản đồ cần tìm kiếm
    int GetSurroundingBiomeCount(int x, int y, Biome targetBiome, Biome[,] mapToSearch)
    {
        int count = 0;
        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0 && j == 0) continue;

                int checkX = x + i;
                int checkY = y + j;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    if (mapToSearch[checkX, checkY] == targetBiome)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    TileBase GetTileForBiome(Biome biome)
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

    float FractalPerlin(float x, float y, int oct, float pers, float lac)
    {
        float amp = 1f;
        float freq = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < oct; i++)
        {
            sum += Mathf.PerlinNoise(x * freq, y * freq) * amp;
            norm += amp;
            amp *= pers;
            freq *= lac;
        }
        return sum / norm;
    }
    
    float IslandFalloff(int x, int y, int w, int h, float power = 2f)
    {
        float cx = (x / (float)w) * 2f - 1f;
        float cy = (y / (float)h) * 2f - 1f;
        float dist = Mathf.Sqrt(cx * cx + cy * cy);
        dist = Mathf.InverseLerp(0f, 1.0f, dist);
        return Mathf.Pow(Mathf.Clamp01(dist), power) * 0.35f;
    }

    // ===== A. Majority filter: xóa điểm 1-pixel theo đa số hàng xóm =====
    void MajorityFilter(int iterations = 1)
    {
        for (int it = 0; it < iterations; it++)
        {
            Biome[,] src = biomeMap.Clone() as Biome[,];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int[] counts = new int[4]; // Water, Sand, Grass, Mountain
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0) continue;
                    int nx = x + i, ny = y + j;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    counts[(int)src[nx, ny]]++;
                }
                int maxIdx = 0, maxCnt = counts[0];
                for (int k = 1; k < 4; k++) if (counts[k] > maxCnt) { maxCnt = counts[k]; maxIdx = k; }
                if (maxCnt >= 5) biomeMap[x, y] = (Biome)maxIdx; // >=5/8 thì đổi
            }
        }
    }

    // ===== B. Ép dải cát đệm giữa nước và cỏ =====
    static readonly Vector2Int[] dirs8 = {
        new(1,0), new(-1,0), new(0,1), new(0,-1),
        new(1,1), new(1,-1), new(-1,1), new(-1,-1)
    };
    void EnforceBeachBuffer(int radius = 1)
    {
        var toSand = new System.Collections.Generic.List<Vector2Int>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (biomeMap[x, y] != Biome.Grass) continue;
            bool touchWater = false;
            foreach (var d in dirs8)
            {
                int nx = x + d.x, ny = y + d.y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (biomeMap[nx, ny] == Biome.Water) { touchWater = true; break; }
            }
            if (touchWater) toSand.Add(new Vector2Int(x, y));
        }
        foreach (var c in toSand) biomeMap[c.x, c.y] = Biome.Sand;

        if (radius > 1) EnforceBeachBuffer(radius - 1); // mở rộng bờ thêm 1 ô nữa nếu muốn
    }

    // ===== C. Xóa “đảo nhỏ” (flood-fill) =====
    void RemoveSmallIslands(Biome target, int minSize, Biome fillWith)
    {
        bool[,] visited = new bool[width, height];
        var q = new System.Collections.Generic.Queue<Vector2Int>();
        var comp = new System.Collections.Generic.List<Vector2Int>();

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (visited[x, y] || biomeMap[x, y] != target) continue;

            comp.Clear(); q.Clear();
            q.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                comp.Add(p);
                int[] dx = {1,-1,0,0};
                int[] dy = {0,0,1,-1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx<0||nx>=width||ny<0||ny>=height) continue;
                    if (visited[nx, ny]) continue;
                    if (biomeMap[nx, ny] != target) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }

            if (comp.Count < minSize)
                foreach (var p in comp) biomeMap[p.x, p.y] = fillWith;
        }
    }

void ErodeBiome(Biome target, Biome fillWith, int minSameNeighbors = 2, int iterations = 1)
{
    for (int it = 0; it < iterations; it++)
    {
        var next = biomeMap.Clone() as Biome[,];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (biomeMap[x,y] != target) continue;
            int same = 0;
            if (x>0         && biomeMap[x-1,y]==target) same++;
            if (x<width-1   && biomeMap[x+1,y]==target) same++;
            if (y>0         && biomeMap[x,y-1]==target) same++;
            if (y<height-1  && biomeMap[x,y+1]==target) same++;
            if (same < minSameNeighbors) next[x,y] = fillWith;
        }
        biomeMap = next;
    }
}

// NỞ: ô fillFrom nào có >= minSameNeighbors target ở 4 hướng -> biến thành target
void DilateBiome(Biome target, Biome fillFrom, int minSameNeighbors = 2, int iterations = 1)
{
    for (int it = 0; it < iterations; it++)
    {
        var next = biomeMap.Clone() as Biome[,];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (biomeMap[x,y] != fillFrom) continue;
            int same = 0;
            if (x>0         && biomeMap[x-1,y]==target) same++;
            if (x<width-1   && biomeMap[x+1,y]==target) same++;
            if (y>0         && biomeMap[x,y-1]==target) same++;
            if (y<height-1  && biomeMap[x,y+1]==target) same++;
            if (same >= minSameNeighbors) next[x,y] = target;
        }
        biomeMap = next;
    }
}

void OpenBiome(Biome target, Biome fillWith, int iterations = 1)
{
    // Erode rồi Dilate
    ErodeBiome(target, fillWith, minSameNeighbors:2, iterations:iterations);
    DilateBiome(target, fillWith, minSameNeighbors:2, iterations:iterations);
}

void CloseBiome(Biome target, Biome fillFrom, int iterations = 1)
{
    // Dilate rồi Erode (lấp khe 1px)
    DilateBiome(target, fillFrom, minSameNeighbors:2, iterations:iterations);
    ErodeBiome(target, fillFrom, minSameNeighbors:2, iterations:iterations);
}
void RemoveDiagonalBridges(Biome target, Biome fillWith)
{
    var toFill = new System.Collections.Generic.List<Vector2Int>();
    for (int y = 0; y < height-1; y++)
    for (int x = 0; x < width-1; x++)
    {
        // mẫu 1
        if (biomeMap[x,y]==target && biomeMap[x+1,y+1]==target &&
            biomeMap[x+1,y]!=target && biomeMap[x,y+1]!=target)
        {
            // cắt góc: chọn một trong hai ô target để đổi (tùy ý)
            toFill.Add(new Vector2Int(x+1,y));   // lấp ngang
            toFill.Add(new Vector2Int(x,y+1));   // lấp dọc
        }
        // mẫu 2 đảo
        if (biomeMap[x+1,y]==target && biomeMap[x,y+1]==target &&
            biomeMap[x,y]!=target && biomeMap[x+1,y+1]!=target)
        {
            toFill.Add(new Vector2Int(x,y));     // lấp
            toFill.Add(new Vector2Int(x+1,y+1)); // lấp
        }
    }
    foreach (var p in toFill)
        if (p.x>=0 && p.x<width && p.y>=0 && p.y<height)
            biomeMap[p.x,p.y] = fillWith;
}

// Gán nhãn (component id) cho các vùng Water, trả về mảng id (>=1) và tổng số vùng.
int[,] LabelWaterComponents(out int compCount)
{
    int[,] id = new int[width, height];
    compCount = 0;
    var q = new System.Collections.Generic.Queue<Vector2Int>();

    for (int y=0; y<height; y++)
    for (int x=0; x<width; x++)
    {
        if (biomeMap[x,y] != Biome.Water || id[x,y] != 0) continue;
        compCount++;
        id[x,y] = compCount;
        q.Enqueue(new Vector2Int(x,y));

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int px=p.x, py=p.y;
            int[] dx={1,-1,0,0};
            int[] dy={0,0,1,-1};
            for (int k=0;k<4;k++)
            {
                int nx=px+dx[k], ny=py+dy[k];
                if (nx<0||nx>=width||ny<0||ny>=height) continue;
                if (biomeMap[nx,ny] != Biome.Water || id[nx,ny] != 0) continue;
                id[nx,ny] = compCount;
                q.Enqueue(new Vector2Int(nx,ny));
            }
        }
    }
    return id;
}

// Cắt cầu đất/cát mỏng giữa HAI cụm nước khác nhau, lặp 'iterations' lần.
// minGap=2 nghĩa là không cho tồn tại "cầu" mỏng <2 ô (1 ô sẽ bị cắt).
void CutNarrowLandBridgesBetweenLakes(int minGap = 2, int iterations = 2)
{
    for (int it = 0; it < iterations; it++)
    {
        int[,] waterId = LabelWaterComponents(out _);
        var toWater = new System.Collections.Generic.List<Vector2Int>();

        for (int y=0; y<height; y++)
        for (int x=0; x<width; x++)
        {
            if (biomeMap[x,y] == Biome.Water) continue; // chỉ xét đất/cát/núi

            // Lấy id nước ở 4 hướng
            int L = (x>0)           && biomeMap[x-1,y]==Biome.Water ? waterId[x-1,y] : 0;
            int R = (x<width-1)     && biomeMap[x+1,y]==Biome.Water ? waterId[x+1,y] : 0;
            int U = (y<height-1)    && biomeMap[x,y+1]==Biome.Water ? waterId[x,y+1] : 0;
            int D = (y>0)           && biomeMap[x,y-1]==Biome.Water ? waterId[x,y-1] : 0;

            // Nếu ô này đang kẹp giữa 2 cụm nước KHÁC NHAU theo trục đối diện → biến thành nước
            bool bridgeLR = (L>0 && R>0 && L!=R);
            bool bridgeUD = (U>0 && D>0 && U!=D);

            if (bridgeLR || bridgeUD)
                toWater.Add(new Vector2Int(x,y));
        }

        // Áp dụng một lớp "cắt"
        foreach (var p in toWater)
            biomeMap[p.x, p.y] = Biome.Water;

        // Lặp thêm (minGap-1) lần để đảm bảo bề rộng tối thiểu
        // Ý tưởng: mỗi vòng đẩy mép nước tiến 1 ô vào "cầu"
    }

    // Sau khi cắt, ép lớp beach lại cho đẹp
    EnforceBeachBuffer(1);
}

    // Kiểm tra xem vùng cỏ có đủ lớn không (tránh spawn trên đảo cỏ nhỏ)
    bool IsLargeGrassArea(int centerX, int centerY, int radius)
    {
        int grassCount = 0;
        int totalCells = 0;
        
        for (int j = -radius; j <= radius; j++)
        {
            for (int i = -radius; i <= radius; i++)
            {
                int nx = centerX + i, ny = centerY + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    totalCells++;
                    if (biomeMap[nx, ny] == Biome.Grass)
                        grassCount++;
                }
            }
        }
        
        // Ít nhất 80% vùng xung quanh phải là cỏ
        float grassRatio = (float)grassCount / totalCells;
        return grassRatio >= 0.8f;
    }

    // Tương tự cho vùng cát
    bool IsLargeSandArea(int centerX, int centerY, int radius)
    {
        int sandCount = 0;
        int totalCells = 0;
        
        for (int j = -radius; j <= radius; j++)
        {
            for (int i = -radius; i <= radius; i++)
            {
                int nx = centerX + i, ny = centerY + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    totalCells++;
                    if (biomeMap[nx, ny] == Biome.Sand)
                        sandCount++;
                }
            }
        }
        
        // Ít nhất 70% vùng xung quanh phải là cát (cát có thể ít hơn cỏ)
        float sandRatio = (float)sandCount / totalCells;
        return sandRatio >= 0.7f;
    }

    void RemoveSmallGrassPatches(int minSize)
    {
        RemoveSmallIslands(Biome.Grass, minSize, Biome.Sand);
    }

    void RemoveSmallSandPatches(int minSize) 
    {
        // Chỉ xóa vùng cát không chạm nước (để giữ lại beach)
        RemoveSmallIslandsNotTouchingWater(Biome.Sand, minSize, Biome.Grass);
    }

    void RemoveSmallIslandsNotTouchingWater(Biome target, int minSize, Biome fillWith)
    {
        bool[,] visited = new bool[width, height];
        var q = new System.Collections.Generic.Queue<Vector2Int>();
        var comp = new System.Collections.Generic.List<Vector2Int>();

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (visited[x, y] || biomeMap[x, y] != target) continue;

            comp.Clear(); q.Clear();
            q.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;
            bool touchesWater = false;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                comp.Add(p);
                
                // Kiểm tra có chạm nước không
                if (!touchesWater && IsNearBiome(p.x, p.y, Biome.Water, 1))
                    touchesWater = true;

                int[] dx = {1,-1,0,0};
                int[] dy = {0,0,1,-1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx<0||nx>=width||ny<0||ny>=height) continue;
                    if (visited[nx, ny]) continue;
                    if (biomeMap[nx, ny] != target) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }

            // Chỉ xóa nếu vùng nhỏ VÀ không chạm nước
            if (comp.Count < minSize && !touchesWater)
                foreach (var p in comp) biomeMap[p.x, p.y] = fillWith;
        }
    }
}
