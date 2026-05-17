# ProjektMooPing — System Report

> เอกสารสรุประบบเกม สำหรับ Developer ที่เพิ่งเข้ามาทำความเข้าใจ Codebase
> Tech Stack: .NET MAUI + CommunityToolkit.Mvvm + Plugin.Maui.Audio

---

## 1. Architecture & Project Structure

### โครงสร้างโฟลเดอร์

```
ProjektMooPing/
├── App.xaml(.cs)              ─ Entry point, ตั้งค่า Window
├── AppShell.xaml(.cs)         ─ Shell wrapper (ว่าง — ไม่ได้ใช้ navigation tabs)
├── MauiProgram.cs             ─ Bootstrap, ลงทะเบียน font + SkiaSharp
│
├── Models/                    ─ Data structures (POCO classes)
│   ├── PlayerProfile.cs       ─ State ทั้งหมดของผู้เล่น (Money, Day, Inventory, Rating, Loan)
│   ├── Recipe.cs              ─ สูตรอาหาร (Ingredients, SellingPrice, IsAllowed)
│   ├── Location.cs            ─ สถานที่ขาย (Rent, Traffic, DogLucky, Story)
│   ├── Ingredient.cs          ─ วัตถุดิบ (Cost, BasePopularity, Category)
│   ├── StoryCutScene.cs       ─ ข้อมูล CutScene พร้อม Localization (Th/En)
│   ├── DaySummary.cs          ─ สรุปรายวัน (Profit, Rating breakdown)
│   ├── Messages.cs            ─ Message types สำหรับ WeakReferenceMessenger
│   └── ...
│
├── Services/                  ─ Logic แบบ static (ไม่มี DI)
│   ├── SaveService.cs         ─ Save/Load → mooping_save.json
│   ├── SoundService.cs        ─ จัดการ Audio (SFX, BGM, Typewriter loop)
│   ├── RecipeService.cs       ─ คำนวณ Popularity, Quality Score, Synergy
│   ├── RatingService.cs       ─ คะแนน Rating (Profit/Sales/Quality/Stockout)
│   ├── BuffService.cs         ─ ระบบ Buff จาก Lucky (9 ตัว ผูกกับ Location)
│   └── LocalizationService.cs ─ Singleton สลับภาษา Th/En
│
├── View/                      ─ UI Pages (XAML + code-behind)
│   ├── MainGamePage           ─ หน้าหลัก ★ ขนาดใหญ่มาก ~1,200 บรรทัด (ดู §2)
│   ├── CutScenePage           ─ แสดง Story / Buff พร้อม Typewriter + เสียง
│   ├── DaySummaryPage         ─ สรุป Rating หลังจบวัน
│   ├── SettingPage            ─ ตั้งค่าภาษา + Reset
│   ├── PopupPage              ─ Generic popup (ShowInfo/ShowConfirm)
│   ├── EditDetailPage         ─ แก้ไขสูตร
│   ├── MenuDetailPage         ─ ดูเมนู
│   └── DiscoverPage           ─ ปลดล็อควัตถุดิบใหม่
│
├── ViewModel/                 ─ View Adapters
│   ├── RecipeViewModel        ─ implements INotifyPropertyChanged
│   └── IngredientViewModel    ─ inventory display
│
└── Resources/
    ├── Raw/                   ─ MasterData.json, .mp3 files
    ├── Images/                ─ Location/Story sprites
    └── Styles/Colors.xaml     ─ Theme (MooPingDarkBrown, Cream, ...)
```

### การแบ่งหน้าที่

| ระบบเกม | ที่อยู่หลัก |
|---|---|
| **หน้าร้าน / Customer simulation** | `MainGamePage.StartCustomerAnimation()` + `CheckSaleLogic()` |
| **จัดการเมนู (สูตร)** | `MainGamePage.RefreshMenuUI()` + `EditDetailPage` + `MenuDetailPage` |
| **คลังวัตถุดิบ** | `Player.IngredientInventory` (Dictionary) + `RefreshInventoryUI()` |
| **ระบบสถานที่ (เช่า/ปลดล็อค)** | `MainGamePage.UpdateLocTabUI()` + `OnSignContractClicked()` |
| **ระบบ Rating & Buff** | `RatingService` + `BuffService` (ดู §3) |
| **ระบบบันทึก** | `SaveService` (JSON ที่ `FileSystem.AppDataDirectory`) |

