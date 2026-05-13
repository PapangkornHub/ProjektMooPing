namespace ProjektMooPing.View;

public partial class SplashPage : ContentPage
{
	public SplashPage()
	{
		InitializeComponent();
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(2500);

        Application.Current.MainPage = new AppShell();
    }
}