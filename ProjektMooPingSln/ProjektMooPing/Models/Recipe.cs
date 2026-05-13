using System;
using System.Collections.Generic;
using System.Text;

namespace ProjektMooPing.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> IngredientIds { get; set; }
        public int SellingPrice { get; set; }
        public bool IsAllowed { get; set; } = false;
    }
}
