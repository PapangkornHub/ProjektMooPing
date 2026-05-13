using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ProjektMooPing.Models
{
    public class PlayerProfile
    {
        public double Money { get; set; }
        public int Day { get; set; } = 1;

        public List<Recipe> CreatedRecipes { get; set; } = new List<Recipe>();

        // --- Ingredient ---
        // ID
        public HashSet<int> UnlockedIngredientIds { get; set; } = new HashSet<int> { 1 };

        // Ingredient ID, Quantity
        public Dictionary<int, int> IngredientInventory { get; set; } = new Dictionary<int, int>();

        // --- Recipes ---
        // Recipes
        public HashSet<int> UnlockedRecipeIds { get; set; } = new HashSet<int>();
        // Recipes ID, Quantity
        public Dictionary<int, int> RecipeInventory { get; set; } = new Dictionary<int, int>();
        public bool IsAllowed { get; set; }

        // --- Rating ---
        public int TotalRating { get; set; } = 0;

        public PlayerProfile()
        {
            // Starting Money
            Money = 100;

            var defaultRecipe = new Recipe
            {
                Id = 1,
                Name = "Original",
                IngredientIds = new List<int> { 1, 8 },
                SellingPrice = 25,
                IsAllowed = true
            };

            CreatedRecipes.Add(defaultRecipe);
            UnlockedRecipeIds.Add(1);
            RecipeInventory.Add(1, 5);

            // Starting Ingredients - ID 1, 8
            UnlockedIngredientIds = new HashSet<int> { 1, 8 };

            // Starting Ingredient Inventory
            IngredientInventory = new Dictionary<int, int>
            {
                { 1, 5 }, // มีเนื้อไหล่หมู 5 ชิ้น
                { 8, 5 }, // มีรากผักชี 5 ชุด
            };
        }
    }
}