### ข้อสังเกตเชิงสถาปัตยกรรม
- **ไม่มีระบบ "พนักงาน" (Employee)** — ผู้เล่นทำงานคนเดียวทั้งหมด
- ไม่ใช้ DI Container — ทุก Service เป็น `static class`
- ไม่ใช้ Shell navigation — ใช้ `Navigation.PushModalAsync` ตรงๆ
- `MainGamePage` ทำหน้าที่เป็น "God Object" — เก็บ logic ส่วนใหญ่ของเกม (ดู §4)

---

## 2. Core Game Logic & Data Flow

### Data Loading Pipeline (เปิดเกม)

```
App.OnStart()
  └→ Navigation → MainGamePage
       └→ OnAppearing() (รันครั้งเดียวด้วย _initialized flag)
            ├→ await LoadMasterData()
            │    └→ อ่าน Resources/Raw/MasterData.json
            │       → populate AllLocations, AllIngredients, AllStoryCutScenes
            │
            └→ LoadGameData()
                 ├→ SaveService.LoadGame() → mooping_save.json
                 │   (ถ้าไม่มี → สร้าง PlayerProfile ใหม่ที่ Day 1, Money 100)
                 ├→ Backward-compat fixes (null inventory, default contract)
                 ├→ Refresh UI ทั้งหมด
                 └→ ถ้า AlreadyWatchIntro == false → push CutScenePage (Intro)
```

### Message Bus (WeakReferenceMessenger)

ทุกหน้าจอแยกกันสื่อสารกันผ่าน Message — ไม่มีหน้าไหนถือ reference ของอีกหน้า

| Message | Sender | Receiver (`MainGamePage`) |
|---|---|---|
| `NewRecipeMessage` | EditDetailPage (สร้างสูตรใหม่) | Add → Refresh → Save |
| `RecipeUpdatedMessage` | EditDetailPage (แก้สูตร) | Refresh → Save |
| `RecipeDeletedMessage` | MenuDetailPage (ลบสูตร) | Remove → Refresh → Save |
| `ResetGameMessage` | SettingPage (Reset), TriggerGameOver | LoadGameData ใหม่ |
| `IngredientUnlockedMessage` | DiscoverPage | Refresh Inventory |
| `AddRatingMessage` | (เคยมีใน Dev Tool, ปัจจุบันยังเหลือ handler) | Add Rating + Save |

### Data Binding (XAML ↔ Code)

- `BindingContext = this` ที่ root → bind ตรงไปยัง property ของ Page
- `Player.Money`, `Player.Day` ฯลฯ bind ผ่าน `{Binding Player.Xxx}`
- Localization ใช้ `{Binding LblXxx, Source={x:Static services:LocalizationService.Instance}}`
- Refresh UI หลายส่วนใช้ `OnPropertyChanged(nameof(Player))` กระตุ้น re-bind ทั้งก้อน

### Customer Sale Flow (หัวใจของเกม)

```
กด "เปิดร้าน" → StartCustomerAnimation()
  ├─ CheckLoanRepayment()      (ถ้าหนี้ครบ → จ่าย หรือ TriggerGameOver)
  ├─ CheckContractValid()      (ถ้าสัญญาหมด → ต่อสัญญา)
  ├─ Player.Day++
  ├─ EnduranceBuff: stock +3 ต่อสูตรที่มีอยู่
  ├─ totalCustomers = Random(MinTraffic, MaxTraffic)
  ├─ LongingBuff: totalCustomers += 3
  │
  └─ For each customer (1..totalCustomers):
       ├─ Animate sprite เดินเข้า
       ├─ CheckSaleLogic() ← คำนวณการซื้อ
       │    ├─ Random 1-3 เมนูที่จะสนใจ
       │    ├─ For each เมนู:
       │    │    ├─ basePop = CalculatePopularity(recipe, price, ingredients,
       │    │    │             ignoreNegSynergy=LettingGoBuff,
       │    │    │             costMult = CivilizationBuff ? 0.9 : 1.0)
       │    │    ├─ Loop q จาก maxWant ลง: ถ้า rnd ≤ basePop × diffMult → ขายได้
       │    │    ├─ MischiefBuff: ถ้าไม่ผ่านเลย แต่ basePop > 0 → ขายขั้นต่ำ 1
       │    │    ├─ หักสต็อก → NegotiationBuff: 25% บวก +1 ไม้ฟรี (ถ้าสต็อกพอ)
       │    │    └─ บันทึก Revenue, UnitCost
       │    └─ Player.Money += earnings
       ├─ Animate sprite เดินออก
       └─ ถ้าสต็อกหมดและยังมีลูกค้า → hadStockout = true, BREAK

  └─ (finally) คำนวณ DailyRating:
       ProfitScore  = CalcProfitScore(profit)  × (SuccessBuff ? 2 : 1)
       SalesScore   = CalcSalesScore(served, total)
       QualityScore = CalcQualityScore(avgQuality)
       Penalty      = ResilienceBuff ? 0 : CalcStockoutPenalty(hadStockout)
       DailyRating  = (Profit + Sales + Quality − Penalty) × (EuphoriaBuff ? 1.2 : 1.0)
       Player.TotalRating += DailyRating (clamp 0..8000)
       └→ Push DaySummaryPage
       └→ ถ้า Location 10 + Rating 8000 → True Ending CutScene
```

