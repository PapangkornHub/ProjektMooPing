using System;
using System.Collections.Generic;
using System.Text;

namespace ProjektMooPing.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameTh { get; set; }
        public string Category { get; set; }
        public double BaseCost { get; set; }
        public int BasePopularity { get; set; }
        public string Icon { get; set; }

        /// <summary>ชื่อตามภาษาที่เลือกอยู่ (อ่านจาก LocalizationService)</summary>
        public string DisplayName =>
            ProjektMooPing.Services.LocalizationService.Instance.IsThai && !string.IsNullOrEmpty(NameTh)
                ? NameTh : Name;
    }
}
