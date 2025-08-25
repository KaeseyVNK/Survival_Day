using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static MapUtilities;

[System.Serializable]
public class BiomeSmoothingSettings
{
    [Range(0, 5)] public int smoothingIterations = 2;
    [Range(1, 8)] public int waterCleanupThreshold = 4;
    [Range(1, 8)] public int sandCleanupThreshold = 3;
    [Range(1, 8)] public int grassCleanupThreshold = 4;
    [Range(0, 8)] public int iterations = 4;
}

public class BiomeProcessor
{
    public List<Vector2Int> debug_pixelsToFill = new List<Vector2Int>();
    public BiomeProcessor() { }

    public void ProcessBiomes(ref Biome[,] biomeMap, BiomeSmoothingSettings settings)
    {
        int width = biomeMap.GetLength(0);
        int height = biomeMap.GetLength(1);

        for (int i = 0; i < settings.smoothingIterations; i++)
        {
            SmoothMap(ref biomeMap, settings, width, height);
            MajorityFilter(ref biomeMap, 1, width, height);
        }

        // Clean up small islands first
        RemoveSmallIslands(ref biomeMap, Biome.Water, settings.waterCleanupThreshold * 2, Biome.Sand, width, height);
        RemoveSmallIslands(ref biomeMap, Biome.Grass, settings.grassCleanupThreshold * 2, Biome.Sand, width, height);
        
        // Enforce a beach buffer between grass and water
        EnforceBeachBuffer(ref biomeMap, 1, width, height);

        // Perform aggressive final cleanup to remove tails and artifacts
        AggressiveCleanup(ref biomeMap, width, height);
    }

