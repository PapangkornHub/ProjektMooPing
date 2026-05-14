using CommunityToolkit.Mvvm.Messaging;
using ProjektMooPing.Models;
using ProjektMooPing.Services;

namespace ProjektMooPing.View;

public partial class DiscoverPage : ContentPage
{
    private List<QuestionData> _allQuestions;
    private List<QuestionData> _currentSessionQuestions;
    private int _currentIndex = 0;
    private int _score = 0;
    private PlayerProfile _player;
    private List<Ingredient> _allIngredients;
    private Ingredient _potentialReward;

    public DiscoverPage(PlayerProfile player, List<QuestionData> questions, List<Ingredient> ingredients)
    {
        InitializeComponent();
        _player = player;
        _allQuestions = questions;
        _allIngredients = ingredients;
        _currentSessionQuestions = _allQuestions.OrderBy(x => Guid.NewGuid()).Take(10).ToList();
        SetupPotentialReward();
        ShowQuestion();
    }

    private void SetupPotentialReward()
    {
        var lockedIngredients = _allIngredients
            .Where(i => !_player.UnlockedIngredientIds.Contains(i.Id))
            .ToList();

        if (lockedIngredients.Any())
        {
            _potentialReward = lockedIngredients[new Random().Next(lockedIngredients.Count)];
            RewardHintLabel.Text = LocalizationService.Instance.FmtGoal(_potentialReward.DisplayName);
        }
        else
        {
            RewardHintLabel.Text = LocalizationService.Instance.LblGoalComplete;
        }
    }

    private void ShowQuestion()
    {
        var q = _currentSessionQuestions[_currentIndex];
        var loc = LocalizationService.Instance;
        QuestionCountLabel.Text = loc.FmtQuestionCount(_currentIndex + 1, 10);
        ScoreLabel.Text = loc.FmtScore(_score);
        QuestionLabel.Text = q.DisplayQuestion;   // ภาษาตาม LocalizationService
        ChoicesContainer.Clear();

        var choices = q.DisplayChoices;           // List<string> ตามภาษา
        for (int i = 0; i < choices.Count; i++)
        {
            var btn = new Button
            {
                Text = choices[i],
                Margin = new Thickness(0, 5),
                CornerRadius = 15,
                HeightRequest = 50,
                BackgroundColor = Color.FromArgb("#F0EBE0"),
                TextColor = Colors.Black,
                BorderColor = Color.FromArgb("#5D4037"),
                BorderWidth = 1
            };

            int index = i;
            btn.Clicked += (s, e) => CheckAnswer(index);
            ChoicesContainer.Add(btn);
        }
    }

    private async void CheckAnswer(int selectedIndex)
    {
        var q = _currentSessionQuestions[_currentIndex];
        var loc = LocalizationService.Instance;
        var choices = q.DisplayChoices;

        ChoicesContainer.IsVisible = false;
        if (selectedIndex == q.CorrectIndex)
        {
            SoundService.PlayClick1();
            _score++;
            QuestionLabel.Text = loc.IsThai ? "✅ ถูกต้อง!" : "✅ Correct!";
            QuestionLabel.TextColor = Color.FromArgb("#76B041");
        }
        else
        {
            SoundService.PlayClick2();
            string correctAnswer = choices[q.CorrectIndex];
            QuestionLabel.Text = loc.IsThai
                ? $"❌ ผิด! (คำตอบ: {correctAnswer})"
                : $"❌ Wrong! (Answer: {correctAnswer})";
            QuestionLabel.TextColor = Color.FromArgb("#B22222");
        }

        TriviaLabel.Text = q.DisplayTrivia;       // Trivia ตามภาษา
        TriviaLabel.IsVisible = true;
        NextButton.IsVisible = true;
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        _currentIndex++;

        if (_currentIndex < 10)
        {
            TriviaLabel.IsVisible = false;
            NextButton.IsVisible = false;
            ChoicesContainer.IsVisible = true;
            QuestionLabel.TextColor = Colors.Black;
            SoundService.PlayClick1();
            ShowQuestion();
            btn.IsEnabled = true;
        }
        else
        {
            await FinishQuiz();
        }
    }

    private async Task FinishQuiz()
    {
        bool passed = _score >= 6;
        string icon, title, resultMsg;
        var loc = LocalizationService.Instance;

        resultMsg = loc.FmtQuizScore(_score) + "\n";

        if (passed)
        {
            if (_potentialReward != null)
            {
                _player.UnlockedIngredientIds.Add(_potentialReward.Id);
                icon  = "🎉";
                title = loc.QuizPassTitle;
                resultMsg += loc.FmtQuizPass(_potentialReward.DisplayName);

                // แจ้ง MainGamePage ให้รี Inventory UI ทันที
                WeakReferenceMessenger.Default.Send(new IngredientUnlockedMessage(_potentialReward));
            }
            else
            {
                icon  = "✅";
                title = loc.QuizPassTitle;
                resultMsg += loc.QuizAllUnlocked;
            }
        }
        else
        {
            SoundService.PlayClickB();
            icon  = "😢";
            title = loc.QuizFailTitle;
            resultMsg += loc.QuizFailMsg;
        }

        await PopupPage.ShowInfo(this, icon, title, resultMsg);
        await Navigation.PopModalAsync();
    }

    private async void OnExitClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        SoundService.PlayClickB();
        var loc = LocalizationService.Instance;
        bool confirm = await PopupPage.ShowConfirm(this, "❓", loc.QuizQuitTitle, loc.QuizQuitMsg, loc.BtnQuit, loc.BtnStay);
        if (confirm)
            await Navigation.PopModalAsync();
        else
            btn.IsEnabled = true;
    }
}