### Save Strategy

- `SaveCurrentGame()` ถูกเรียก**บ่อยมาก** — หลังจากทุก action: ซื้อของ, แก้สูตร, จบวัน, เซ็นสัญญา
- บันทึกแบบ overwrite ทั้งไฟล์ JSON ทุกครั้ง

---

## 3. Deep Dive Insights — เจาะลึกการตัดสินใจเชิงระบบ

### A. ระบบ Buff ผูกกับ Location Unlock

**โค้ด ([`BuffService.cs`](ProjektMooPing/Services/BuffService.cs)):**
```csharp
public static bool HasNegotiationBuff(PlayerProfile p) => p.UnlockedLocationIds.Contains(2);
public static bool HasResilienceBuff(PlayerProfile p)  => p.UnlockedLocationIds.Contains(3);
// ...
```

**ทำไมใช้เทคนิคนี้:**
- ไม่ต้องสร้าง entity `Lucky` แยก ไม่ต้อง track "ผู้เล่นได้ buff อะไรบ้าง"
- Truth ของ buff = state เดียว (Unlocked location) → state ใน save file ตัวเดียว
- เพิ่ม buff ใหม่ = แก้ method เดียว ไม่ต้อง migrate save

**ถ้าไม่ใช้:**
- ต้องมี `List<int> UnlockedBuffIds` ใน `PlayerProfile` → ต้อง sync กับ location → bug ได้ง่าย (เช่นปลดล็อคสถานที่แต่ลืม add buff)
- Save ที่มีอยู่จะต้อง migrate ใหม่หมด

**ข้อเสีย:** ผูกแน่นมาก — ถ้าต้องการ buff ที่ไม่ได้มาจาก location (เช่นจาก achievement) จะต้อง refactor

---

### B. `TaskCompletionSource` ใน CutScenePage

**โค้ด ([`CutScenePage.xaml.cs`](ProjektMooPing/View/CutScenePage.xaml.cs)):**
```csharp
private readonly TaskCompletionSource _dismissed = new();
public Task WaitForDismissAsync() => _dismissed.Task;
protected override void OnDisappearing()
{
    base.OnDisappearing();
    _dismissed.TrySetResult();
}
```

**ทำไมใช้เทคนิคนี้:**
- `Navigation.PushModalAsync()` ของ MAUI return เมื่อ push เสร็จ ไม่รอ pop
- ใช้ใน Game Over flow ที่ต้องรอผู้เล่นปิด cutscene ก่อนจะลบ save

**ถ้าไม่ใช้:**
- จะลบ save ทันทีหลัง push → cutscene เปิดขึ้นมา → ทันทีนั้น `ResetGameMessage` ก็ trigger `LoadGameData()` → สร้าง PlayerProfile ใหม่ที่ `AlreadyWatchIntro = false` → Intro CutScene เปิดซ้อน Game Over CutScene
- (นี่คือ bug ที่เพิ่งแก้ไป)

`TrySetResult` (ไม่ใช่ `SetResult`) ป้องกัน exception กรณี `OnDisappearing` fire ซ้ำ

---

### C. `CancellationTokenSource` สำหรับ Typewriter Animation

**โค้ด:**
```csharp
private CancellationTokenSource? _typewriterCts;
private async Task RunTypewriterAsync(string text, CancellationToken ct)
{
    for (int i = 1; i <= text.Length; i++) {
        ct.ThrowIfCancellationRequested();
        TextLabel.Text = text[..i];
        await Task.Delay(TypewriterDelayMs, ct);
    }
}
```

**ทำไม:** ผู้เล่นต้อง skip animation ได้ (กด Next ระหว่างพิมพ์ → โชว์ text เต็ม)

**ถ้าไม่ใช้:**
- ใช้ `bool _skip = true` แล้ว loop เช็ค flag — ยัง stuck ที่ `Task.Delay` 35ms อยู่
- ถ้าใช้ `Thread.Sleep` แทน → block UI thread

