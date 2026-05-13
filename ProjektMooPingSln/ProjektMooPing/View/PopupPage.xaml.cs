namespace ProjektMooPing.View;

public partial class PopupPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>
    /// รอผลจากผู้เล่น — true = กด Confirm, false = กด Cancel
    /// </summary>
    public Task<bool> Result => _tcs.Task;

    // ---------------------------------------------------------------
    // Constructor หลัก
    // cancelText = null  →  Info mode  (ปุ่มเดียว, OK กลาง)
    // cancelText != null →  Confirm mode (2 ปุ่ม YES / NO)
    // ---------------------------------------------------------------
    public PopupPage(string icon, string title, string message,
                     string confirmText = "OK", string cancelText = null)
    {
        InitializeComponent();

        IconLabel.Text    = icon;
        TitleLabel.Text   = title;
        MessageLabel.Text = message;
        ConfirmButton.Text = confirmText;

        if (cancelText == null)
        {
            // --- Info mode: ปุ่ม OK คนเดียว อยู่กลาง ---
            CancelButton.IsVisible = false;
            Grid.SetColumn(ConfirmButton, 0);
            Grid.SetColumnSpan(ConfirmButton, 2);
        }
        else
        {
            // --- Confirm mode: 2 ปุ่ม ---
            CancelButton.Text = cancelText;
        }
    }

    // ---------------------------------------------------------------
    // Static helpers — เรียกใช้แทน DisplayAlert ได้ทันที
    // ---------------------------------------------------------------

    /// <summary>Info popup — 1 ปุ่ม OK</summary>
    public static async Task ShowInfo(Page page, string icon, string title,
                                      string message, string okText = "OK")
    {
        var popup = new PopupPage(icon, title, message, okText);
        await page.Navigation.PushModalAsync(popup, animated: false);
        await popup.Result;
    }

    /// <summary>Confirm popup — 2 ปุ่ม YES/NO คืนค่า true ถ้ากด Confirm</summary>
    public static async Task<bool> ShowConfirm(Page page, string icon, string title,
                                               string message,
                                               string yesText = "YES", string noText = "NO")
    {
        var popup = new PopupPage(icon, title, message, yesText, noText);
        await page.Navigation.PushModalAsync(popup, animated: false);
        return await popup.Result;
    }

    // ---------------------------------------------------------------
    // Button Handlers
    // ---------------------------------------------------------------
    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: false); // pop ก่อน
        _tcs.TrySetResult(true);                        // แล้วค่อย signal caller
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: false); // pop ก่อน
        _tcs.TrySetResult(false);                       // แล้วค่อย signal caller
    }
}