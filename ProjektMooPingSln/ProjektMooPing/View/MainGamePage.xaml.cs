using CommunityToolkit.Mvvm.Messaging;
using ProjektMooPing.Models;
using ProjektMooPing.Services;
using ProjektMooPing.View;
using ProjektMooPing.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Location = ProjektMooPing.Models.Location;

namespace ProjektMooPing
{
	public partial class MainGamePage : ContentPage
	{
        #region --- Properties & Variables ---
        public ObservableCollection<RecipeViewModel> MyRecipe { get; set; } = new();
        public ObservableCollection<IngredientViewModel> SelectableIngredients { get; set; } = new();
        public ObservableCollection<IngredientViewModel> MyInventory { get; set; } = new();
        public ObservableCollection<ProjektMooPing.Models.Location> AllLocations { get; set; } = new();
        public ObservableCollection<RecipeViewModel> MyMenu { get; set; } = new();
        public List<Ingredient> AllIngredients { get; set; } = new();
        public List<StoryCutScene> AllStoryCutScenes { get; set; } = new();
        public PlayerProfile Player { get; set; }
        public List<QuestionData> AllQuestions { get; set; } = new();
        private Location _currentLocation;
        private int _timeScale = 1;
        private bool _isBusy = false;
        private bool _isNavigating = false;
        private string _invCategoryFilter = "All";
        public Location CurrentLocation
        {
            get => _currentLocation;
            set
            {
                _currentLocation = value;
                OnPropertyChanged(nameof(CurrentLocation));

                if (_currentLocation != null)
                {
                    OnPropertyChanged(nameof(CurrentGameStateImage));
                }
            }
        }
        public string CurrentGameStateImage => CurrentLocation?.ImagePath ?? "wall_rural.png";
        #endregion

        #region --- Initialization ---
        private bool _initialized = false;

        public MainGamePage()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<NewRecipeMessage>(this, (r, m) => {
                Player.CreatedRecipes.Add(m.Value);
                RefreshMenuUI();
                RefreshInventoryUI();
                RefreshRecipeUI();
                SaveCurrentGame();
            });

            WeakReferenceMessenger.Default.Register<RecipeUpdatedMessage>(this, (r, m) => {
                RefreshMenuUI();
                RefreshInventoryUI();
                RefreshRecipeUI();
                SaveCurrentGame();
            });

            WeakReferenceMessenger.Default.Register<RecipeDeletedMessage>(this, (r, m) =>
            {
                var recipeToRemove = Player.CreatedRecipes.FirstOrDefault(x => x.Id == m.Value.Id);
                if (recipeToRemove != null)
                {
                    Player.CreatedRecipes.Remove(recipeToRemove);
                }
                RefreshMenuUI();
                RefreshInventoryUI();
                RefreshRecipeUI();
                SaveCurrentGame();
            });

            WeakReferenceMessenger.Default.Register<ResetGameMessage>(this, (r, m) => {
                LoadGameData();
                RefreshMenuUI();
                RefreshInventoryUI();
                RefreshRecipeUI();
            });

            WeakReferenceMessenger.Default.Register<AddRatingMessage>(this, (r, m) => {
                Player.TotalRating = Math.Clamp(Player.TotalRating + m.Amount, 0, RatingService.MaxTotalRating);
                TotalRatingStarsLabel.Text = RatingService.GetTotalStarDisplay(Player.TotalRating);
                OnPropertyChanged(nameof(Player));
                SaveCurrentGame();
            });

            WeakReferenceMessenger.Default.Register<AddMoneyMessage>(this, (r, m) => {
                Player.Money += m.Amount;
                OnPropertyChanged(nameof(Player));
                SaveCurrentGame();
            });

            WeakReferenceMessenger.Default.Register<IngredientUnlockedMessage>(this, (r, m) => {
                RefreshInventoryUI();
                SaveCurrentGame();
            });

            // รีเฟรช UI ทุกส่วนเมื่อผู้ใช้สลับภาษา (ชื่อวัตถุดิบ / สูตร / สถานที่จะเปลี่ยน)
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                RefreshMenuUI();
                RefreshInventoryUI();
                RefreshRecipeUI();
                // บังคับ CarouselView รีโหลดข้อมูลสถานที่ใหม่
                var savedLocs = AllLocations.ToList();
                AllLocations.Clear();
                foreach (var loc in savedLocs) AllLocations.Add(loc);
            };

