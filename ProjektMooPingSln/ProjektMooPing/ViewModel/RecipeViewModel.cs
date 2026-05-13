using ProjektMooPing.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjektMooPing.ViewModel
{
    public class RecipeViewModel : INotifyPropertyChanged
    {
        public Recipe RecipeSource { get; set; }
        public string Name => RecipeSource.Name;
        public int Id => RecipeSource.Id;
        public string IngredientDisplay { get; set; }

        private int _finishedGoods;
        public int FinishedGoods
        {
            get => _finishedGoods;
            set
            {
                _finishedGoods = value;
                OnPropertyChanged(); // แจ้ง XAML ให้วาดเลขใหม่
            }
        }

        public RecipeViewModel(Recipe recipe, List<Ingredient> allIngredients)
        {
            RecipeSource = recipe;
            // ใช้ DisplayName เพื่อรองรับ 2 ภาษา
            var names = allIngredients
                .Where(i => recipe.IngredientIds.Contains(i.Id))
                .Select(i => i.DisplayName);

            IngredientDisplay = string.Join(", ", names);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
