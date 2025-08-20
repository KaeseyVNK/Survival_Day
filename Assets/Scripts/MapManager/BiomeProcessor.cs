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
    private int width, height;
    private Biome[,] biomeMap;
    
    public List<Vector2Int> debug_pixelsToFill = new List<Vector2Int>();

    public BiomeProcessor(int mapWidth, int mapHeight)
    {
        width = mapWidth;
        height = mapHeight;
    }

    public void ProcessBiomes(Biome[,] inputBiomeMap, BiomeSmoothingSettings settings)
    {
        biomeMap = inputBiomeMap;
        width = inputBiomeMap.GetLength(0);
        height = inputBiomeMap.GetLength(1);
        debug_pixelsToFill.Clear();

        for (int i = 0; i < settings.smoothingIterations; i++)
        {
            SmoothMap(settings);
            MajorityFilter(1);
            EnforceBeachBuffer(1);
            OpenBiome(Biome.Grass, Biome.Sand, iterations: settings.iterations);
            OpenBiome(Biome.Water, Biome.Sand, iterations: settings.iterations);
            RemoveDiagonalBridges(Biome.Grass, Biome.Sand);
            RemoveDiagonalBridges(Biome.Water, Biome.Sand);
            CutNarrowLandBridgesBetweenLakes(minGap: 6, iterations: settings.iterations);
            CloseBiome(Biome.Grass, Biome.Sand, iterations: settings.iterations);
            CloseBiome(Biome.Water, Biome.Sand, iterations: settings.iterations);
            RemoveSmallIslands(Biome.Grass, 18, Biome.Sand);
            RemoveSmallIslands(Biome.Water, 18, Biome.Sand);
            MajorityFilter(1);
        }

        RemoveSmallGrassPatches(15);
        RemoveSmallSandPatches(10);
        AggressiveCleanup();
    }

    private void SmoothMap(BiomeSmoothingSettings settings)
    {
        Biome[,] oldBiomeMap = biomeMap.Clone() as Biome[,];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int waterNeighbors = MapUtilities.GetSurroundingBiomeCount(oldBiomeMap, x, y, Biome.Water, width, height);
                int sandNeighbors = MapUtilities.GetSurroundingBiomeCount(oldBiomeMap, x, y, Biome.Sand, width, height);
                int grassNeighbors = MapUtilities.GetSurroundingBiomeCount(oldBiomeMap, x, y, Biome.Grass, width, height);
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

    private void MajorityFilter(int iterations = 1)
    {
        for (int it = 0; it < iterations; it++)
        {
            Biome[,] src = biomeMap.Clone() as Biome[,];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int[] counts = new int[4];
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
                if (maxCnt >= 5) biomeMap[x, y] = (Biome)maxIdx;
            }
        }
    }

    private void EnforceBeachBuffer(int radius = 1)
    {
        var toSand = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (biomeMap[x, y] != Biome.Grass) continue;
            for (int j = -radius; j <= radius; j++)
            for (int i = -radius; i <= radius; i++)
            {
                if (i==0 && j==0) continue;
                int nx = x + i, ny = y + j;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (biomeMap[nx, ny] == Biome.Water)
                {
                    toSand.Add(new Vector2Int(x, y));
                    goto next_pixel;
                }
            }
            next_pixel:;
        }
        foreach (var c in toSand) biomeMap[c.x, c.y] = Biome.Sand;
    }

    private void RemoveSmallIslands(Biome target, int minSize, Biome fillWith)
    {
        bool[,] visited = new bool[width, height];
        var q = new Queue<Vector2Int>();
        var comp = new List<Vector2Int>();
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
                int[] dx = {1,-1,0,0}, dy = {0,0,1,-1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx<0||nx>=width||ny<0||ny>=height || visited[nx, ny] || biomeMap[nx, ny] != target) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            if (comp.Count < minSize)
                foreach (var p in comp) biomeMap[p.x, p.y] = fillWith;
        }
    }
    
    private void ErodeBiome(Biome target, Biome fillWith, int iterations = 1)
    {
        for (int it = 0; it < iterations; it++)
        {
            var toFill = new List<Vector2Int>();
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (biomeMap[x,y] != target) continue;
                if (biomeMap[x-1,y]!=target || biomeMap[x+1,y]!=target || biomeMap[x,y-1]!=target || biomeMap[x,y+1]!=target)
                    toFill.Add(new Vector2Int(x,y));
            }
            foreach(var p in toFill) biomeMap[p.x, p.y] = fillWith;
        }
    }

    private void DilateBiome(Biome target, Biome fillFrom, int iterations = 1)
    {
        for (int it = 0; it < iterations; it++)
        {
            var toFill = new List<Vector2Int>();
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (biomeMap[x,y] != fillFrom) continue;
                if (biomeMap[x-1,y]==target || biomeMap[x+1,y]==target || biomeMap[x,y-1]==target || biomeMap[x,y+1]==target)
                    toFill.Add(new Vector2Int(x,y));
            }
            foreach(var p in toFill) biomeMap[p.x, p.y] = target;
        }
    }

    private void OpenBiome(Biome target, Biome fillWith, int iterations = 1)
    {
        ErodeBiome(target, fillWith, iterations:iterations);
        DilateBiome(target, fillWith, iterations:iterations);
    }

    private void CloseBiome(Biome target, Biome fillFrom, int iterations = 1)
    {
        DilateBiome(target, fillFrom, iterations:iterations);
        ErodeBiome(target, fillFrom, iterations:iterations);
    }

    private void RemoveDiagonalBridges(Biome target, Biome fillWith)
    {
        for (int y = 0; y < height-1; y++)
        for (int x = 0; x < width-1; x++)
        {
            if (biomeMap[x,y]==target && biomeMap[x+1,y+1]==target && biomeMap[x+1,y]!=target && biomeMap[x,y+1]!=target)
                biomeMap[x+1,y] = fillWith;
            if (biomeMap[x+1,y]==target && biomeMap[x,y+1]==target && biomeMap[x,y]!=target && biomeMap[x+1,y+1]!=target)
                biomeMap[x,y] = fillWith;
        }
    }

    private void CutNarrowLandBridgesBetweenLakes(int minGap = 2, int iterations = 2)
    {
        for (int it = 0; it < iterations; it++)
        {
            int[,] waterId = LabelWaterComponents(out _);
            var toWater = new List<Vector2Int>();
            for (int y=1; y<height-1; y++)
            for (int x=1; x<width-1; x++)
            {
                if (biomeMap[x,y] == Biome.Water) continue;
                int L = waterId[x-1,y], R = waterId[x+1,y], U = waterId[x,y+1], D = waterId[x,y-1];
                if ((L>0 && R>0 && L!=R) || (U>0 && D>0 && U!=D))
                    toWater.Add(new Vector2Int(x,y));
            }
            if (toWater.Count == 0) break;
            foreach (var p in toWater) biomeMap[p.x, p.y] = Biome.Water;
        }
        EnforceBeachBuffer(1);
    }

    private int[,] LabelWaterComponents(out int compCount)
    {
        int[,] id = new int[width, height];
        compCount = 0;
        var q = new Queue<Vector2Int>();
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
                int[] dx={1,-1,0,0}, dy={0,0,1,-1};
                for (int k=0;k<4;k++)
                {
                    int nx=p.x+dx[k], ny=p.y+dy[k];
                    if (nx<0||nx>=width||ny<0||ny>=height || biomeMap[nx,ny] != Biome.Water || id[nx,ny] != 0) continue;
                    id[nx,ny] = compCount;
                    q.Enqueue(new Vector2Int(nx,ny));
                }
            }
        }
        return id;
    }

    private void RemoveSmallGrassPatches(int minSize) { RemoveSmallIslands(Biome.Grass, minSize, Biome.Sand); }
    private void RemoveSmallSandPatches(int minSize) { RemoveSmallIslandsNotTouchingWater(Biome.Sand, minSize, Biome.Grass); }

    private void RemoveSmallIslandsNotTouchingWater(Biome target, int minSize, Biome fillWith)
    {
        bool[,] visited = new bool[width, height];
        var q = new Queue<Vector2Int>();
        var comp = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (visited[x, y] || biomeMap[x, y] != target) continue;
            comp.Clear(); q.Clear();
            q.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;
            bool touchesWater = false;
            var currentComp = new List<Vector2Int>();
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                currentComp.Add(p);
                if (!touchesWater && MapUtilities.IsNearBiome(biomeMap, p.x, p.y, Biome.Water, 1, width, height)) touchesWater = true;
                int[] dx = {1,-1,0,0}, dy = {0,0,1,-1};
                for (int k = 0; k < 4; k++)
                {
                    int nx = p.x + dx[k], ny = p.y + dy[k];
                    if (nx<0||nx>=width||ny<0||ny>=height || visited[nx, ny] || biomeMap[nx, ny] != target) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            if (currentComp.Count < minSize && !touchesWater)
                foreach (var p in currentComp) biomeMap[p.x, p.y] = fillWith;
        }
    }
    
    private void AggressiveCleanup()
    {
        for (int i = 0; i < 3; i++) { RemoveSimpleTails(); MajorityFilter(1); }
        MajorityFilter(5);
        ApplyLowConnectivityRule(Biome.Grass, Biome.Sand, minNeighbors: 4, iterations: 6);
        ApplyLowConnectivityRule(Biome.Water, Biome.Sand, minNeighbors: 4, iterations: 6);
        MajorityFilter(1);
    }

    private Biome GetMostCommonNeighbor(int x, int y)
    {
        int[] counts = new int[4];
        for (int j = -1; j <= 1; j++)
        for (int i = -1; i <= 1; i++)
        {
            if (i == 0 && j == 0) continue;
            int nx = x + i, ny = y + j;
            if (nx >= 0 && nx < width && ny >= 0 && ny < height) counts[(int)biomeMap[nx, ny]]++;
        }
        int maxIdx = 0;
        for (int k = 1; k < 4; k++) if (counts[k] > counts[maxIdx]) maxIdx = k;
        return (Biome)maxIdx;
    }

    private void ApplyLowConnectivityRule(Biome target, Biome fillWith, int minNeighbors, int iterations)
    {
        for (int it = 0; it < iterations; it++)
        {
            var toFill = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (biomeMap[x,y] != target) continue;
                int sameNeighbors = 0;
                for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    if (i == 0 && j == 0) continue;
                    int nx = x + i, ny = y + j;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && biomeMap[nx, ny] == target) 
                        sameNeighbors++;
                }
                if (sameNeighbors < minNeighbors) toFill.Add(new Vector2Int(x,y));
            }
            if (toFill.Count == 0) break;
            debug_pixelsToFill.AddRange(toFill.Where(p => !debug_pixelsToFill.Contains(p)));
            foreach (var p in toFill) biomeMap[p.x,p.y] = fillWith;
        }
    }

    private void RemoveSimpleTails()
    {
        var toChange = new List<(Vector2Int pos, Biome newBiome)>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Biome center = biomeMap[x, y];
            int cardinalNeighbors = 0;
            if (x > 0 && biomeMap[x-1, y] == center) cardinalNeighbors++;
            if (x < width - 1 && biomeMap[x+1, y] == center) cardinalNeighbors++;
            if (y > 0 && biomeMap[x, y-1] == center) cardinalNeighbors++;
            if (y < height - 1 && biomeMap[x, y+1] == center) cardinalNeighbors++;

            int totalNeighbors = 0;
            for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                if (i==0 && j==0) continue;
                int nx = x + i, ny = y + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && biomeMap[nx, ny] == center) 
                    totalNeighbors++;
            }

            if (cardinalNeighbors <= 1 || (cardinalNeighbors == 2 && totalNeighbors < 4) || totalNeighbors < 3)
                toChange.Add((new Vector2Int(x, y), GetMostCommonNeighbor(x, y)));
        }
        foreach (var change in toChange)
            biomeMap[change.pos.x, change.pos.y] = change.newBiome;
    }
}
