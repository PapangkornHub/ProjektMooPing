using ProjektMooPing.Models;
using ProjektMooPing.Services;

namespace ProjektMooPing.View;

public partial class CutScenePage : ContentPage
{
    private readonly List<StoryCutScene> _scenes;
    private int _currentIndex = 0;
    private CancellationTokenSource? _typewriterCts;
    private bool _isTyping = false;
    private string _fullText = string.Empty;
    private readonly TaskCompletionSource _dismissed = new();

    public Task WaitForDismissAsync() => _dismissed.Task;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _dismissed.TrySetResult();
    }

    // milliseconds per character
    private const int TypewriterDelayMs = 35;

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
        StoryImage.Source = scene.DisplayImagePath;

        bool isLast = index >= _scenes.Count - 1;
        ActionBtn.Text = isLast ? L.BtnClose : L.BtnNext;

        PageIndicatorLabel.IsVisible = _scenes.Count > 1;
        PageIndicatorLabel.Text = $"{index + 1} / {_scenes.Count}";

        _fullText = scene.DisplayText;
        TextLabel.Text = string.Empty;
        SoundService.PlayTypewriter();
        StartTypewriter(_fullText);
    }

    private void StartTypewriter(string text)
    {
        _typewriterCts?.Cancel();
        _typewriterCts = new CancellationTokenSource();
        _isTyping = true;
        _ = RunTypewriterAsync(text, _typewriterCts.Token);
    }

    private async Task RunTypewriterAsync(string text, CancellationToken ct)
    {
        try
        {
            for (int i = 1; i <= text.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                TextLabel.Text = text[..i];
                await Task.Delay(TypewriterDelayMs, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isTyping = false;
            SoundService.StopTypewriter();
        }
    }

    private async void OnActionClicked(object sender, EventArgs e)
    {
        SoundService.PlayClick1();

        // Skip typewriter → show full text first
        if (_isTyping)
        {
            _typewriterCts?.Cancel();
            _isTyping = false;
            SoundService.StopTypewriter();
            TextLabel.Text = _fullText;
            return;
        }

        var btn = (Button)sender;
        btn.IsEnabled = false;

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
