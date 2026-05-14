using Microsoft.Maui.Controls;

namespace ProjektMooPing.Models
{
    public class Location : BindableObject
    {
        public int    Id             { get; set; }
        public string LocName        { get; set; }
        public string LocNameTh      { get; set; }
        public int    WeeklyRent     { get; set; }
        public int    UnlockCost     { get; set; }
        public int    RequiredRating { get; set; }
        public int    MinTraffic     { get; set; }
        public int    MaxTraffic     { get; set; }
        public string ImagePath      { get; set; }
        public string DogLucky       { get; set; }
        public string DogLuckyTh     { get; set; }

        // --- Story CutScene ---
        public string Title          { get; set; }
        public string TitleTh        { get; set; }
        public string Text           { get; set; }
        public string TextTh         { get; set; }
        public string StoryImagePath { get; set; }

        public bool HasStoryCutScene => !string.IsNullOrEmpty(StoryImagePath);
        public string DisplayStoryTitle =>
            Loc.IsThai && !string.IsNullOrEmpty(TitleTh) ? TitleTh : Title;
        public string DisplayStoryText =>
            Loc.IsThai && !string.IsNullOrEmpty(TextTh) ? TextTh : Text;
        public string DisplayStoryImagePath =>
            string.IsNullOrEmpty(StoryImagePath) ? null
            : StoryImagePath.EndsWith(".png") ? StoryImagePath : StoryImagePath + ".png";

        private static ProjektMooPing.Services.LocalizationService Loc
            => ProjektMooPing.Services.LocalizationService.Instance;

        public string DisplayName =>
            Loc.IsThai && !string.IsNullOrEmpty(LocNameTh) ? LocNameTh : LocName;

        public int GetDailyTraffic()
        {
            var rng = new System.Random();
            return rng.Next(MinTraffic, MaxTraffic + 1);
        }

        public string TrafficRangeDisplay =>
            Loc.IsThai
                ? $"{MinTraffic} - {MaxTraffic} คน / วัน"
                : $"{MinTraffic} - {MaxTraffic} People / Day";

        public string WeeklyRentDisplay =>
            WeeklyRent == 0
                ? (Loc.IsThai ? "ค่าเช่า: ฟรี" : "Rent: Free")
                : (Loc.IsThai ? $"ค่าเช่า: {WeeklyRent:N0}฿ / สัปดาห์" : $"Rent: {WeeklyRent:N0}฿ / Week");

        public string UnlockCostDisplay =>
            UnlockCost == 0
                ? (Loc.IsThai ? "ฟรี" : "Free")
                : $"{UnlockCost:N0}฿";
    }
}
