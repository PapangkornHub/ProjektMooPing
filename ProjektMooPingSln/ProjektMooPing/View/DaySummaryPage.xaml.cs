using ProjektMooPing.Models;
using ProjektMooPing.Services;

namespace ProjektMooPing.View;

public partial class DaySummaryPage : ContentPage
{
    public DaySummaryPage(DaySummary summary)
    {
        InitializeComponent();
        BindingContext = summary;
        var loc = LocalizationService.Instance;
        SummaryTitleLabel.Text = loc.FmtSummaryTitle(summary.DayNumber);
        DailyPtsLabel.Text = loc.FmtDailyPts(summary.DailyRating);
        DailyPtsLabel.TextColor = summary.DailyRating < 0 ? Color.FromArgb("#CC3333") : Color.FromArgb("#76B041");
        TotalRatingLabel.Text = loc.FmtTotalRating(summary.NewTotalRating);
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClickB();
        await Navigation.PopModalAsync();
    }
}