CTS เป็นทางเดียวที่จะ "ตัด" `Task.Delay` ได้ทันที

---

### D. `LocalizationService` เป็น Singleton + INotifyPropertyChanged

**โค้ด:**
```csharp
public static LocalizationService Instance { get; } = new();
public bool IsThai { get => _isThai; set { _isThai = value; OnPropertyChanged(nameof(IsThai)); /* trigger ALL */ } }
public string LblMoney => IsThai ? "เงิน" : "Money";
// ... อีก ~100 properties
```

**ทำไม:**
- XAML bind `{Binding LblMoney, Source={x:Static services:LocalizationService.Instance}}`
- กดสลับภาษา → `IsThai = !IsThai` → `OnPropertyChanged` ของทุก property → UI refresh ทั้งหมดทันที

**ถ้าไม่ใช้:**
- ต้อง refresh ทุก label ด้วยมือ หรือ rebuild ทุกหน้า
- ROI: เขียน boilerplate เยอะ แต่ scale ได้ดีเมื่อหน้าจอเยอะ

---

### E. `_initialized` Flag ใน OnAppearing

**โค้ด:**
```csharp
private bool _initialized = false;
protected override async void OnAppearing()
{
    base.OnAppearing();
    if (_initialized) return;
    _initialized = true;
    await LoadMasterData();
    LoadGameData();
}
```

**ทำไม:** `OnAppearing` ถูกเรียกทุกครั้งที่กลับมาหน้านี้ (เช่นปิด modal) แต่ initialization ควรทำครั้งเดียว

**ถ้าไม่ใช้:**
- Reset PlayerProfile ทุกครั้งที่ปิด DaySummaryPage → ผู้เล่นเสีย progress

**ข้อเสีย:** ถ้าต้องการ reload ข้อมูล (เช่นภาษาเปลี่ยน) ต้องใช้ Message bus แทน → ซึ่งโปรเจกต์นี้ทำอยู่แล้ว

---

### F. การ Resolve Customer Purchase ด้วย Loop จาก High→Low

**โค้ด:**
```csharp
for (int q = maxWant; q >= 1; q--) {
    float difficultyMult = 1.1f - (q * 0.1f);   // q=5 → 0.6, q=1 → 1.0
    float finalChance = basePop * difficultyMult;
    if (rnd.Next(1, 101) <= finalChance) { actualSoldInThisMenu = q; break; }
}
```

**ทำไม:** ลูกค้า "อยาก" maxWant แต่ "ตัดสินใจ" ตาม pop — ลองเสนอเยอะก่อน ถ้าไม่ผ่านลดลง

**ถ้าใช้ low→high แทน:**
- ผู้เล่นจะขายได้น้อย เพราะลูกค้ายอมซื้อ 1 ไม้ตั้งแต่ q=1 แล้วหยุด
- ปัจจุบัน: ถ้า pop สูง ลูกค้าซื้อเยอะ → reward ฝีมือผู้เล่น

---

## 4. Optimization & Vulnerabilities

### 🔴 ปัญหาเร่งด่วน (ควรแก้)

#### 1. `new Random()` ในลูป (Performance + Randomness Bug)

| ไฟล์ | ปัญหา |
|---|---|
| `MainGamePage.StartCustomerAnimation` | `new Random()` ต่อการเริ่มวัน |
| `MainGamePage.CheckSaleLogic` | `new Random()` ต่อ**ลูกค้าทุกคน** |
| `Location.GetDailyTraffic` | `new Random()` ทุกครั้งที่เรียก |

**ผลกระทบ:** `new Random()` ใช้ system clock seed — สร้างติดกันเร็วๆ จะได้ seed ใกล้กัน → distribution แย่ + เปลือง CPU

**แก้:**
```csharp
private static readonly Random _rng = new();
```

---

#### 2. Synergy Bonus ซ้ำใน `RecipeService`

**โค้ด ([`RecipeService.cs`](ProjektMooPing/Services/RecipeService.cs:36) และ บรรทัด 52):**
```csharp
// บรรทัด 36
if (ids.Contains(17) && ids.Contains(19)) synergyBonus += 15;  // น้ำตาลปี๊บ + กะทิ
// ...
// บรรทัด 52 — เงื่อนไขเดิมเป๊ะ
if (ids.Contains(17) && ids.Contains(19)) synergyBonus += 15;  // น้ำตาลปี๊บ + กะทิ
```

**ผลกระทบ:** ใครใช้คู่นี้จะได้ +30 แทน +15 — โจมตี balance

**แก้:** ลบบรรทัดที่ซ้ำออก

