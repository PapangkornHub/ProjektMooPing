using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjektMooPing.ViewModel
{
    public class IngredientViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameTh { get; set; }
        /// <summary>ชื่อตามภาษาที่เลือก — ใช้ใน XAML binding</summary>
        public string DisplayName =>
            ProjektMooPing.Services.LocalizationService.Instance.IsThai && !string.IsNullOrEmpty(NameTh)
                ? NameTh : Name;
        public string Category { get; set; }
        public double BaseCost { get; set; }
        public int BasePopularity { get; set; }
        public string Icon { get; set; }
        public string PriceDisplay => $"{BaseCost} ฿ / unit";

        #region --- Rarity Logic ---
        public string RarityName => BasePopularity switch
        {
            >= 100 => "Legendary",
            >= 80 => "Epic",
            >= 60 => "Rare",
            _ => "Common"
        };

        public Color RarityColor => BasePopularity switch
        {
            >= 100 => Color.FromArgb("#FF8C00"), // ส้ม
            >= 80 => Color.FromArgb("#A335EE"), // ม่วง
            >= 60 => Color.FromArgb("#0070DD"), // ฟ้า
            _ => Color.FromArgb("#555555")      // เทาเข้ม
        };
        #endregion

        #region --- Inventory Logic ---
        private int _ownedAmount;
        public int OwnedAmount
        {
            get => _ownedAmount;
            set { _ownedAmount = value; OnPropertyChanged(); }
        }

        private int _orderAmount;
        public int OrderAmount
        {
            get => _orderAmount;
            set
            {
                _orderAmount = Math.Max(0, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCost));
            }
        }
        public double TotalCost => BaseCost * OrderAmount;
        #endregion

        #region --- Edit Logic ---
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set 
            { 
                _isSelected = value; OnPropertyChanged();
            }
        }
        #endregion

        #region --- Property Changed ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}