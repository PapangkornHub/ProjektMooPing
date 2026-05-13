using ProjektMooPing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjektMooPing.Services
{
    public static class RecipeService
    {
        // สูตร Popularity = max(0, (avg(BasePopularity) + synergyBonus) - (sellingPrice - cost))
        public static float CalculateTotalScore(Recipe recipe, List<Ingredient> masterData)
        {
            var myIngredients = masterData
                .Where(i => recipe.IngredientIds.Contains(i.Id))
                .ToList();

            if (myIngredients.Count == 0) return 0;

            // Base Score
            float baseScore = (float)myIngredients.Average(i => i.BasePopularity);

            // Synergy Bonus
            float synergyBonus = 0;
            var categories = myIngredients.Select(i => i.Category).Distinct().ToList();

            if (categories.Contains("Meat"))
            {
                if (categories.Contains("Spice")) synergyBonus += 5;
                if (categories.Contains("Seasoning")) synergyBonus += 10;
                if (categories.Contains("Marinade")) synergyBonus += 5;
                if (categories.Contains("Side")) synergyBonus += 10;
            }
            var ids = recipe.IngredientIds;
            // --- สูตร Synergy Bonus พิเศษ ---
            // น้ำตาลปี๊บ + กะทิ
            if (ids.Contains(17) && ids.Contains(19)) synergyBonus += 15;

            // กระเทียม + รากผักชี + พริกไทยขาวไม่ก็ดำ
            if (ids.Contains(7) && ids.Contains(8) && (ids.Contains(9) || ids.Contains(10)))
                synergyBonus += 20;

            // ตับ + พริกไทยขาว
            if (ids.Contains(6) && ids.Contains(10)) synergyBonus += 15;

            // อกไก่ + เบกกิ้งโซดา
            if (ids.Contains(4) && ids.Contains(21)) synergyBonus += 15;

            // พริกป่น + น้ำปลา
            if (ids.Contains(11) && ids.Contains(13)) synergyBonus += 10;

            // น้ำตาลปี๊บ + กะทิ
            if (ids.Contains(17) && ids.Contains(19)) synergyBonus += 15;

            // --- Special Beef Synergy ---
            if (ids.Contains(5)) // Beef
            {
                if (ids.Contains(24)) synergyBonus += 15; // สับปะรด + เนื้อ? Mama Mia ของแปก
                if (ids.Contains(9)) synergyBonus += 10;  // พริกไทยดำ + เนื้อ
            }

            // --- Penalty ไม่อร่อย ---
            // น้ำผึ้ง + ผงชูรส
            if (ids.Contains(23) && ids.Contains(18)) synergyBonus -= 15;
            // น้ำปลา + นมสด
            if (ids.Contains(13) && ids.Contains(20)) synergyBonus -= 20;
            // สับปะรด + นม
            if (ids.Contains(24) && ids.Contains(20)) synergyBonus -= 25;

            return baseScore + synergyBonus;
        }
        public static double CalculateTotalCost(Recipe recipe, List<Ingredient> masterData)
        {
            return masterData
                .Where(i => recipe.IngredientIds.Contains(i.Id))
                .Sum(i => i.BaseCost);
        }

        public static float CalculatePopularity(Recipe recipe, double sellingPrice, List<Ingredient> masterData)
        {
            float qualityScore = CalculateTotalScore(recipe, masterData);
            double cost = CalculateTotalCost(recipe, masterData);

            double profitMargin = sellingPrice - cost;
            float finalPopularity = qualityScore - (float)profitMargin;

            return Math.Max(0, finalPopularity);
        }
    }
}
