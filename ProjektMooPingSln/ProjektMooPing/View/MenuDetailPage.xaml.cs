using CommunityToolkit.Mvvm.Messaging;
using ProjektMooPing.Models;
using ProjektMooPing.Services;
using System.Collections.ObjectModel;

namespace ProjektMooPing.View;

public partial class MenuDetailPage : ContentPage
{
    #region --- Properties & Variables ---
    private Recipe _recipe;
    private PlayerProfile _player;
    private List<Ingredient> _masterData;
    private double _currentPrice;

    public class IngredientStockView
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Stock { get; set; }
        public Color StockColor => Stock > 0 ? Colors.Black : Colors.Red;
    }
    #endregion

    #region --- Initialization ---
    public MenuDetailPage(Recipe recipe, List<Ingredient> masterData, PlayerProfile player)
    {
        InitializeComponent();
        _recipe = recipe;
        _masterData = masterData;
        _player = player;

        BindingContext = _recipe;
        _currentPrice = _recipe.SellingPrice;
        PriceEntry.Text = _currentPrice.ToString();

        LoadIngredientData();
        UpdateAnalysis();
    }
    #endregion

    #region --- Logic & Analysis ---
    private void UpdateFinishedGoodsDisplay()
    {
        int count = 0;
        if (_player.RecipeInventory != null && _player.RecipeInventory.ContainsKey(_recipe.Id))
        {
            count = _player.RecipeInventory[_recipe.Id];
        }

        FinishedGoodsLabel.Text = count.ToString();
    }
    private void LoadIngredientData()
    {
        var stockList = new List<IngredientStockView>();

        foreach (var id in _recipe.IngredientIds)
        {
            var master = _masterData.FirstOrDefault(i => i.Id == id);
            if (master != null)
            {
                int currentStock = 0;
                if (_player.IngredientInventory != null && _player.IngredientInventory.ContainsKey(id))
                {
                    currentStock = _player.IngredientInventory[id];
                }

                stockList.Add(new IngredientStockView
                {
                    Name = master.DisplayName,
                    Icon = master.Icon,
                    Stock = currentStock
                });
            }
        }
        IngredientList.ItemsSource = stockList;

        double cost = RecipeService.CalculateTotalCost(_recipe, _masterData);
        CostLabel.Text = cost.ToString("N2");
    }

    private void UpdateAnalysis()
    {
        float pop = RecipeService.CalculatePopularity(_recipe, _currentPrice, _masterData);
        PopularityLabel.Text = pop.ToString("F1");
        UpdateFinishedGoodsDisplay();
    }
    #endregion

    #region --- UI Events ---
    private void OnPriceAdjusted(object sender, EventArgs e)
    {
        if (sender is Button btn && double.TryParse(btn.CommandParameter.ToString(), out double adjust))
        {
            SoundService.PlayClick2();
            _currentPrice += adjust;
            if (_currentPrice < 1) _currentPrice = 1;
            PriceEntry.Text = _currentPrice.ToString();
        }
    }

    private void OnPriceChanged(object sender, TextChangedEventArgs e)
    {
        if (double.TryParse(e.NewTextValue, out double newPrice))
        {
            _currentPrice = newPrice;
            UpdateAnalysis();
        }
    }
    #endregion

    #region --- Save & Action Actions ---
    private async void OnAcceptClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        if (double.TryParse(PriceEntry.Text, out double newPrice))
        {
            _recipe.SellingPrice = (int)newPrice;
        }
        SoundService.PlayClick1();
        WeakReferenceMessenger.Default.Send(new RecipeUpdatedMessage(_recipe));
        await Navigation.PopModalAsync();
    }

    private async void OnCookClicked(object sender, EventArgs e)
    {
        foreach (var id in _recipe.IngredientIds)
        {
            if (!_player.IngredientInventory.ContainsKey(id) || _player.IngredientInventory[id] <= 0)
            {
                SoundService.PlayClickF();
                var master = _masterData.FirstOrDefault(i => i.Id == id);
                //await DisplayAlert("Failed", $"Ingredients '{master?.Name}' Out of Stock!", "OK");
                return;
            }
        }

        SoundService.PlayGrill();
        foreach (var id in _recipe.IngredientIds)
        {
            _player.IngredientInventory[id] -= 1;
        }

        if (_player.RecipeInventory.ContainsKey(_recipe.Id))
        {
            _player.RecipeInventory[_recipe.Id] += 1;
        }
        else
        {
            _player.RecipeInventory.Add(_recipe.Id, 1);
        }
        LoadIngredientData();
        UpdateFinishedGoodsDisplay();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClickB();
        await Navigation.PopModalAsync();
    }
    #endregion
}