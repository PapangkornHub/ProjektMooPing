using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjektMooPing.Services
{
    /// <summary>
    /// Singleton สำหรับจัดการ 2 ภาษา (ไทย / English)
    /// ใช้ใน XAML: {Binding SomeProp, Source={x:Static services:LocalizationService.Instance}}
    /// ใช้ใน C#:   LocalizationService.Instance.SomeProp
    /// </summary>
    public class LocalizationService : INotifyPropertyChanged
    {
        // ─── Singleton ──────────────────────────────────────────────────
        public static readonly LocalizationService Instance = new();

        private bool _isThai;

        public bool IsThai
        {
            get => _isThai;
            set
            {
                if (_isThai == value) return;
                _isThai = value;
                Preferences.Set("app_language", value ? "th" : "en");
                // แจ้ง MAUI binding engine ให้รีเฟรช property ทุกตัว
                OnPropertyChanged(string.Empty);
            }
        }

        private LocalizationService()
        {
            string lang = Preferences.Get("app_language", "th");
            _isThai = lang == "th";
        }

        // ─── Tab Labels ─────────────────────────────────────────────────
        public string TabMenu      => IsThai ? "เมนู"     : "Menu";
        public string TabEdit      => IsThai ? "สูตร"     : "Edit";
        public string TabInventory => IsThai ? "คลัง"     : "Inv";
        public string TabLocation  => IsThai ? "สถานที่"  : "Loc";

        // ─── MainGame Buttons ───────────────────────────────────────────
        public string BtnStartDay  => IsThai ? "เริ่มวัน"       : "Start Day";
        public string BtnCreate    => IsThai ? "สร้าง"          : "Create";
        public string BtnDiscover  => IsThai ? "ค้นพบ (100 ฿)" : "Discover (100 ฿)";
        public string BtnBuy       => IsThai ? "ซื้อ"           : "Buy";
        public string BtnRelocate  => IsThai ? "ย้ายมาที่นี่"  : "Relocate";
        public string BtnSignContract   => IsThai ? "เซนสัญญา / ย้ายมาที่นี่" : "Sign Contract";
        public string BtnContractActive => IsThai ? "✅ สัญญาใช้อยู่"          : "✅ Active Contract";
        public string BtnContractRenew  => IsThai ? "ต่อสัญญา"                 : "Renew Contract";
        public string LblCost      => IsThai ? "ต้นทุน"         : "Cost";

        // Speed button — ใส่ _timeScale แล้วเรียกใช้ใน code-behind
        public string FmtSpeed(int scale) => IsThai ? $"{scale}x ความเร็ว" : $"{scale}x Speed";

        // ─── DaySummary ─────────────────────────────────────────────────
        public string FmtSummaryTitle(int day)  => IsThai ? $"สรุปวันที่ {day}" : $"Day {day} Summary";
        public string LblTotalCost   => IsThai ? "ต้นทุนรวม:"    : "Total Cost:";
        public string LblTotalIncome => IsThai ? "รายได้รวม:"    : "Total Income:";
        public string LblTotalProfit => IsThai ? "กำไรสุทธิ:"    : "Total Profit:";
        public string LblTodayRating => IsThai ? "Rating วันนี้" : "Today's Rating";
        public string FmtDailyPts(int pts)  => pts >= 0
            ? (IsThai ? $"+{pts} คะแนน วันนี้" : $"+{pts} pts today")
            : (IsThai ? $"{pts} คะแนน วันนี้"  : $"{pts} pts today");
        public string FmtTotalRating(int n) => IsThai ? $"รวม {n}/8000"       : $"Total {n}/8000";
        public string LblProfit      => IsThai ? "กำไร"            : "Profit";
        public string LblSatisfy     => IsThai ? "ลูกค้าพอใจ"     : "Satisfaction";
        public string LblQuality     => IsThai ? "คุณภาพสูตร"     : "Recipe Quality";
        public string LblStockout    => IsThai ? "ของหมดก่อนเวลา" : "Stockout";

        // ─── Discover / Quiz ────────────────────────────────────────────
        public string LblNextQuestion => IsThai ? "คำถามถัดไป ➔"  : "Next Question ➔";
        public string BtnExit         => IsThai ? "ออก"            : "Exit";
        public string FmtGoal(string reward) =>
            IsThai ? $"เป้าหมาย: 3/5 เพื่อปลดล็อก [{reward}]!"
                   : $"Goal: Score 3/5 to unlock [{reward}]!";
        public string LblGoalComplete =>
            IsThai ? "เป้าหมาย: 3/5 เพื่อชนะ!" : "Goal: Score 3/5 to win!";
        public string FmtQuestionCount(int cur, int total) =>
            IsThai ? $"คำถามที่ {cur}/{total}" : $"Question {cur}/{total}";
        public string FmtScore(int s) => IsThai ? $"คะแนน: {s}" : $"Score: {s}";

        // ─── MenuDetail ─────────────────────────────────────────────────
        public string LblPopularity => IsThai ? "ความนิยม:"  : "Popularity:";
        public string LblReady      => IsThai ? "พร้อมขาย:"  : "Ready:";
        public string BtnClose      => IsThai ? "ปิด"         : "Close";
        public string BtnAccept     => IsThai ? "ยืนยัน"     : "Accept";
        public string BtnNext       => IsThai ? "ถัดไป ▶"     : "Next ▶";
        public string BtnCookNow    => IsThai ? "ปิ้งเลย! 🔥" : "COOK NOW! 🔥";
        public string BtnCookAll    => IsThai ? "ปิ้งทั้งหมด! 🔥🔥" : "COOK ALL! 🔥🔥";

        // ─── EditDetail ─────────────────────────────────────────────────
        public string LblCostAnalysis         => IsThai ? "ต้นทุน"          : "Cost";
        public string LblSynergyAnalysis      => IsThai ? "ซินเนอร์จี้"     : "Synergy";
        public string LblIngredientsSelection => IsThai ? "เลือกวัตถุดิบ"   : "Ingredients Selection";
        public string AllowEnabled            => IsThai ? "เปิดขาย ✅"      : "Selling: ON ✅";
        public string AllowDisabled           => IsThai ? "ปิดขาย ❌"       : "Selling: OFF ❌";
        public string LblNewMenu              => IsThai ? "เมนูใหม่"         : "New Menu";
        public string EditNameTitle           => IsThai ? "แก้ชื่อสูตร"     : "Edit Name";
        public string EditNameMsg             => IsThai ? "ใส่ชื่อสูตรอาหาร" : "Enter the recipe name";

        // ─── Settings ───────────────────────────────────────────────────
        public string PageSettings    => IsThai ? "ตั้งค่า"       : "Settings";
        public string LblGameVersion  => IsThai ? "เวอร์ชัน:"     : "Game Version:";
        public string LblDangerZone   => IsThai ? "โซนอันตราย"    : "DANGER ZONE";
        public string LblResetWarning => IsThai ? "รีเซตข้อมูล?"  : "Wiping data?";
        public string LblResetDesc    =>
            IsThai ? "ข้อมูลทั้งหมดจะถูกลบและไม่สามารถกู้คืนได้"
                   : "All progress will be deleted. This cannot be reverted.";
        public string BtnResetData    => IsThai ? "รีเซตข้อมูล"   : "RESET DATA";
        public string LblLanguage     => IsThai ? "ภาษา / Language" : "Language / ภาษา";

        // ─── Common ─────────────────────────────────────────────────────
        public string BtnOK     => IsThai ? "ตกลง"   : "OK";
        public string BtnCancel => IsThai ? "ยกเลิก" : "Cancel";

        // ─── Popup: MainGame ────────────────────────────────────────────
        public string PopupOutOfStockTitle => IsThai ? "หมดสต็อก!"         : "Out of Stock!";
        public string PopupOutOfStockMsg   => IsThai ? "สินค้าหมดแล้ว... ปิดร้านก่อนเวลา"
                                                     : "Out of stock! Closing shop early.";
        public string PopupErrorTitle      => IsThai ? "เกิดข้อผิดพลาด"    : "Error";
        public string PopupLocError        => IsThai ? "ไม่สามารถเปลี่ยนสถานที่ได้"
                                                     : "Cannot change location.";
        public string PopupRentTitle       => IsThai ? "เงินไม่พอ!"         : "Not Enough Cash!";
        public string FmtRentMsg(int rent, string loc) =>
            IsThai ? $"ต้องมีเงินอย่างน้อย {rent}฿\nเพื่อจ่ายค่าเช่าที่ {loc}"
                   : $"Need at least {rent}฿\nto pay rent at {loc}";
        public string PopupContractExpiredTitle => IsThai ? "สัญญาหมดอายุ!"      : "Contract Expired!";
        public string PopupContractExpiredMsg   =>
            IsThai ? "กรุณาเซนสัญญาใหม่ที่แท็บ 'สถานที่' ก่อนเริ่มวัน"
                   : "Please sign a new contract in the 'Loc' tab before starting the day.";
        public string PopupContractRatingTitle  => IsThai ? "Rating ไม่พอ!"       : "Rating Too Low!";
        public string FmtContractRatingMsg(int required, int current) =>
            IsThai ? $"ต้องการ Rating {required} คะแนน\nปัจจุบันมี {current} คะแนน"
                   : $"Requires {required} Rating\nYou have {current}";
        public string PopupContractMoneyMsg(int cost) =>
            IsThai ? $"ต้องการเงิน {cost}฿ เพื่อเซนสัญญา" : $"Need {cost}฿ to sign contract.";
        public string FmtContractSigned(string locName, int expiryDay) =>
            IsThai ? $"เซนสัญญาที่ {locName} สำเร็จ!\nสัญญาใช้ได้ถึงวันที่ {expiryDay}"
                   : $"Contract signed at {locName}!\nValid until Day {expiryDay}";
        public string FmtContractExpiry(int day) =>
            IsThai ? $"สัญญาถึง: วันที่ {day}" : $"Contract until: Day {day}";
        public string LblContractExpired  => IsThai ? "⚠️ สัญญาหมดอายุ!" : "⚠️ Contract Expired!";
        public string LblLockedRating     => IsThai ? "🔒 Rating ไม่เพียงพอ" : "🔒 Rating Required";
        public string LblRequiredRating   => IsThai ? "Rating ที่ต้องการ:"   : "Required Rating:";
        public string LblTraffic          => IsThai ? "จำนวนลูกค้า:"        : "Customers:";
        public string LblRequirements     => IsThai ? "🎯 เงื่อนไขการเซนสัญญา" : "🎯 Requirements";
        public string LblWeeklyRent       => IsThai ? "ค่าเช่า/สัปดาห์:"    : "Weekly Rent:";
        public string LblSignCost         => IsThai ? "ค่าเซนสัญญา:"        : "Sign Cost:";
        public string LblFree             => IsThai ? "ฟรี"                  : "Free";

        // ─── Loan Shark ─────────────────────────────────────────────────
        public string LoanSharkTitle => IsThai ? "เฮียบิ๊กเงินกู้!" : "Big Loan Shark!";
        public string FmtLoanSharkOffer(int rentCost, double repayAmount, int repayDay) =>
            IsThai ? $"เฮียจ่ายค่าเช่า {rentCost}฿ให้ก่อน\nแต่ต้องคืน {repayAmount:N0}฿ ภายในวันที่ {repayDay}\nรับไหม?"
                   : $"I'll cover {rentCost}฿ rent for you.\nRepay {repayAmount:N0}฿ by Day {repayDay}.\nAccept?";
        public string BtnAcceptLoan    => IsThai ? "ตกลงเฮีย 🙏" : "Deal! 🙏";
        public string BtnRefuseLoan    => IsThai ? "ไม่เอา"        : "Refuse";
        public string LoanRepaidTitle  => IsThai ? "ใช้หนี้สำเร็จ!" : "Loan Repaid!";
        public string FmtLoanRepaid(double amount) =>
            IsThai ? $"หักเงิน {amount:N0}฿ คืนเฮียบิ๊กแล้ว"
                   : $"Repaid {amount:N0}฿ to the loan shark.";
        public string GameOverTitle    => IsThai ? "เกมโอเวอร์!" : "Game Over!";
        public string GameOverLoanMsg  =>
            IsThai ? "เงินไม่พอคืนเฮียบิ๊ก...\nร้านมูปิ้งถูกยึดไปแล้ว!"
                   : "Can't repay the loan shark...\nYour stall has been seized!";
        public string LoanReminderTitle => IsThai ? "⚠️ เตือนหนี้" : "⚠️ Debt Reminder";
        public string FmtLoanReminder(double amount, int repayDay) =>
            IsThai ? $"ยังค้างหนี้เฮียบิ๊ก {amount:N0}฿\nต้องคืนภายในวันที่ {repayDay}"
                   : $"You owe the shark {amount:N0}฿\nDue by Day {repayDay}";
        public string LblCannotDowngrade  =>
            IsThai ? "⛔ ย้ายถอยหลังไม่ได้" : "⛔ Cannot go back";
        public string LblSequentialLocked =>
            IsThai ? "🔒 ต้องปลดล็อคที่ก่อนหน้าก่อน" : "🔒 Unlock previous locations first";
        public string LblAutoRenewTitle   => IsThai ? "ต่อสัญญาอัตโนมัติ!"  : "Auto-Renewed!";
        public string FmtAutoRenewMsg(string locName, int expiryDay) =>
            IsThai ? $"หักค่าเช่าอัตโนมัติที่ {locName}\nสัญญาใช้ได้ถึงวันที่ {expiryDay}"
                   : $"Rent auto-deducted at {locName}\nValid until Day {expiryDay}";
        public string PopupDiscoverTitle   => IsThai ? "เงินไม่พอ!"         : "Not Enough Money!";
        public string PopupDiscoverMsg     =>
            IsThai ? "ต้องการ 100฿ เพื่อเริ่ม Discover วัตถุดิบใหม่"
                   : "Need 100฿ to start discovering ingredients.";
        public string FmtLoadError(string msg) =>
            IsThai ? $"โหลดข้อมูลไม่สำเร็จ: {msg}" : $"Cannot load data: {msg}";

        // ─── Popup: Quiz ────────────────────────────────────────────────
        public string QuizPassTitle => IsThai ? "ผ่านแล้ว!" : "Passed!";
        public string QuizFailTitle => IsThai ? "ไม่ผ่าน"   : "Failed";
        public string FmtQuizScore(int s) => IsThai ? $"คะแนน {s}/5" : $"Score: {s}/5";
        public string FmtQuizPass(string reward) =>
            IsThai ? $"ปลดล็อกวัตถุดิบใหม่: {reward}" : $"New ingredient unlocked: {reward}";
        public string QuizAllUnlocked => IsThai ? "ปลดล็อกวัตถุดิบครบหมดแล้ว!" : "All ingredients unlocked!";
        public string QuizFailMsg     => IsThai ? "ลองใหม่อีกครั้งนะ!"         : "Try again next time!";
        public string QuizQuitTitle   => IsThai ? "ออกจากเกม?"  : "Quit?";
        public string QuizQuitMsg     =>
            IsThai ? "ต้องการออกหรือไม่? (ไม่คืนเงิน 200฿)"
                   : "Do you wish to quit? (No REFUND)";
        public string BtnQuit => IsThai ? "ออก"    : "Quit";
        public string BtnStay => IsThai ? "อยู่ต่อ" : "Stay";

        // ─── Popup: Edit ────────────────────────────────────────────────
        public string EditWarnIngTitle    => IsThai ? "คำเตือน"      : "Warning";
        public string EditWarnIngMsg      =>
            IsThai ? "ต้องเลือกวัตถุดิบอย่างน้อย 1 ชนิด!"
                   : "A recipe must have at least one ingredient!";
        public string EditWarnMeatTitle   => IsThai ? "ต้องมีเนื้อ!" : "Missing Meat!";
        public string EditWarnMeatMsg     =>
            IsThai ? "สูตรต้องมีวัตถุดิบประเภท Meat อย่างน้อย 1 ชนิด"
                   : "A recipe must have at least one meat ingredient.";
        public string EditCannotEditTitle =>
            IsThai ? "แก้ไขไม่ได้!" : "Cannot Edit!";
        public string EditCannotEditMsg   =>
            IsThai ? "ยังมีสินค้าของสูตรนี้อยู่ในคลัง\nกรุณาขายให้หมดก่อน"
                   : "There are still products in stock.\nSell them first.";
        public string EditCannotDelTitle  =>
            IsThai ? "ลบไม่ได้!" : "Cannot Delete!";
        public string EditCannotDelMsg    =>
            IsThai ? "ยังมีสินค้าของสูตรนี้อยู่ในคลัง\nกรุณาขายให้หมดก่อน"
                   : "There are still products in stock.\nSell them first.";

        // ─── Popup: Settings ────────────────────────────────────────────
        public string SettingResetTitle =>
            IsThai ? "ยืนยันการรีเซต?" : "Confirm Reset?";
        public string SettingResetMsg   =>
            IsThai ? "ข้อมูลทั้งหมดจะถูกลบ\nและไม่สามารถกู้คืนได้!"
                   : "All data will be deleted\nand cannot be recovered!";
        public string BtnDeleteConfirm  => IsThai ? "ลบเลย"   : "Delete";

        // ─── INotifyPropertyChanged ─────────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
