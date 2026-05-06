using System.Windows;
using System.Windows.Input;

namespace Pic.App;

public partial class FloatingToolbar : Window
{
    private readonly App _app;

    public FloatingToolbar(App app)
    {
        InitializeComponent();
        _app = app;

        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen != null)
        {
            Left = (screen.WorkingArea.Width - Width) / 2;
            Top = screen.WorkingArea.Height - Height - 60;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnRegion_Click(object sender, RoutedEventArgs e) => _app.CaptureRegion();
    private void BtnFullScreen_Click(object sender, RoutedEventArgs e) => _app.CaptureFullScreen();
    private void BtnWindow_Click(object sender, RoutedEventArgs e) => _app.CaptureActiveWindow();
    private void BtnScrolling_Click(object sender, RoutedEventArgs e) => _app.CaptureScrolling();
    private void BtnRecording_Click(object sender, RoutedEventArgs e) => _app.StartRecording();
    private void BtnSettings_Click(object sender, RoutedEventArgs e) => ShowSettings();
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private static void ShowSettings()
    {
        var settings = Pic.Shared.AppSettings.Load();
        var win = new SettingsWindow(settings);
        win.SettingsSaved += (s, s2) => s2.Save();
        win.Show();
    }
}
