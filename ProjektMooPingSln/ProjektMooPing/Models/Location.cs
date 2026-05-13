using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls;

namespace ProjektMooPing.Models
{
    public class Location : BindableObject
    {
        public int Id { get; set; }
        public string LocName { get; set; }
        public string LocNameTh { get; set; }
        public int DailyRent { get; set; }
        public int MinTraffic { get; set; }
        public int MaxTraffic { get; set; }
        public string ImagePath { get; set; }

        private static ProjektMooPing.Services.LocalizationService Loc
            => ProjektMooPing.Services.LocalizationService.Instance;

        /// <summary>ชื่อสถานที่ตามภาษาที่เลือก</summary>
        public string DisplayName =>
            Loc.IsThai && !string.IsNullOrEmpty(LocNameTh) ? LocNameTh : LocName;

        public int GetDailyTraffic()
        {
            Random random = new Random();
            return random.Next(MinTraffic, MaxTraffic + 1);
        }

        public string TrafficRangeDisplay =>
            Loc.IsThai
                ? $"{MinTraffic} - {MaxTraffic} คน / วัน"
                : $"{MinTraffic} - {MaxTraffic} People / Day";

        public string RentDisplay =>
            DailyRent == 0
                ? (Loc.IsThai ? "ค่าเช่า: ฟรี" : "Rent: Free")
                : (Loc.IsThai ? $"ค่าเช่า: {DailyRent}฿" : $"Rent: {DailyRent}฿");
    }
}