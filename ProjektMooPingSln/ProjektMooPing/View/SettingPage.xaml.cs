using CommunityToolkit.Mvvm.Messaging;
using ProjektMooPing.Models;
using ProjektMooPing.Services;

namespace ProjektMooPing.View;

public partial class SettingPage : ContentPage
{
    public SettingPage()
    {
        InitializeComponent();
        string version = AppInfo.Current.VersionString;
        string build   = AppInfo.Current.BuildString;
        VersionLabel.Text = $"v{version} (Build {build})";

        UpdateLanguageButtons();
    }

    // ─── Language Buttons ──────────────────────────────────────────────
    private void OnThaiClicked(object sender, EventArgs e)
    {
        SoundService.PlayClick1();
        LocalizationService.Instance.IsThai = true;
        UpdateLanguageButtons();
    }

    private void OnEnglishClicked(object sender, EventArgs e)
    {
        SoundService.PlayClick1();
        LocalizationService.Instance.IsThai = false;
        UpdateLanguageButtons();
    }

    private void UpdateLanguageButtons()
    {
        var active   = (Color)Application.Current.Resources["MooPingDarkBrown"];
        var inactive = Color.FromArgb("#CCCCCC");

        if (LocalizationService.Instance.IsThai)
        {
            BtnThai.BackgroundColor    = active;
            BtnThai.TextColor          = Colors.White;
            BtnEnglish.BackgroundColor = inactive;
            BtnEnglish.TextColor       = Colors.Black;
        }
        else
        {
            BtnEnglish.BackgroundColor = active;
            BtnEnglish.TextColor       = Colors.White;
            BtnThai.BackgroundColor    = inactive;
            BtnThai.TextColor          = Colors.Black;
        }
    }

    // ─── Reset Game ────────────────────────────────────────────────────
    private async void OnResetGameClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClick1();
        var loc = LocalizationService.Instance;
        bool answer = await PopupPage.ShowConfirm(this, "⚠️",
            loc.SettingResetTitle, loc.SettingResetMsg,
            loc.BtnDeleteConfirm, loc.BtnCancel);

        if (answer)
        {
            SoundService.PlayDelete();
            SaveService.DeleteSave();
            WeakReferenceMessenger.Default.Send(new ResetGameMessage());
            await Navigation.PopModalAsync();
        }
        else { btn.IsEnabled = true; }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClickB();
        await Navigation.PopModalAsync();
    }
}
