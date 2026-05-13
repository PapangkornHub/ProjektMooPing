using System;
using System.Collections.Generic;
using System.Text;

namespace ProjektMooPing.Models
{
    public class DaySummary
    {
        public int DayNumber { get; set; }
        public List<MenuSalesStats> SalesDetails { get; set; } = new();
        public double TotalCost { get; set; }
        public double TotalRevenue { get; set; }
        public double TotalProfit => TotalRevenue - TotalCost;

        // --- Rating ของวันนี้ ---
        public int CustomersServed { get; set; }   // ลูกค้าที่ซื้อ
        public int TotalCustomers { get; set; }    // ลูกค้าทั้งหมด
        public bool HadStockout { get; set; }      // ของหมดก่อนลูกค้าหมด
        public float AvgQualityScore { get; set; } // คะแนนคุณภาพเฉลี่ย

        // Score แยก Factor (set โดย MainGamePage หลังจบวัน)
        public int ProfitScore { get; set; }
        public int SalesScore { get; set; }
        public int QualityScore { get; set; }
        public int StockoutPenalty { get; set; }
        public int DailyRating { get; set; }

        // แสดงผล
        public string StarDisplay { get; set; } = "☆☆☆☆☆";
        public int NewTotalRating { get; set; }    // TotalRating หลังบวกวันนี้แล้ว
    }

    public class MenuSalesStats
    {
        public string MenuName { get; set; }
        public int Quantity { get; set; }
        public double Revenue { get; set; }
        public double UnitCost { get; set; }
    }
}