            MenuTab.IsVisible = true;
            EditTab.IsVisible = false;
            InvTab.IsVisible = false;
            LocTab.IsVisible = false;
            this.BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized) return;
            _initialized = true;

            Debug.WriteLine(">>>> SAVE FILE IS HERE: " + Path.Combine(FileSystem.AppDataDirectory, "mooping_save.json"));
            await LoadMasterData();
            LoadGameData();
        }
        #endregion

        #region --- Gameplay Animations ---
        private async void StartCustomerAnimation(object sender, EventArgs e)
        {
            if (_isBusy)
            {
                SoundService.PlayClickF();
                return;
            }

            var btn = (Button)sender;

            if (!await CheckLoanRepayment())
                return;

            if (!await CheckContractValid())
                return;

            btn.IsEnabled = false;
            SoundService.PlayOpen();
            SoundService.PlayBGM();
            SmokeWrapper.IsVisible = true;
            _ = SmokeWrapper.FadeTo(1, 800);
            Player.Day++;
            var daySummary = new DaySummary { DayNumber = Player.Day };
            RefreshMenuUI();
            RefreshInventoryUI();
            RefreshRecipeUI();
            OnPropertyChanged(nameof(Player));

            // --- ลัคกี้แห่งความอดทน: stock +3 ต่อสูตรที่มีอยู่ ---
            if (BuffService.HasEnduranceBuff(Player))
            {
                foreach (var key in Player.RecipeInventory.Keys.ToList())
                    if (Player.RecipeInventory[key] > 0)
                        Player.RecipeInventory[key] += 3;
            }

            // --- Random Customer Count ---
            Random rnd = new Random();
            int totalInThisRound = rnd.Next(CurrentLocation.MinTraffic, CurrentLocation.MaxTraffic + 1);

            // --- ลัคกี้แห่งการโหยหา: ลูกค้าประจำ +3 คน ---
            if (BuffService.HasLongingBuff(Player)) totalInThisRound += 3;

            // --- Rating Tracking ---
            int customersServed = 0;
            bool hadStockout = false;

            try
            {
                _isBusy = true;
                // --- Loop ---
                for (int i = 1; i <= totalInThisRound; i++)
                {
                    // Reset
                    CustomerSprite.TranslationX = 600;
                    CustomerSprite.Opacity = 1;

                    // Walk in
                    await CustomerSprite.TranslateToAsync(80, 0, (uint)(2000 / _timeScale), Easing.CubicOut);

                    // Considering Logic
                    await SpeechBubble.FadeTo(1, (uint)(200 / _timeScale));

                    var result = await CheckSaleLogic(i, totalInThisRound);
                    await Task.Delay(400 / _timeScale);

                    if (result.Earnings > 0)
                    {
                        customersServed++; // นับลูกค้าที่ซื้อ
                        SoundService.PlayCoin();
                        OnPropertyChanged(nameof(Player));
                        RefreshMenuUI();
                        RefreshInventoryUI();
                        RefreshRecipeUI();
                        CustomerChatLabel.Text = $"{result.Summary} Total: {result.Earnings}";
                        await Task.Delay(1000 / _timeScale);
                    }
                    else
                    {
                        SoundService.PlayHmm();
                        CustomerChatLabel.Text = "❌";
                        await Task.Delay(600 / _timeScale);
                    }

                    daySummary.TotalRevenue += result.Earnings;

                    foreach (var item in result.Details)
                    {
                        daySummary.TotalCost += (item.UnitCost * item.Quantity);

                        var existing = daySummary.SalesDetails.FirstOrDefault(x => x.MenuName == item.MenuName);
                        if (existing != null)
                        {
                            existing.Quantity += item.Quantity;
                            existing.Revenue += item.Revenue;
                        }
                        else
                            daySummary.SalesDetails.Add(item);
                    }

                    await SpeechBubble.FadeTo(0, (uint)(100 / _timeScale));
                    // Walk out (เดินออกพ้นจอทางซ้ายสุด)
                    await CustomerSprite.TranslateToAsync(-700, 0, (uint)(2000 / _timeScale), Easing.CubicIn);

                    CustomerChatLabel.Text = "🤔";

                    if (!IsAnythingLeftToSell())
                    {
                        hadStockout = (i < totalInThisRound); // ยังมีลูกค้าค้างแต่ของหมด
                        var loc = LocalizationService.Instance;
                        await PopupPage.ShowInfo(this, "📦", loc.PopupOutOfStockTitle, loc.PopupOutOfStockMsg);
                        break; // ปิดร้าน รวย กลับบ้าน
                    }

                    // Pause
                    await Task.Delay(500 / _timeScale);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in customer animation: " + ex.Message);
            }
            finally
            {
                // --- คำนวณ Rating ---
                float avgQuality = CalcAvgQualityFromSales(daySummary);

                daySummary.CustomersServed  = customersServed;
                daySummary.TotalCustomers   = totalInThisRound;
                daySummary.HadStockout      = hadStockout;
                daySummary.AvgQualityScore  = avgQuality;

                // --- Buff flags สำหรับ Rating ---
                bool hasResilience = BuffService.HasResilienceBuff(Player);
                bool hasSuccess    = BuffService.HasSuccessBuff(Player);
                bool hasEuphoria   = BuffService.HasEuphoriaBuff(Player);

                daySummary.ProfitScore  = RatingService.CalcProfitScore(daySummary.TotalProfit);
                // ลัคกี้แห่งความสำเร็จ: Profit Score ×2
                if (hasSuccess) daySummary.ProfitScore *= 2;

                daySummary.SalesScore    = RatingService.CalcSalesScore(customersServed, totalInThisRound);
                daySummary.QualityScore  = RatingService.CalcQualityScore(avgQuality);
                // ลัคกี้แห่งความทรหด: ยกเว้นโทษขาดสต็อก
                daySummary.StockoutPenalty = hasResilience ? 0 : RatingService.CalcStockoutPenalty(hadStockout);

                daySummary.DailyRating = daySummary.ProfitScore + daySummary.SalesScore
                                       + daySummary.QualityScore - daySummary.StockoutPenalty;
                // ลัคกี้แห่งความหรรษา: Daily Rating ×1.2
                if (hasEuphoria) daySummary.DailyRating = (int)(daySummary.DailyRating * 1.2f);
                daySummary.StarDisplay      = RatingService.GetDailyStarDisplay(daySummary.DailyRating);

                // อัปเดต TotalRating
                int prevRating = Player.TotalRating;
                Player.TotalRating = Math.Clamp(
                    Player.TotalRating + daySummary.DailyRating,
                    0, RatingService.MaxTotalRating);
                daySummary.NewTotalRating = Player.TotalRating;

                // อัปเดต TopBar ดาว
                TotalRatingStarsLabel.Text = RatingService.GetTotalStarDisplay(Player.TotalRating);

                _isBusy = false;
                await SmokeWrapper.FadeTo(0, 1000);
                SmokeWrapper.IsVisible = false;
                RefreshMenuUI();
                RefreshInventoryUI();
                RefreshRecipeUI();
                OnPropertyChanged(nameof(Player));
                await Navigation.PushModalAsync(new DaySummaryPage(daySummary));
                SoundService.PlayClose();
                SoundService.StopBgm();
                SaveCurrentGame();

                // 2.3 True Ending
                if (Player.ContractLocationId == 10
                    && prevRating < RatingService.MaxTotalRating
                    && Player.TotalRating >= RatingService.MaxTotalRating)
                {
                    var endingScene = AllStoryCutScenes.FirstOrDefault(s => s.Id == 14);
                    if (endingScene != null)
                        await Navigation.PushModalAsync(new CutScenePage(new List<StoryCutScene> { endingScene }));
                }

                btn.IsEnabled = true;
            }
        }

        // --- คำนวณ Quality Score เฉลี่ยจากเมนูที่ขายได้วันนี้ ---
        private float CalcAvgQualityFromSales(DaySummary summary)
        {
            if (!summary.SalesDetails.Any()) return 0f;

            var scores = summary.SalesDetails
                .Select(s => Player.CreatedRecipes.FirstOrDefault(r => r.Name == s.MenuName))
                .Where(r => r != null)
                .Select(r => RecipeService.CalculateTotalScore(r, AllIngredients))
                .ToList();

            return scores.Any() ? (float)scores.Average() : 0f;
        }

        private void OnSpeedUpClicked(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            if (_timeScale == 1) _timeScale = 2;
            else if (_timeScale == 2) _timeScale = 4;
            else _timeScale = 1;

            btn.Text = LocalizationService.Instance.FmtSpeed(_timeScale);
            SoundService.PlayClick1();
        }
        #endregion

        #region --- Main Game ---
        public class SaleResult
        {
            public string Summary { get; set; }
            public double Earnings { get; set; }
            public List<MenuSalesStats> Details { get; set; } = new();
        }

        private async Task<SaleResult> CheckSaleLogic(int currentNumber, int total)
        {
            var activeMenus = Player.CreatedRecipes.Where(r => r.IsAllowed).ToList();
            if (activeMenus.Count == 0)
                return new SaleResult { Summary = "", Earnings = 0 };

            var result = new SaleResult();
            Random rnd = new Random();
            string orderSummary = "";
            double totalEarnings = 0;

            // --- Buff flags ---
            bool hasMischief    = BuffService.HasMischiefBuff(Player);
            bool hasNegotiation = BuffService.HasNegotiationBuff(Player);
            bool hasCivilization = BuffService.HasCivilizationBuff(Player);
            bool hasLettingGo   = BuffService.HasLettingGoBuff(Player);
            double costMultiplier = hasCivilization ? 0.9 : 1.0;

            // Select Menu(s)
            var availableOrderPool = activeMenus.OrderBy(x => Guid.NewGuid()).ToList();
            int varietyCount = Math.Min(availableOrderPool.Count, rnd.Next(1, 4));

            for (int v = 0; v < varietyCount; v++)
            {
                var recipe = availableOrderPool[v];
                // Random Quantity
                float basePop = RecipeService.CalculatePopularity(recipe, recipe.SellingPrice, AllIngredients, hasLettingGo, costMultiplier);

                int maxWant = rnd.Next(1, 6);
                int actualSoldInThisMenu = 0;

                for (int q = maxWant; q >= 1; q--)
                {
                    // basePop * (1.1 - (q * 0.1))
                    float difficultyMult = 1.1f - (q * 0.1f);
                    float finalChance = basePop * difficultyMult;

                    if (rnd.Next(1, 101) <= finalChance)
                    {
                        actualSoldInThisMenu = q;
                        break;
                    }
                }

                // ลัคกี้แห่งความซุกซน: การันตีซื้อขั้นต่ำ 1 ไม้ (เฉพาะเมื่อ Popularity > 0)
                if (actualSoldInThisMenu == 0 && hasMischief && basePop > 0)
                    actualSoldInThisMenu = 1;

                if (actualSoldInThisMenu > 0)
                {
                    int stockAvailable = Player.RecipeInventory.ContainsKey(recipe.Id) ? Player.RecipeInventory[recipe.Id] : 0;
                    int finalSold = Math.Min(actualSoldInThisMenu, stockAvailable);

                    if (finalSold > 0)
                    {
                        Player.RecipeInventory[recipe.Id] -= finalSold;

                        // ลัคกี้แห่งการเจรจา: 25% โอกาส +1 ไม้ฟรี
                        if (hasNegotiation && rnd.Next(1, 101) <= 25)
                        {
                            int remaining = Player.RecipeInventory.ContainsKey(recipe.Id) ? Player.RecipeInventory[recipe.Id] : 0;
                            if (remaining > 0)
                            {
                                Player.RecipeInventory[recipe.Id]--;
                                finalSold++;
                            }
                        }

                        double subTotal = finalSold * recipe.SellingPrice;
                        double unitCost = RecipeService.CalculateTotalCost(recipe, AllIngredients);

                        totalEarnings += subTotal;
                        orderSummary += $"{recipe.Name} {finalSold} Unit(s)\n";

                        result.Details.Add(new MenuSalesStats
                        {
                            MenuName = recipe.Name,
                            Quantity = finalSold,
                            Revenue = subTotal,
                            UnitCost = unitCost
                        });
                    }
                }
            }

            if (totalEarnings > 0)
            {
                Player.Money += totalEarnings;
                SaveCurrentGame();
            }

            result.Summary = orderSummary;
            result.Earnings = totalEarnings;

            return result;
        }

        private bool IsAnythingLeftToSell()
        {
            // Check if Everything is Out of Stock
            return Player.RecipeInventory.Any(item => item.Value > 0);
        }
        #endregion

        #region --- Tab Menu ---
        private void UpdateTabVisuals(string activeTab)
        {
            var darkColor = (Color)Application.Current.Resources["MooPingDarkBrown"];
            var mediumColor = (Color)Application.Current.Resources["MooPingMediumBrown"];

            MenuTabBtn.BackgroundColor = mediumColor;
            EditTabBtn.BackgroundColor = mediumColor;
            InvTabBtn.BackgroundColor = mediumColor;
            LocTabBtn.BackgroundColor = mediumColor;

            switch (activeTab)
            {
                case "Menu": MenuTabBtn.BackgroundColor = darkColor; break;
                case "Edit": EditTabBtn.BackgroundColor = darkColor; break;
                case "Inv": InvTabBtn.BackgroundColor = darkColor; break;
                case "Loc": LocTabBtn.BackgroundColor = darkColor; break;
            }
        }
        private void OnMenuClicked(object sender, EventArgs e)
        {
            SoundService.PlayPaper();
            RefreshMenuUI();
            UpdateTabVisuals("Menu");
            MenuTab.IsVisible = true;
            EditTab.IsVisible = false;
            InvTab.IsVisible = false;
            LocTab.IsVisible = false;
        }
        private void OnEditClicked(object sender, EventArgs e)
        {
            SoundService.PlayPaper();
            RefreshRecipeUI();
            UpdateTabVisuals("Edit");
            MenuTab.IsVisible = false;
            EditTab.IsVisible = true;
            InvTab.IsVisible = false;
            LocTab.IsVisible = false;
        }
        private void OnInvClicked(object sender, EventArgs e)
        {
            SoundService.PlayPaper();
            UpdateTabVisuals("Inv");
            MenuTab.IsVisible = false;
            EditTab.IsVisible = false;
            InvTab.IsVisible = true;
            LocTab.IsVisible = false;
            BuildInvFilterButtons();
            RefreshInventoryUI();
        }
        private void OnLocClicked(object sender, EventArgs e)
        {
            SoundService.PlayPaper();
            UpdateTabVisuals("Loc");
            MenuTab.IsVisible = false;
            EditTab.IsVisible = false;
            InvTab.IsVisible = false;
            LocTab.IsVisible = true;
            UpdateLocTabUI();
        }
        #endregion

        #region --- Save Game Data ---
        private void SaveCurrentGame()
        {
            if (Player != null)
            {
                SaveService.SaveGame(Player);
                Console.WriteLine(">>>> Game Saved Successfully!");
            }
        }
        public class MasterDataRoot
        {
            public List<ProjektMooPing.Models.Location> Locations { get; set; }
            public List<Ingredient> Ingredients { get; set; }
            public List<QuestionData> Quizzes { get; set; }
            public List<StoryCutScene> StoryCutScene { get; set; }
        }
        #endregion

        #region --- Load Game Data ---
        public async void LoadGameData()
        {
            try
            {
                // Open MasterData.json from the app (Resources/Raw)
                using var stream = await FileSystem.OpenAppPackageFileAsync("MasterData.json");
                using var reader = new StreamReader(stream);
                var contents = await reader.ReadToEndAsync();
                var data = JsonSerializer.Deserialize<MasterDataRoot>(contents, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data != null)
                {
                    // Locations
                    if (data.Locations != null)
                    {
                        AllLocations.Clear();
                        foreach (var loc in data.Locations)
                        {
                            if (!loc.ImagePath.EndsWith(".png")) loc.ImagePath += ".png";
                            AllLocations.Add(loc);
                        }
                    }

                    // Ingredients
                    if (data.Ingredients != null)
                        AllIngredients = data.Ingredients;

                    // StoryCutScenes
                    if (data.StoryCutScene != null)
                        AllStoryCutScenes = data.StoryCutScene;
                }

                Player = SaveService.LoadGame();
                Player.IngredientInventory ??= new Dictionary<int, int>();
                Player.RecipeInventory ??= new Dictionary<int, int>();
                Player.CreatedRecipes ??= new List<Recipe>();

                // backward-compat: save ก่อนมีระบบสัญญา
                Player.UnlockedLocationIds ??= new HashSet<int> { 1 };
                if (Player.UnlockedLocationIds.Count == 0) Player.UnlockedLocationIds.Add(1);
                if (Player.ContractLocationId == 0)  Player.ContractLocationId = 1;
                if (Player.ContractExpiryDay  == 0)  Player.ContractExpiryDay  = Player.Day + 7;

                string savePath = Path.Combine(FileSystem.AppDataDirectory, "mooping_save.json");
                if (!File.Exists(savePath))
                {
                    SaveCurrentGame();
                    Debug.WriteLine(">>>> Initial Save File Created!");
                }

                CurrentLocation = AllLocations.FirstOrDefault(l => l.Id == Player.ContractLocationId)
                               ?? AllLocations.FirstOrDefault()
                               ?? new Location();
                BuildInvFilterButtons();
                RefreshInventoryUI();
                RefreshRecipeUI();
                RefreshMenuUI();
                OnPropertyChanged(nameof(Player));
                UpdateLoanBanner();

                // 2.1 Intro cutscene (แสดงครั้งเดียว)
                if (!Player.AlreadyWatchIntro)
                {
                    Player.AlreadyWatchIntro = true;
                    SaveCurrentGame();
                    var introScenes = AllStoryCutScenes
                        .Where(s => s.Id >= 11 && s.Id <= 13)
                        .OrderBy(s => s.Id).ToList();
                    if (introScenes.Any())
                        await Navigation.PushModalAsync(new CutScenePage(introScenes));
                }
            }
            catch (Exception ex)
            {
                var loc = LocalizationService.Instance;
                await PopupPage.ShowInfo(this, "❌", loc.PopupErrorTitle, loc.FmtLoadError(ex.Message));
            }
        }
        #endregion

        #region --- Menu Logic ---
        private async void OnMenuTapped(object sender, TappedEventArgs e)
        {
            if (_isNavigating) return;
            if (e.Parameter is not RecipeViewModel selectedVM) return;
            _isNavigating = true;
            try
            {
                SoundService.PlayClick1();
                await Navigation.PushModalAsync(new MenuDetailPage(selectedVM.RecipeSource, AllIngredients, Player));
            }
            finally { _isNavigating = false; }
        }
        public void RefreshMenuUI()
        {
            MyMenu.Clear();
            foreach (var recipe in Player.CreatedRecipes)
            {
                if (recipe.IsAllowed)
                {
                    var vm = new RecipeViewModel(recipe, AllIngredients);
                    if (Player.RecipeInventory.TryGetValue(recipe.Id, out int count))
                    {
                        vm.FinishedGoods = count;
                    }

                    MyMenu.Add(vm);
                }
            }
        }
        #endregion

        #region --- Edit Recipe Logic ---
        public void RefreshRecipeUI()
        {
            MyRecipe.Clear();
            foreach (var recipe in Player.CreatedRecipes)
            {
                var vm = new RecipeViewModel(recipe, AllIngredients);
                MyRecipe.Add(vm);
            }
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                SoundService.PlayClick1();
                await Navigation.PushModalAsync(new EditDetailPage(Player, AllIngredients));
            }
            finally { _isNavigating = false; }
        }

        private async void OnRecipeTapped(object sender, TappedEventArgs e)
        {
            if (_isNavigating) return;
            if (e.Parameter is not RecipeViewModel vm) return;
            _isNavigating = true;
            try
            {
                SoundService.PlayClick1();
                await Navigation.PushModalAsync(new EditDetailPage(Player, AllIngredients, vm.RecipeSource));
            }
            finally { _isNavigating = false; }
        }
        #endregion

        #region --- Inventory ---
        public void RefreshInventoryUI()
        {
            MyInventory.Clear();
            foreach (var id in Player.UnlockedIngredientIds)
            {
                var master = AllIngredients.FirstOrDefault(i => i.Id == id);
                if (master == null) continue;
                if (_invCategoryFilter != "All" && master.Category != _invCategoryFilter) continue;

                int owned = Player.IngredientInventory.ContainsKey(id) ? Player.IngredientInventory[id] : 0;
                MyInventory.Add(new IngredientViewModel
                {
                    Id = master.Id,
                    Name = master.Name,
                    NameTh = master.NameTh,
                    Category = master.Category,
                    BasePopularity = master.BasePopularity,
                    BaseCost = master.BaseCost,
                    Icon = master.Icon,
                    OwnedAmount = owned,
                    OrderAmount = 0
                });
            }
        }

        private void BuildInvFilterButtons()
        {
            if (AllIngredients == null || InvFilterRow == null) return;
            InvFilterRow.Children.Clear();
            var L = LocalizationService.Instance;
            var activeColor   = (Color)Application.Current.Resources["MooPingLightBrown"];
            var inactiveColor = (Color)Application.Current.Resources["MooPingCream"];

            var categories = new[] { "All" }
                .Concat(AllIngredients
                    .Where(i => Player.UnlockedIngredientIds.Contains(i.Id))
                    .Select(i => i.Category).Distinct().OrderBy(c => c))
                .ToList();

            foreach (var cat in categories)
            {
                bool isActive = cat == _invCategoryFilter;
                var btn = new Button
                {
                    Text = cat == "All" ? (L.IsThai ? "ทั้งหมด" : "All") : cat,
                    HeightRequest = 30,
                    Padding = new Thickness(10, 0),
                    CornerRadius = 15,
                    FontSize = 12,
                    BackgroundColor = isActive ? activeColor : inactiveColor,
                    TextColor = Colors.Black
                };
                string captured = cat;
                btn.Clicked += (s, e) =>
                {
                    _invCategoryFilter = captured;
                    SoundService.PlayClick2();
                    BuildInvFilterButtons();
                    RefreshInventoryUI();
                };
                InvFilterRow.Children.Add(btn);
            }
        }

        private void OnAdjustOrder(object sender, EventArgs e)
        {
            SoundService.PlayClick2();
            var btn = (Button)sender;
            var item = (IngredientViewModel)btn.BindingContext;
            int amount = int.Parse(btn.Text); // "+1" "-5"
            item.OrderAmount += amount;
        }

        private async void OnBuyClicked(object sender, EventArgs e)
        {
            var item = (IngredientViewModel)((Button)sender).BindingContext;
            if (item.OrderAmount <= 0) return;

            if (Player.Money >= item.TotalCost)
            {
                SoundService.PlayCashRegister();
                Player.Money -= item.TotalCost;

                // Add Ingredient Inventory
                if (!Player.IngredientInventory.ContainsKey(item.Id))
                    Player.IngredientInventory[item.Id] = 0;

                Player.IngredientInventory[item.Id] += item.OrderAmount;

                // อัปเดต UI
                item.OwnedAmount = Player.IngredientInventory[item.Id];
                item.OrderAmount = 0;

                OnPropertyChanged(nameof(Player)); // Money Update
                SaveCurrentGame();
            }
            else
            {
                SoundService.PlayClickF();
                // await DisplayAlert("Failed", "Not Enough Money", "OK");
            }
        }
        #endregion

        #region --- Location ---

        private void OnLocationPositionChanged(object sender, PositionChangedEventArgs e)
        {
            UpdateLocTabUI(e.CurrentPosition);
        }

        private void OnPrevLocationClicked(object sender, EventArgs e)
        {
            SoundService.PlayClick2();
            int prev = LocationCarousel.Position - 1;
            if (prev >= 0) LocationCarousel.Position = prev;
        }

        private void OnNextLocationClicked(object sender, EventArgs e)
        {
            SoundService.PlayClick2();
            int next = LocationCarousel.Position + 1;
            if (next < AllLocations.Count) LocationCarousel.Position = next;
        }

        private void UpdateLocTabUI(int? position = null)
        {
            if (Player == null || AllLocations.Count == 0) return;

            int idx = position ?? LocationCarousel.Position;
            if (idx < 0 || idx >= AllLocations.Count) return;
            var loc = AllLocations[idx];

            var L = LocalizationService.Instance;

            LocNameLabel.Text    = loc.DisplayName;
            LocTrafficLabel.Text = loc.TrafficRangeDisplay;
            LocRentLabel.Text    = loc.WeeklyRentDisplay;
            LocRatingLabel.Text  = loc.RequiredRating == 0 ? L.LblFree : $"{loc.RequiredRating:N0}";

            bool isContracted    = (loc.Id == Player.ContractLocationId);
            bool isActive        = isContracted && Player.Day < Player.ContractExpiryDay;
            bool alreadyUnlocked = Player.UnlockedLocationIds.Contains(loc.Id);
            bool isDowngrade     = loc.Id < Player.ContractLocationId;

            // #5 ตรวจว่าปลดล็อคที่ก่อนหน้าครบหรือยัง (เฉพาะที่ยังไม่เคยปลดล็อค)
            bool sequentialOk = alreadyUnlocked
                || AllLocations.Where(l => l.Id < loc.Id).All(l => Player.UnlockedLocationIds.Contains(l.Id));

            // #3 rating ตรวจเฉพาะครั้งแรก
            bool ratingOk = alreadyUnlocked || Player.TotalRating >= loc.RequiredRating;

            // ค่าใช้จ่าย: UnlockCost เฉพาะครั้งแรก + WeeklyRent เสมอ
            int signCost = loc.WeeklyRent + (alreadyUnlocked ? 0 : loc.UnlockCost);
            LocCostLabel.Text = signCost == 0 ? L.LblFree : $"{signCost:N0}฿";

            // สถานะสัญญา
            if (isActive)
            {
                LocStatusLabel.Text      = L.FmtContractExpiry(Player.ContractExpiryDay);
                LocStatusLabel.TextColor = Color.FromArgb("#76B041");
            }
            else if (isContracted)
            {
                LocStatusLabel.Text      = L.LblContractExpired;
                LocStatusLabel.TextColor = Color.FromArgb("#CC3333");
            }
            else
            {
                LocStatusLabel.Text      = alreadyUnlocked ? (L.IsThai ? "ปลดล็อคแล้ว" : "Unlocked") : "";
                LocStatusLabel.TextColor = Color.FromArgb("#888888");
            }

            // ปุ่มเซนสัญญา — ตรวจตามลำดับความสำคัญ
            if (isActive)
            {
                SetContractBtn(L.BtnContractActive, false, "#AAAAAA");
            }
            else if (isDowngrade)                          // #6 ถอยหลังไม่ได้
            {
                SetContractBtn(L.LblCannotDowngrade, false, "#AAAAAA");
            }
            else if (!sequentialOk)                        // #5 ต้องปลดล็อคก่อนหน้าก่อน
            {
                SetContractBtn(L.LblSequentialLocked, false, "#AAAAAA");
            }
            else if (!ratingOk)                            // #3 rating เฉพาะครั้งแรก
            {
                SetContractBtn(L.LblLockedRating, false, "#AAAAAA");
            }
            else if (Player.Money < signCost)
            {
                SetContractBtn(L.IsThai ? "💸 เงินไม่พอ" : "💸 Not Enough", false, "#AAAAAA");
            }
            else
            {
                SetContractBtn(isContracted ? L.BtnContractRenew : L.BtnSignContract, true, "#76B041");
            }
        }

        private void UpdateLoanBanner()
        {
            if (Player == null) return;
            LoanBanner.IsVisible = Player.HasLoan;
            if (!Player.HasLoan) return;
            var L = LocalizationService.Instance;
            LoanBannerLabel.Text = L.IsThai
                ? $"💸 หนี้: {Player.LoanAmount:N0}฿ | จ่ายวันที่ {Player.LoanRepayDay}"
                : $"💸 Debt: {Player.LoanAmount:N0}฿ | Due Day {Player.LoanRepayDay}";
        }

        private void SetContractBtn(string text, bool enabled, string hex)
        {
            SignContractBtn.Text            = text;
            SignContractBtn.IsEnabled       = enabled;
            SignContractBtn.BackgroundColor = Color.FromArgb(hex);
        }

        private async void OnSignContractClicked(object sender, EventArgs e)
        {
            if (_isBusy || _isNavigating) { SoundService.PlayClickF(); return; }
            _isNavigating = true;
            try { await DoSignContract(); }
            finally { _isNavigating = false; }
        }

        private async Task DoSignContract()
        {

            int idx = LocationCarousel.Position;
            if (idx < 0 || idx >= AllLocations.Count) return;
            var selectedLoc = AllLocations[idx];

            var L = LocalizationService.Instance;

            // #6 ถอยหลังไม่ได้
            if (selectedLoc.Id < Player.ContractLocationId)
            {
                SoundService.PlayClickF();
                await PopupPage.ShowInfo(this, "⛔", L.PopupErrorTitle, L.LblCannotDowngrade);
                return;
            }

            bool alreadyUnlocked = Player.UnlockedLocationIds.Contains(selectedLoc.Id);

            // #5 ต้องปลดล็อคที่ก่อนหน้าครบก่อน
            bool sequentialOk = alreadyUnlocked
                || AllLocations.Where(l => l.Id < selectedLoc.Id).All(l => Player.UnlockedLocationIds.Contains(l.Id));
            if (!sequentialOk)
            {
                SoundService.PlayClickF();
                await PopupPage.ShowInfo(this, "🔒", L.PopupErrorTitle, L.LblSequentialLocked);
                return;
            }

            // #3 rating เฉพาะครั้งแรก
            if (!alreadyUnlocked && Player.TotalRating < selectedLoc.RequiredRating)
            {
                SoundService.PlayClickF();
                await PopupPage.ShowInfo(this, "⭐", L.PopupContractRatingTitle,
                    L.FmtContractRatingMsg(selectedLoc.RequiredRating, Player.TotalRating));
                return;
            }

            int signCost = selectedLoc.WeeklyRent + (alreadyUnlocked ? 0 : selectedLoc.UnlockCost);

            if (Player.Money < signCost)
            {
                SoundService.PlayClickF();
                await PopupPage.ShowInfo(this, "💸", L.PopupRentTitle,
                    L.PopupContractMoneyMsg(signCost));
                return;
            }

            // ตัดเงิน + อัปเดตสัญญา
            Player.Money -= signCost;
            Player.UnlockedLocationIds.Add(selectedLoc.Id);
            Player.ContractLocationId = selectedLoc.Id;
            Player.ContractExpiryDay  = Player.Day + 7;

            // เปลี่ยน background
            var bgImage = this.FindByName<Image>("GameBackgroundImage");
            await bgImage.FadeTo(0, 300);
            CurrentLocation = selectedLoc;
            SoundService.PlayMove();
            await bgImage.FadeTo(1, 500);

            OnPropertyChanged(nameof(Player));
            SaveCurrentGame();
            UpdateLocTabUI();

            await PopupPage.ShowInfo(this, "📋", L.IsThai ? "เซนสัญญาสำเร็จ!" : "Contract Signed!",
                L.FmtContractSigned(selectedLoc.DisplayName, Player.ContractExpiryDay));

            // 2.2 Location unlock cutscene (เฉพาะ unlock ครั้งแรก)
            if (!alreadyUnlocked && selectedLoc.HasStoryCutScene)
            {
                var scene = new StoryCutScene
                {
                    Title          = selectedLoc.Title,
                    TitleTh        = selectedLoc.TitleTh,
                    Text           = selectedLoc.Text,
                    TextTh         = selectedLoc.TextTh,
                    StoryImagePath = selectedLoc.StoryImagePath
                };

                var scenes = new List<StoryCutScene> { scene };

                // Scene 2: แสดง Buff ที่ได้รับจากลัคกี้นี้
                var (titleTh, titleEn, descTh, descEn) = BuffService.GetBuffDescription(selectedLoc.Id);
                if (!string.IsNullOrEmpty(titleTh))
                {
                    scenes.Add(new StoryCutScene
                    {
                        Title          = titleEn,
                        TitleTh        = titleTh,
                        Text           = descEn,
                        TextTh         = descTh,
                        StoryImagePath = selectedLoc.StoryImagePath
                    });
                }

                await Navigation.PushModalAsync(new CutScenePage(scenes));
            }
        }

        private async Task<bool> CheckContractValid()
        {
            if (Player.Day < Player.ContractExpiryDay) return true;

            // #2 สัญญาหมด — ลอง auto-renew
            var contractLoc = AllLocations.FirstOrDefault(l => l.Id == Player.ContractLocationId);
            var L = LocalizationService.Instance;

            if (contractLoc != null && Player.Money >= contractLoc.WeeklyRent)
            {
                Player.Money -= contractLoc.WeeklyRent;
                Player.ContractExpiryDay = Player.Day + 7;
                OnPropertyChanged(nameof(Player));
                SaveCurrentGame();
                await PopupPage.ShowInfo(this, "📋", L.LblAutoRenewTitle,
                    L.FmtAutoRenewMsg(contractLoc.DisplayName, Player.ContractExpiryDay));
                return true;
            }

            // เงินไม่พอ auto-renew — เรียกเฮียบิ๊ก
            if (contractLoc != null)
                return await TriggerLoanShark(contractLoc);

            SoundService.PlayClickF();
            await PopupPage.ShowInfo(this, "📋", L.PopupContractExpiredTitle, L.PopupContractExpiredMsg);
            return false;
        }

        private async Task<bool> TriggerLoanShark(Location contractLoc)
        {
            var L = LocalizationService.Instance;

            if (Player.HasLoan)
            {
                await TriggerGameOver();
                return false;
            }

            BigLoanSprite.Opacity = 1;
            await BigLoanSprite.TranslateToAsync(80, 0, 600, Easing.CubicOut);
            SoundService.PlayClick1();

            double repayAmount = contractLoc.WeeklyRent * 1.5;
            int repayDay = Player.Day + 7;

            bool accepted = await PopupPage.ShowConfirm(this, "🦈",
                L.LoanSharkTitle,
                L.FmtLoanSharkOffer(contractLoc.WeeklyRent, repayAmount, repayDay),
                L.BtnAcceptLoan, L.BtnRefuseLoan);

            if (accepted)
            {
                Player.ContractExpiryDay = Player.Day + 7;
                Player.HasLoan = true;
                Player.LoanAmount = repayAmount;
                Player.LoanRepayDay = repayDay;
                OnPropertyChanged(nameof(Player));
                SaveCurrentGame();
                UpdateLoanBanner();

                await BigLoanSprite.TranslateToAsync(600, 0, 600, Easing.CubicIn);
                BigLoanSprite.Opacity = 0;
                return true;
            }
            else
            {
                await TriggerGameOver();
                return false;
            }
        }

        private async Task<bool> CheckLoanRepayment()
        {
            if (!Player.HasLoan) return true;
            var L = LocalizationService.Instance;

            if (Player.Day >= Player.LoanRepayDay)
            {
                if (Player.Money >= Player.LoanAmount)
                {
                    double paid = Player.LoanAmount;
                    Player.Money -= paid;
                    Player.HasLoan = false;
                    Player.LoanAmount = 0;
                    Player.LoanRepayDay = 0;
                    OnPropertyChanged(nameof(Player));
                    SaveCurrentGame();
                    UpdateLoanBanner();
                    await PopupPage.ShowInfo(this, "💰", L.LoanRepaidTitle, L.FmtLoanRepaid(paid));
                    return true;
                }
                else
                {
                    await TriggerGameOver();
                    return false;
                }
            }

            return true;
        }

        private async Task TriggerGameOver()
        {
            BigLoanSprite.Opacity = 1;
            await BigLoanSprite.TranslateToAsync(80, 0, 600, Easing.CubicOut);
            SoundService.PlayClickF();

            // 2.4 Game Over cutscene
            var gameOverScene = AllStoryCutScenes.FirstOrDefault(s => s.Id == 15);
            if (gameOverScene != null)
            {
                var gameOverPage = new CutScenePage(new List<StoryCutScene> { gameOverScene });
                await Navigation.PushModalAsync(gameOverPage);
                await gameOverPage.WaitForDismissAsync();
            }

            SaveService.DeleteSave();
            WeakReferenceMessenger.Default.Send(new ResetGameMessage());
            BigLoanSprite.TranslationX = 600;
            BigLoanSprite.Opacity = 0;
        }

        #endregion

        #region --- Discover ---
        private async void OnDiscoverClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (Player.Money < 100)
                {
                    SoundService.PlayClickF();
                    var loc = LocalizationService.Instance;
                    await PopupPage.ShowInfo(this, "💸", loc.PopupDiscoverTitle, loc.PopupDiscoverMsg);
                    return;
                }
                Player.Money -= 100;
                OnPropertyChanged(nameof(Player));
                SaveCurrentGame();
                SoundService.PlayClick1();
                await Navigation.PushModalAsync(new DiscoverPage(Player, AllQuestions, AllIngredients));
            }
            finally { _isNavigating = false; }
        }

        private async Task LoadMasterData()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("MasterData.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var root = JsonSerializer.Deserialize<MasterDataRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (root != null)
            {
                this.AllIngredients = root.Ingredients;
                this.AllQuestions = root.Quizzes;
            }
        }
        #endregion

        #region --- Settings ---
        private async void OnSettingClicked(object sender, EventArgs e)
        {
            if (_isBusy || _isNavigating) { SoundService.PlayClickF(); return; }
            _isNavigating = true;
            try
            {
                SoundService.PlayClick1();
                await Navigation.PushModalAsync(new SettingPage());
            }
            finally { _isNavigating = false; }
        }
        #endregion
    }
}