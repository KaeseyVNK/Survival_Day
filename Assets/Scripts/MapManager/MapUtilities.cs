using UnityEngine;

public static class MapUtilities
{
    public enum Biome { Water, Sand, Grass, Mountain }

    // Tạo Perlin Noise với Fractal
    public static float FractalPerlin(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float amp = 1f;
        float freq = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * freq, y * freq) * amp;
            norm += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        return sum / norm;
    }
    
    // Tạo hiệu ứng đảo (falloff từ biên)
    public static float IslandFalloff(int x, int y, int width, int height, float power = 2f)
    {
        float cx = (x / (float)width) * 2f - 1f;
        float cy = (y / (float)height) * 2f - 1f;
        float dist = Mathf.Sqrt(cx * cx + cy * cy);
        dist = Mathf.InverseLerp(0f, 1.0f, dist);
        return Mathf.Pow(Mathf.Clamp01(dist), power) * 0.35f;
    }

    // Kiểm tra có gần biome nào đó không
    public static bool IsNearBiome(Biome[,] biomeMap, int x, int y, Biome targetBiome, int radius, int width, int height)
    {
        for (int j = -radius; j <= radius; j++)
        {
            for (int i = -radius; i <= radius; i++)
            {
                if (i == 0 && j == 0) continue;
                int nx = x + i, ny = y + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (biomeMap[nx, ny] == targetBiome) return true;
                }
            }
        }
        return false;
    }

    // Đếm số lượng biome xung quanh
    public static int GetSurroundingBiomeCount(Biome[,] biomeMap, int x, int y, Biome targetBiome, int width, int height)
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
                    if (biomeMap[checkX, checkY] == targetBiome)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    // Kiểm tra vùng có đủ lớn không
    public static bool IsLargeArea(Biome[,] biomeMap, int centerX, int centerY, Biome targetBiome, int radius, float minRatio, int width, int height)
    {
        int targetCount = 0;
        int totalCells = 0;
        
        for (int j = -radius; j <= radius; j++)
        {
            for (int i = -radius; i <= radius; i++)
            {
                int nx = centerX + i, ny = centerY + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    totalCells++;
                    if (biomeMap[nx, ny] == targetBiome)
                        targetCount++;
                }
            }
        }
        
        float ratio = (float)targetCount / totalCells;
        return ratio >= minRatio;
    }
}
