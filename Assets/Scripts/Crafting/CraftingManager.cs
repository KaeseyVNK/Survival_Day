using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager instance;

    [Tooltip("Danh sách tất cả các công thức chế tạo có thể có trong game")]
    [SerializeField] private List<CraftingRecipe> allRecipes;

    // Cache to store the normalized (trimmed) version of recipes for performance
    private Dictionary<CraftingRecipe, (List<ItemData> pattern, int width, int height)> _normalizedRecipeCache = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Normalizes a given pattern by trimming empty rows and columns.
    /// </summary>
    /// <returns>A tuple containing the trimmed pattern, its width, and its height.</returns>
    private (List<ItemData> pattern, int width, int height) NormalizePattern(List<ItemData> originalPattern, int gridWidth)
    {
        if (gridWidth <= 0 || originalPattern == null || originalPattern.Count == 0)
        {
            return (new List<ItemData>(), 0, 0);
        }

        int gridHeight = originalPattern.Count / gridWidth;
        int minX = gridWidth, minY = gridHeight, maxX = -1, maxY = -1;

        // Find the bounds of the items in the grid
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                if (index < originalPattern.Count && originalPattern[index] != null)
                {
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        // If no items were found, return an empty pattern
        if (maxX == -1)
        {
            return (new List<ItemData>(), 0, 0);
        }

        int normWidth = maxX - minX + 1;
        int normHeight = maxY - minY + 1;
        List<ItemData> normalized = new List<ItemData>();

        // Extract the sub-grid
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int index = y * gridWidth + x;
                normalized.Add(originalPattern[index]);
            }
        }

        return (normalized, normWidth, normHeight);
    }
    
    /// <summary>
    /// Checks a pattern from the crafting grid against all known recipes.
    /// This version is position-independent.
    /// </summary>
    /// <param name="currentPattern">The list of items from the UI grid.</param>
    /// <param name="gridWidth">The width of the UI grid (e.g., 3 for a 3x3 grid).</param>
    /// <returns>The matching recipe, or null if no match is found.</returns>
    public CraftingRecipe CheckRecipe(List<ItemData> currentPattern, int gridWidth)
    {
        var (normalizedInput, inputWidth, inputHeight) = NormalizePattern(currentPattern, gridWidth);

        // If the user's grid is empty after trimming, there's no recipe.
        if (normalizedInput.Count == 0)
        {
            return null;
        }

        foreach (var recipe in allRecipes)
        {
            // Normalize the recipe pattern if not already in cache.
            // Note: This assumes all recipes are defined for the same grid size as the crafting window.
            if (!_normalizedRecipeCache.ContainsKey(recipe))
            {
                _normalizedRecipeCache[recipe] = NormalizePattern(recipe.craftingPattern, gridWidth);
            }

            var (normalizedRecipe, recipeWidth, recipeHeight) = _normalizedRecipeCache[recipe];

            // Compare dimensions first
            if (inputWidth == recipeWidth && inputHeight == recipeHeight)
            {
                // Compare the actual items in the trimmed patterns
                if (normalizedInput.SequenceEqual(normalizedRecipe, new ItemDataComparer()))
                {
                    return recipe; // Found a match!
                }
            }
        }

        return null; // No match found
    }
}

// Helper class to compare DropItemData objects based on their ID, handling nulls.
public class ItemDataComparer : IEqualityComparer<ItemData>
{
    public bool Equals(ItemData x, ItemData y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;
        return x.id == y.id;
    }

    public int GetHashCode(ItemData obj)
    {
        return obj == null ? 0 : obj.id.GetHashCode();
    }
}
