using Microsoft.Extensions.DependencyInjection;

namespace ProjektMooPing
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override async void OnStart()
        {
            base.OnStart();
            await Task.Delay(200);
            await Shell.Current.GoToAsync("//MainGamePage");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.Created += (s, e) =>
            {
                const int width = 412;
                const int height = 915;

                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window.Handler.PlatformView as Microsoft.UI.Xaml.Window);
                var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                
                appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            };
#endif
            return window;
        }
    }
}