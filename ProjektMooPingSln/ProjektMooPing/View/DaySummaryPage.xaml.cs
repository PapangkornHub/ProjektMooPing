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
        TotalRatingLabel.Text = loc.FmtTotalRating(summary.NewTotalRating);
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        SoundService.PlayClickB();
        await Navigation.PopModalAsync();
    }
}