    private void SmoothMap(ref Biome[,] biomeMap, BiomeSmoothingSettings settings, int width, int height)
    {
        Biome[,] oldBiomeMap = biomeMap.Clone() as Biome[,];
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int waterNeighbors = GetSurroundingBiomeCount(oldBiomeMap, x, y, Biome.Water, width, height);
                int sandNeighbors = GetSurroundingBiomeCount(oldBiomeMap, x, y, Biome.Sand, width, height);
                int grassNeighbors = GetSurroundingBiomeCount(oldBiomeMap, x, y, Biome.Grass, width, height);
                Biome currentBiome = oldBiomeMap[x, y];
                if (currentBiome == Biome.Grass)
                {
                    if (grassNeighbors < settings.grassCleanupThreshold) biomeMap[x, y] = Biome.Sand;
                }
                else if (currentBiome == Biome.Sand)
                {
                    if (sandNeighbors < settings.sandCleanupThreshold && waterNeighbors == 0) biomeMap[x, y] = Biome.Grass;
                }
                else if (currentBiome == Biome.Water)
                {
                    if (waterNeighbors < settings.waterCleanupThreshold) biomeMap[x, y] = Biome.Sand;
                }
            }
        }
    }

    private void MajorityFilter(ref Biome[,] biomeMap, int iterations, int width, int height)
    {
        for (int it = 0; it < iterations; it++)
        {
            Biome[,] src = biomeMap.Clone() as Biome[,];
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                int[] counts = new int[4];
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0) continue;
                    int nx = x + i, ny = y + j;
                    counts[(int)src[nx, ny]]++;
                }
                int maxIdx = 0, maxCnt = counts[0];
                for (int k = 1; k < 4; k++) if (counts[k] > maxCnt) { maxCnt = counts[k]; maxIdx = k; }
                if (maxCnt >= 5) biomeMap[x, y] = (Biome)maxIdx;
            }
        }
    }

    private void EnforceBeachBuffer(ref Biome[,] biomeMap, int radius, int width, int height)
    {
        var toSand = new List<Vector2Int>();
        for (int y = radius; y < height - radius; y++)
        for (int x = radius; x < width - radius; x++)
        {
            if (biomeMap[x, y] != Biome.Grass) continue;
            bool nearWater = false;
            for (int j = -radius; j <= radius; j++)
            {
                for (int i = -radius; i <= radius; i++)
                {
                    if (i == 0 && j == 0) continue;
                    int nx = x + i, ny = y + j;
                    if (biomeMap[nx, ny] == Biome.Water)
                    {
                        nearWater = true;
                        break;
                    }
                }
                if (nearWater) break;
            }
            if (nearWater) toSand.Add(new Vector2Int(x, y));
        }
        foreach (var c in toSand) biomeMap[c.x, c.y] = Biome.Sand;
    }

    private void RemoveSmallIslands(ref Biome[,] biomeMap, Biome target, int minSize, Biome fillWith, int width, int height)
    {
        bool[,] visited = new bool[width, height];
        var q = new Queue<Vector2Int>();
        var comp = new List<Vector2Int>();
        for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++)
        {
            if (visited[x, y] || biomeMap[x, y] != target) continue;
            comp.Clear(); q.Clear();
            q.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                comp.Add(p);
                int[] dx = {1, -1, 0, 0}, dy = {0, 0, 1, -1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || visited[nx, ny] || biomeMap[nx, ny] != target) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            if (comp.Count < minSize)
                foreach (var p in comp) biomeMap[p.x, p.y] = fillWith;
        }
    }
    
    private void ErodeBiome(ref Biome[,] biomeMap, Biome target, Biome fillWith, int iterations, int width, int height)
    {
        for (int it = 0; it < iterations; it++)
        {
            var toFill = new List<Vector2Int>();
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (biomeMap[x, y] != target) continue;
                if (biomeMap[x - 1, y] != target || biomeMap[x + 1, y] != target || biomeMap[x, y - 1] != target || biomeMap[x, y + 1] != target)
                    toFill.Add(new Vector2Int(x, y));
            }
            foreach(var p in toFill) biomeMap[p.x, p.y] = fillWith;
        }
    }

    private void DilateBiome(ref Biome[,] biomeMap, Biome target, Biome fillFrom, int iterations, int width, int height)
    {
        for (int it = 0; it < iterations; it++)
        {
            var toFill = new List<Vector2Int>();
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (biomeMap[x, y] != fillFrom) continue;
                if (biomeMap[x - 1, y] == target || biomeMap[x + 1, y] == target || biomeMap[x, y - 1] == target || biomeMap[x, y + 1] == target)
                    toFill.Add(new Vector2Int(x, y));
            }
            foreach(var p in toFill) biomeMap[p.x, p.y] = target;
        }
    }

    private void OpenBiome(ref Biome[,] biomeMap, Biome target, Biome fillWith, int iterations, int width, int height)
    {
        ErodeBiome(ref biomeMap, target, fillWith, iterations, width, height);
        DilateBiome(ref biomeMap, target, fillFrom: fillWith, iterations, width, height);
    }

    private void CloseBiome(ref Biome[,] biomeMap, Biome target, Biome fillFrom, int iterations, int width, int height)
    {
        DilateBiome(ref biomeMap, target, fillFrom, iterations, width, height);
        ErodeBiome(ref biomeMap, target, fillFrom, iterations, width, height);
    }

    private void RemoveDiagonalBridges(ref Biome[,] biomeMap, Biome target, Biome fillWith, int width, int height)
    {
        for (int y = 0; y < height - 1; y++)
        for (int x = 0; x < width - 1; x++)
        {
            if (biomeMap[x, y] == target && biomeMap[x + 1, y + 1] == target && biomeMap[x + 1, y] != target && biomeMap[x, y + 1] != target)
                biomeMap[x + 1, y] = fillWith;
            if (biomeMap[x + 1, y] == target && biomeMap[x, y + 1] == target && biomeMap[x, y] != target && biomeMap[x + 1, y + 1] != target)
                biomeMap[x, y] = fillWith;
        }
    }

    private void CutNarrowLandBridgesBetweenLakes(ref Biome[,] biomeMap, int minGap, int iterations, int width, int height)
    {
        for (int it = 0; it < iterations; it++)
        {
            int[,] waterId = LabelWaterComponents(biomeMap, out _, width, height);
            var toWater = new List<Vector2Int>();
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (biomeMap[x, y] == Biome.Water) continue;
                int L = waterId[x - 1, y], R = waterId[x + 1, y], U = waterId[x, y + 1], D = waterId[x, y - 1];
                if ((L > 0 && R > 0 && L != R) || (U > 0 && D > 0 && U != D))
                    toWater.Add(new Vector2Int(x, y));
            }
            if (toWater.Count == 0) break;
            foreach (var p in toWater) biomeMap[p.x, p.y] = Biome.Water;
        }
        EnforceBeachBuffer(ref biomeMap, 1, width, height);
    }

    private int[,] LabelWaterComponents(Biome[,] biomeMap, out int compCount, int width, int height)
    {
        int[,] id = new int[width, height];
        compCount = 0;
        var q = new Queue<Vector2Int>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (biomeMap[x, y] != Biome.Water || id[x, y] != 0) continue;
            compCount++;
            id[x, y] = compCount;
            q.Enqueue(new Vector2Int(x, y));
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int[] dx = {1, -1, 0, 0}, dy = {0, 0, 1, -1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || biomeMap[nx, ny] != Biome.Water || id[nx, ny] != 0) continue;
                    id[nx, ny] = compCount;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
        return id;
    }

    private void RemoveSmallGrassPatches(ref Biome[,] biomeMap, int minSize, int width, int height) { RemoveSmallIslands(ref biomeMap, Biome.Grass, minSize, Biome.Sand, width, height); }
    private void RemoveSmallSandPatches(ref Biome[,] biomeMap, int minSize, int width, int height) { RemoveSmallIslandsNotTouchingWater(ref biomeMap, Biome.Sand, minSize, Biome.Grass, width, height); }

    private void RemoveSmallIslandsNotTouchingWater(ref Biome[,] biomeMap, Biome target, int minSize, Biome fillWith, int width, int height)
    {
        bool[,] visited = new bool[width, height];
        var q = new Queue<Vector2Int>();
        for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++)
        {
            if (visited[x, y] || biomeMap[x, y] != target) continue;
            var currentComp = new List<Vector2Int>();
            q.Clear();
            q.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;
            bool touchesWater = false;
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                currentComp.Add(p);
                if (!touchesWater && IsNearBiome(biomeMap, p.x, p.y, Biome.Water, 1, width, height)) touchesWater = true;
                int[] dx = {1, -1, 0, 0}, dy = {0, 0, 1, -1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || visited[nx, ny] || biomeMap[nx, ny] != target) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            if (currentComp.Count < minSize && !touchesWater)
                foreach (var p in currentComp) biomeMap[p.x, p.y] = fillWith;
        }
    }
    
    private void AggressiveCleanup(ref Biome[,] biomeMap, int width, int height)
    {
        for (int i = 0; i < 3; i++) { RemoveSimpleTails(ref biomeMap, width, height); MajorityFilter(ref biomeMap, 1, width, height); }
        MajorityFilter(ref biomeMap, 5, width, height);
        ApplyLowConnectivityRule(ref biomeMap, Biome.Grass, Biome.Sand, 4, 6, width, height);
        ApplyLowConnectivityRule(ref biomeMap, Biome.Water, Biome.Sand, 4, 6, width, height);
        MajorityFilter(ref biomeMap, 1, width, height);
    }

    private Biome GetMostCommonNeighbor(Biome[,] biomeMap, int x, int y, int width, int height)
    {
        int[] counts = new int[4];
        for (int j = -1; j <= 1; j++)
        for (int i = -1; i <= 1; i++)
        {
            if (i == 0 && j == 0) continue;
            int nx = x + i, ny = y + j;
            counts[(int)biomeMap[nx, ny]]++;
        }
        int maxIdx = 0;
        for (int k = 1; k < 4; k++) if (counts[k] > counts[maxIdx]) maxIdx = k;
        return (Biome)maxIdx;
    }

    private void ApplyLowConnectivityRule(ref Biome[,] biomeMap, Biome target, Biome fillWith, int minNeighbors, int iterations, int width, int height)
    {
        for (int it = 0; it < iterations; it++)
        {
            var toFill = new List<Vector2Int>();
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (biomeMap[x, y] != target) continue;
                int sameNeighbors = 0;
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0) continue;
                    int nx = x + i, ny = y + j;
                    if (biomeMap[nx, ny] == target) 
                        sameNeighbors++;
                }
                if (sameNeighbors < minNeighbors) toFill.Add(new Vector2Int(x, y));
            }
            if (toFill.Count == 0) break;
            debug_pixelsToFill.AddRange(toFill.Where(p => !debug_pixelsToFill.Contains(p)));
            foreach (var p in toFill) biomeMap[p.x, p.y] = fillWith;
        }
    }

    private void RemoveSimpleTails(ref Biome[,] biomeMap, int width, int height)
    {
        var toChange = new List<(Vector2Int pos, Biome newBiome)>();
        for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++)
        {
            Biome center = biomeMap[x, y];
            int cardinalNeighbors = 0;
            if (biomeMap[x - 1, y] == center) cardinalNeighbors++;
            if (biomeMap[x + 1, y] == center) cardinalNeighbors++;
            if (biomeMap[x, y - 1] == center) cardinalNeighbors++;
            if (biomeMap[x, y + 1] == center) cardinalNeighbors++;

            int totalNeighbors = 0;
            for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0 && j == 0) continue;
                int nx = x + i, ny = y + j;
                if (biomeMap[nx, ny] == center) 
                    totalNeighbors++;
            }

            if (cardinalNeighbors <= 1 || (cardinalNeighbors == 2 && totalNeighbors < 4) || totalNeighbors < 3)
                toChange.Add((new Vector2Int(x, y), GetMostCommonNeighbor(biomeMap, x, y, width, height)));
        }
        foreach (var change in toChange)
            biomeMap[change.pos.x, change.pos.y] = change.newBiome;
    }
}
