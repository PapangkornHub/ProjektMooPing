using CommunityToolkit.Mvvm.Messaging;
using ProjektMooPing.Models;
using ProjektMooPing.Services;
using ProjektMooPing.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ProjektMooPing.View;

namespace ProjektMooPing;

public partial class EditDetailPage : ContentPage
{
    #region --- Properties & Variables ---
    public ObservableCollection<IngredientViewModel> SelectableIngredients { get; set; } = new();
    public ObservableCollection<IngredientViewModel> FilteredEditIngredients { get; set; } = new();
    private string _editCategoryFilter = "All";
    private Recipe _recipeToEdit;
    private PlayerProfile _player;
    private List<Ingredient> _masterData;
    private bool _isNewRecipeAllowed = false;
    private string _recipeName = LocalizationService.Instance.LblNewMenu;
    public string RecipeName
    {
        get => _recipeName;
        set { _recipeName = value; OnPropertyChanged(); }
    }
    #endregion

    #region --- Initialization ---
    public EditDetailPage(PlayerProfile player, List<Ingredient> allIngredients, Recipe recipe = null)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EditDetailPage XAML Initialization Error: {ex.Message}");
            Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }

        try
        {
            _recipeToEdit = recipe;
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _masterData = allIngredients ?? throw new ArgumentNullException(nameof(allIngredients));

            if (_recipeToEdit != null)
                UpdateAllowButtonVisual(_recipeToEdit.IsAllowed);
            else
                UpdateAllowButtonVisual(_isNewRecipeAllowed);

            InitializeIngredients();
            BuildEditFilterButtons();
            ApplyEditFilter();

            if (_recipeToEdit != null)
                RecipeName = _recipeToEdit.Name;

            BindingContext = this;

            UpdateAnalysis();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EditDetailPage Data Initialization Error: {ex.Message}");
            Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void InitializeIngredients()
    {
        SelectableIngredients.Clear();

        if (_player?.UnlockedIngredientIds == null || _masterData == null)
        {
            Debug.WriteLine("Warning: UnlockedIngredientIds or MasterData is null");
            return;
        }

        foreach (var id in _player.UnlockedIngredientIds)
        {
            try
            {
                var master = _masterData.FirstOrDefault(i => i.Id == id);
                if (master == null)
                {
                    Debug.WriteLine($"Warning: Ingredient with ID {id} not found in master data");
                    continue;
                }

                var vm = new IngredientViewModel
                {
                    Id = master.Id,
                    Name = master.Name ?? "Unknown",
                    NameTh = master.NameTh,
                    Category = master.Category,
                    Icon = master.Icon
                };

                if (_recipeToEdit != null && _recipeToEdit.IngredientIds?.Contains(id) == true)
                    vm.IsSelected = true;

                SelectableIngredients.Add(vm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing ingredient {id}: {ex.Message}");
            }
        }
    }
    #endregion

    #region --- Logic & Analysis ---
    private void BuildEditFilterButtons()
    {
        if (EditFilterRow == null) return;
        EditFilterRow.Children.Clear();
        var L = LocalizationService.Instance;
        var darkBrown = (Color)Application.Current.Resources["MooPingDarkBrown"];

        var categories = new[] { "All" }
            .Concat(SelectableIngredients.Select(i => i.Category).Distinct().OrderBy(c => c))
            .ToList();

        foreach (var cat in categories)
        {
            bool isActive = cat == _editCategoryFilter;
            var btn = new Button
            {
                Text = cat == "All" ? (L.IsThai ? "ทั้งหมด" : "All") : cat,
                HeightRequest = 28,
                Padding = new Thickness(8, 0),
                CornerRadius = 14,
                FontSize = 11,
                BackgroundColor = isActive ? darkBrown : Color.FromArgb("#CCCCCC"),
                TextColor = isActive ? Colors.White : Colors.Black
            };
            string captured = cat;
            btn.Clicked += (s, e) =>
            {
                _editCategoryFilter = captured;
                SoundService.PlayClick2();
                BuildEditFilterButtons();
                ApplyEditFilter();
            };
            EditFilterRow.Children.Add(btn);
        }
    }

    private void ApplyEditFilter()
    {
        FilteredEditIngredients.Clear();
        foreach (var vm in SelectableIngredients)
        {
            if (_editCategoryFilter == "All" || vm.Category == _editCategoryFilter)
                FilteredEditIngredients.Add(vm);
        }
    }

    private void UpdateAnalysis()
    {
        try
        {
            if (_masterData == null)
            {
                Debug.WriteLine("Warning: UpdateAnalysis called with null _masterData");
                return;
            }

            var currentSelectedIds = SelectableIngredients
                .Where(i => i.IsSelected)
                .Select(i => i.Id)
                .ToList();

            var tempRecipe = new Recipe { IngredientIds = currentSelectedIds };

            double totalCost = RecipeService.CalculateTotalCost(tempRecipe, _masterData);
            float totalScore = RecipeService.CalculateTotalScore(tempRecipe, _masterData);

            var myIngredients = _masterData.Where(i => currentSelectedIds.Contains(i.Id)).ToList();
            float baseAverage = myIngredients.Any() ? (float)myIngredients.Average(i => i.BasePopularity) : 0;
            float synergyOnly = totalScore - baseAverage;

            CostLabel.Text = totalCost.ToString("N2");
            SynergyLabel.Text = synergyOnly > 0 ? $"+{synergyOnly:F0}" : "0";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in UpdateAnalysis: {ex.Message}");
        }
    }

    private void UpdateAllowButtonVisual(bool isAllowed)
    {
        try
        {
            var loc = LocalizationService.Instance;
            if (isAllowed)
            {
                AllowButton.Text = loc.AllowEnabled;
                AllowButton.BackgroundColor = Color.FromArgb("#76B041");
            }
            else
            {
                AllowButton.Text = loc.AllowDisabled;
                AllowButton.BackgroundColor = Color.FromArgb("#B22222");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in UpdateAllowButtonVisual: {ex.Message}");
        }
    }
    #endregion

    #region --- UI Events ---
    private void OnIngredientToggled(object sender, TappedEventArgs e)
    {
        if (e.Parameter is IngredientViewModel vm)
        {
            SoundService.PlayClick1();
            vm.IsSelected = !vm.IsSelected;
            UpdateAnalysis();
        }
    }

    private async void OnEditNameClicked(object sender, EventArgs e)
    {
        SoundService.PlayClick1();
        var loc = LocalizationService.Instance;
        string result = await DisplayPromptAsync(loc.EditNameTitle, loc.EditNameMsg, initialValue: RecipeName);
        if (!string.IsNullOrWhiteSpace(result))
        {
            RecipeName = result;
        }
    }

    private async void OnAllowToMenuClicked(object sender, EventArgs e)
    {
        if (_recipeToEdit == null)
        {
            SoundService.PlayClick1();
            _isNewRecipeAllowed = !_isNewRecipeAllowed;
            UpdateAllowButtonVisual(_isNewRecipeAllowed);
        }
        else
        {
            SoundService.PlayClick2();
            _recipeToEdit.IsAllowed = !_recipeToEdit.IsAllowed;
            UpdateAllowButtonVisual(_recipeToEdit.IsAllowed);
        }
    }
    #endregion

    #region --- Accept Button ---
    private async void OnAcceptClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        var selectedIds = SelectableIngredients.Where(i => i.IsSelected).Select(s => s.Id).ToList();

        if (selectedIds.Count == 0)
        {
            var loc = LocalizationService.Instance;
            await PopupPage.ShowInfo(this, "⚠️", loc.EditWarnIngTitle, loc.EditWarnIngMsg);
            btn.IsEnabled = true;
            return;
        }

        var hasMeat = _masterData
        .Where(i => selectedIds.Contains(i.Id))
        .Any(i => i.Category == "Meat");

        if (!hasMeat)
        {
            SoundService.PlayClickF();
            var loc = LocalizationService.Instance;
            await PopupPage.ShowInfo(this, "🥩", loc.EditWarnMeatTitle, loc.EditWarnMeatMsg);
            btn.IsEnabled = true;
            return;
        }

        var tempRecipe = new Recipe { IngredientIds = selectedIds };
        double baseCost = RecipeService.CalculateTotalCost(tempRecipe, _masterData);

        #region --- Create New Mode ---
        if (_recipeToEdit == null)
        {
            if (_player.CreatedRecipes == null) _player.CreatedRecipes = new List<Recipe>();

            int maxId = _player.CreatedRecipes.Any() ? _player.CreatedRecipes.Max(r => r.Id) : 0;

            var newR = new Recipe
            {
                Id = maxId + 1,
                Name = RecipeName,
                IngredientIds = selectedIds,
                SellingPrice = (int)baseCost, // Set the default price based on the cost
                IsAllowed = _isNewRecipeAllowed
            };

            SoundService.PlayClick1();
            WeakReferenceMessenger.Default.Send(new NewRecipeMessage(newR));
        }
        #endregion

        #region --- Edit Mode ---
        else
        {
            bool hasStock = _player.RecipeInventory != null &&
                            _player.RecipeInventory.ContainsKey(_recipeToEdit.Id) &&
                            _player.RecipeInventory[_recipeToEdit.Id] > 0;

            if (hasStock)
            {
                SoundService.PlayClickF();
                var loc = LocalizationService.Instance;
                await PopupPage.ShowInfo(this, "🔒", loc.EditCannotEditTitle, loc.EditCannotEditMsg);
                btn.IsEnabled = true;
                return;
            }

            _recipeToEdit.Name = RecipeName;
            _recipeToEdit.IngredientIds = selectedIds;
            _recipeToEdit.SellingPrice = (int)baseCost;

            SoundService.PlayClick1();
            WeakReferenceMessenger.Default.Send(new RecipeUpdatedMessage(_recipeToEdit));
        }
        #endregion

        await Navigation.PopModalAsync();
    }
    #endregion

    #region --- Delete Button ---
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;

        if (_recipeToEdit == null)
        {
            await Navigation.PopModalAsync();
            return;
        }

        bool hasRemainingStock = _player.RecipeInventory != null &&
                                 _player.RecipeInventory.ContainsKey(_recipeToEdit.Id) &&
                                 _player.RecipeInventory[_recipeToEdit.Id] > 0;

        if (hasRemainingStock)
        {
            SoundService.PlayClickF();
            var loc = LocalizationService.Instance;
            await PopupPage.ShowInfo(this, "🔒", loc.EditCannotDelTitle, loc.EditCannotDelMsg);
            btn.IsEnabled = true;
            return;
        }

        SoundService.PlayDelete();
        WeakReferenceMessenger.Default.Send(new RecipeDeletedMessage(_recipeToEdit));
        await Navigation.PopModalAsync();
    }
    #endregion

    #region --- Cancel Button ---
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClickB();
        await Navigation.PopModalAsync();
    }
    #endregion
}