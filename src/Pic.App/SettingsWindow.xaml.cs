using System.Windows;
using System.Windows.Controls;
using Pic.Shared;

namespace Pic.App;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public event EventHandler<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        TxtSaveDir.Text = settings.SaveDirectory;
        ChkAutoCopy.IsChecked = settings.AutoCopyToClipboard;
        ChkOpenEditor.IsChecked = settings.OpenEditorAfterCapture;
        ChkShowToolbar.IsChecked = settings.ShowToolbarOnStartup;

        foreach (ComboBoxItem item in CmbFormat.Items)
        {
            if (item.Tag?.ToString() == settings.DefaultImageFormat.ToString())
            {
                CmbFormat.SelectedItem = item;
                break;
            }
        }
    }

    private void BrowseSaveDir_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select save directory for screenshots",
            SelectedPath = _settings.SaveDirectory,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtSaveDir.Text = dialog.SelectedPath;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.SaveDirectory = TxtSaveDir.Text;
        _settings.AutoCopyToClipboard = ChkAutoCopy.IsChecked == true;
        _settings.OpenEditorAfterCapture = ChkOpenEditor.IsChecked == true;
        _settings.ShowToolbarOnStartup = ChkShowToolbar.IsChecked == true;

        if (CmbFormat.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            _settings.DefaultImageFormat = Enum.Parse<ImageFormat>(item.Tag.ToString()!);
        }

        SettingsSaved?.Invoke(this, _settings);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
