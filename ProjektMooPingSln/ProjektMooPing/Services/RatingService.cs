namespace ProjektMooPing.Services
{
    public static class RatingService
    {
        public const int MaxTotalRating = 8000;
        public const int MaxDailyRating = 80;

        // --- Profit Score ---
        public static int CalcProfitScore(double profit) => profit switch
        {
            >= 500 => 30,
            >= 200 => 20,
            >= 50  => 10,
            >= 10  =>  5,
            _      =>  0
        };

        // --- Sales Score (Conversion Rate) ---
        public static int CalcSalesScore(int served, int total)
        {
            if (total == 0) return 0;
            double rate = (double)served / total * 100.0;
            return rate switch
            {
                >= 80 => 30,
                >= 50 => 15,
                >= 20 =>  5,
                _     =>  0
            };
        }

        // --- Quality Score ---
        public static int CalcQualityScore(float avgQuality) => avgQuality switch
        {
            >= 80 => 20,
            >= 60 => 10,
            >= 40 =>  5,
            _     =>  0
        };

        // --- Stockout Penalty ---
        public static int CalcStockoutPenalty(bool hadStockout) => hadStockout ? 15 : 0;

        // --- Daily Rating รวม ---
        public static int CalcDailyRating(double profit, int served, int total, float avgQuality, bool hadStockout)
        {
            int score = CalcProfitScore(profit)
                      + CalcSalesScore(served, total)
                      + CalcQualityScore(avgQuality)
                      - CalcStockoutPenalty(hadStockout);
            return score;
        }

        // --- แปลงคะแนนเป็นจำนวนดาว (0–5) ---
        public static int GetDailyStarCount(int dailyRating) => dailyRating switch
        {
            >= 64 => 5,
            >= 48 => 4,
            >= 32 => 3,
            >= 16 => 2,
            >  0  => 1,
            _     => 0
        };

        // --- string ดาว Daily เช่น "⭐⭐⭐☆☆" ---
        public static string GetDailyStarDisplay(int dailyRating)
        {
            int stars = GetDailyStarCount(dailyRating);
            return string.Concat(Enumerable.Repeat("⭐", stars))
                 + string.Concat(Enumerable.Repeat("☆", 5 - stars));
        }

        // --- string ดาว Total (5 ดาว, แต่ละดาว = 1600 pts) ---
        public static string GetTotalStarDisplay(int totalRating)
        {
            int stars = Math.Min(5, totalRating / 1600);
            return string.Concat(Enumerable.Repeat("⭐", stars))
                 + string.Concat(Enumerable.Repeat("☆", 5 - stars));
        }
    }
}
