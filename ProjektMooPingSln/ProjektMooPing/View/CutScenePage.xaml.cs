using ProjektMooPing.Models;
using ProjektMooPing.Services;

namespace ProjektMooPing.View;

public partial class CutScenePage : ContentPage
{
    private readonly List<StoryCutScene> _scenes;
    private int _currentIndex = 0;

    public CutScenePage(List<StoryCutScene> scenes)
    {
        InitializeComponent();
        _scenes = scenes;
        ShowScene(0);
    }

    private void ShowScene(int index)
    {
        var scene = _scenes[index];
        var L = LocalizationService.Instance;

        TitleLabel.Text = scene.DisplayTitle;
        TextLabel.Text  = scene.DisplayText;
        StoryImage.Source = scene.DisplayImagePath;

        bool isLast = index >= _scenes.Count - 1;
        ActionBtn.Text = isLast ? L.BtnClose : L.BtnNext;

        PageIndicatorLabel.IsVisible = _scenes.Count > 1;
        PageIndicatorLabel.Text = $"{index + 1} / {_scenes.Count}";
    }

    private async void OnActionClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClick1();

        _currentIndex++;
        if (_currentIndex < _scenes.Count)
        {
            ShowScene(_currentIndex);
            btn.IsEnabled = true;
        }
        else
        {
            await Navigation.PopModalAsync();
        }
    }
}