---

#### 3. `SaveCurrentGame()` เรียกบ่อยเกินไป

ทุก action เขียน JSON ใหม่ทั้งไฟล์ — ซื้อของ 10 ครั้ง = เขียนไฟล์ 10 ครั้ง

**แก้:**
- ใช้ debounce: เรียก save แต่จริงๆ flush หลัง idle 500ms
- หรือ save แค่จุดสำคัญ (จบวัน, ปลดล็อค, ออกเกม)

---

#### 4. Race Condition: `LoadGameData()` เรียกจาก `ResetGameMessage`

```csharp
WeakReferenceMessenger.Default.Register<ResetGameMessage>(this, (r, m) => {
    LoadGameData();   // ← เป็น async void, fire-and-forget
    RefreshMenuUI();  // ← รันทันทีก่อน LoadGameData เสร็จ!
    ...
});
```

**ผลกระทบ:** Refresh UI ก่อน Player ถูกโหลด → bind ตัวเก่าหรือ null

**แก้:** เปลี่ยน lambda เป็น `async` แล้ว `await LoadGameData()` (ต้องเปลี่ยน return type เป็น Task)

---

### 🟡 ปัญหาระดับกลาง

#### 5. `LoadGameData()` เป็น `async void`

Exception ใน async void จะ crash app (ไม่ผ่าน Task ปกติ)

**ปัจจุบัน:** มี try/catch ครอบใน method → ปลอดภัยแค่ในกรณีที่ exception เกิดใน try block

**แก้:** เปลี่ยนเป็น `async Task` แล้ว caller await

---

#### 6. `MainGamePage` เป็น God Object (~1,200 บรรทัด)

ถือ logic ทั้งหมด — customer sim, save, location mgmt, recipe filter, loan, UI refresh, message handlers

**แก้ (refactor):**
- แยก `GameLoopController` (customer sim + rating)
- แยก `LocationController` (สัญญา + ปลดล็อค)
- แยก `LoanController` (กู้/จ่าย/Game Over)

---

#### 7. ไม่มี `Dispose` ของ `WeakReferenceMessenger.Register`

ลงทะเบียน 6 messages แต่ไม่ unregister — `WeakReference` ช่วยกัน leak ส่วนใหญ่ได้ แต่ปฏิบัติที่ดีคือ unregister ใน `OnDisappearing` หรือ destructor

---

#### 8. List ขยายไม่จำกัด

- `Player.CreatedRecipes` — เก็บทุกสูตรที่เคยสร้าง
- `Player.RecipeInventory` — entry ไม่เคยลบ

**ผลกระทบ:** เล่น 100+ วัน save file โตขึ้นเรื่อยๆ → load ช้า

**แก้:** เพิ่ม UI ให้ผู้เล่นลบสูตรเก่า หรือ purge entries ที่ stock = 0 มานานๆ

---

### 🟢 จุดที่ทำได้ดี

- **Buff system ออกแบบ extensible** (ดู §3.A)
- **Localization architecture** sustainable
- **TaskCompletionSource + CancellationToken** ใช้ถูกตามจุดประสงค์
- **CutScenePage รองรับ List<Scene>** → reuse ได้กับ intro, location story, buff, ending ทั้งหมด

---

## ภาคผนวก: Quick Reference

### Constants สำคัญ
```csharp
RatingService.MaxTotalRating = 8000     // 5 ดาว
RatingService.MaxDailyRating = 80       // เพดานก่อน buff
StartingMoney = 100฿                    // PlayerProfile constructor
ContractDuration = 7 days
TypewriterDelayMs = 35
TypewriterVolume = 0.6
```

### Save Path
```
{FileSystem.AppDataDirectory}/mooping_save.json
Android: /data/data/<package>/files/mooping_save.json
```

### CutScene IDs (ใน MasterData.json)
```
11, 12, 13  → Intro (3 scenes)
14          → True Ending (Location 10 + Rating 8000)
15          → Game Over (หนี้จ่ายไม่ได้)
```

### Lucky → Location Map
```
Loc 2  Negotiation    25% +1 free skewer
Loc 3  Resilience     ยกเว้น stockout penalty
Loc 4  Mischief       การันตี min 1 (pop > 0)
Loc 5  Civilization   ต้นทุน ×0.9 ใน pop calc
Loc 6  Endurance      stock +3 ต่อวัน
Loc 7  Longing        traffic +3
Loc 8  Euphoria       daily rating ×1.2
Loc 9  Letting Go     ปิด negative synergy
Loc 10 Success        profit score ×2
```
