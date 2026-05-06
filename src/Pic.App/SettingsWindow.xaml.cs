using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Pic.Shared;

namespace Pic.App;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Dictionary<Pic.Shared.CaptureMode, (System.Windows.Controls.TextBox TextBox, System.Windows.Controls.Button RecordBtn)> _hotkeyControls = new();
    private Pic.Shared.CaptureMode? _recordingHotkey;

    public event EventHandler<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        TxtSaveDir.Text = settings.SaveDirectory;
        ChkAutoCopy.IsChecked = settings.AutoCopyToClipboard;
        ChkOpenEditor.IsChecked = settings.OpenEditorAfterCapture;
        ChkEditorTopmost.IsChecked = settings.EditorTopmost;
        ChkShowToolbar.IsChecked = settings.ShowToolbarOnStartup;
        TxtDelaySeconds.Text = settings.DelaySeconds.ToString();
        TxtRecordDir.Text = settings.RecordingsDirectory;
        SldJpegQuality.Value = settings.JpegQuality;
        LblJpegQuality.Text = settings.JpegQuality.ToString();

        foreach (ComboBoxItem item in CmbFormat.Items)
        {
            if (item.Tag?.ToString() == settings.DefaultImageFormat.ToString())
            {
                CmbFormat.SelectedItem = item;
                break;
            }
        }

        foreach (ComboBoxItem item in CmbFrameRate.Items)
        {
            if (item.Tag?.ToString() == settings.RecordingFrameRate.ToString())
            {
                CmbFrameRate.SelectedItem = item;
                break;
            }
        }

        BuildHotkeyRows();

        KeyDown += OnKeyDown;
    }

    private void BuildHotkeyRows()
    {
        HotkeyPanel.Children.Clear();
        _hotkeyControls.Clear();

        var modes = new[]
        {
            (Pic.Shared.CaptureMode.FullScreen, "Full Screen"),
            (Pic.Shared.CaptureMode.ActiveWindow, "Active Window"),
            (Pic.Shared.CaptureMode.Region, "Region"),
            (Pic.Shared.CaptureMode.Scrolling, "Scrolling"),
            (Pic.Shared.CaptureMode.Delayed, "Delayed"),
            (Pic.Shared.CaptureMode.ScreenRecording, "Recording"),
        };

        foreach (var (mode, label) in modes)
        {
            var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };

            var lbl = new TextBlock
            {
                Text = label + ":",
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(lbl, Dock.Left);
            row.Children.Add(lbl);

            var keyText = _settings.Hotkeys.GetValueOrDefault(mode, "");
            var txt = new System.Windows.Controls.TextBox
            {
                Text = keyText,
                Width = 160,
                IsReadOnly = true,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55)),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(txt, Dock.Left);
            row.Children.Add(txt);

            var btn = new System.Windows.Controls.Button
            {
                Content = "Record",
                Width = 60,
                Height = 22,
                Margin = new Thickness(4, 0, 0, 0),
                FontSize = 11,
                Tag = mode,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x55, 0x55, 0x55)),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += RecordHotkey_Click;
            DockPanel.SetDock(btn, Dock.Left);
            row.Children.Add(btn);

            HotkeyPanel.Children.Add(row);
            _hotkeyControls[mode] = (txt, btn);
        }
    }

    private void RecordHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not Pic.Shared.CaptureMode mode) return;

        if (_recordingHotkey == mode)
        {
            _recordingHotkey = null;
            btn.Content = "Record";
            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));
            return;
        }

        foreach (var kvp in _hotkeyControls)
        {
            kvp.Value.RecordBtn.Content = "Record";
            kvp.Value.RecordBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));
        }

        _recordingHotkey = mode;
        btn.Content = "Press key...";
        btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0x33, 0x33));
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_recordingHotkey == null) return;

        e.Handled = true;

        var modifiers = "";
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers += "Ctrl+";
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers += "Alt+";
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers += "Shift+";
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers += "Win+";

        var keyName = e.Key.ToString();
        if (keyName.StartsWith("D") && keyName.Length == 2 && char.IsDigit(keyName[1]))
            keyName = keyName[1].ToString();
        if (e.Key == Key.Snapshot) keyName = "PrintScreen";
        if (e.Key == Key.System) keyName = "";

        if (!string.IsNullOrEmpty(keyName) && keyName != "None" && keyName != "LeftCtrl"
            && keyName != "RightCtrl" && keyName != "LeftAlt" && keyName != "RightAlt"
            && keyName != "LeftShift" && keyName != "RightShift" && keyName != "LWin" && keyName != "RWin")
        {
            var hotkeyStr = modifiers + keyName;

            if (_hotkeyControls.TryGetValue(_recordingHotkey.Value, out var ctrl))
            {
                ctrl.TextBox.Text = hotkeyStr;
                _settings.Hotkeys[_recordingHotkey.Value] = hotkeyStr;
            }

            if (_hotkeyControls.TryGetValue(_recordingHotkey.Value, out var ctrl2))
            {
                ctrl2.RecordBtn.Content = "Record";
                ctrl2.RecordBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));
            }

            _recordingHotkey = null;
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

    private void BrowseRecordDir_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select save directory for recordings",
            SelectedPath = _settings.RecordingsDirectory,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtRecordDir.Text = dialog.SelectedPath;
        }
    }

    private void SldJpegQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        LblJpegQuality.Text = ((int)e.NewValue).ToString();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.SaveDirectory = TxtSaveDir.Text;
        _settings.AutoCopyToClipboard = ChkAutoCopy.IsChecked == true;
        _settings.OpenEditorAfterCapture = ChkOpenEditor.IsChecked == true;
        _settings.EditorTopmost = ChkEditorTopmost.IsChecked == true;
        _settings.ShowToolbarOnStartup = ChkShowToolbar.IsChecked == true;
        _settings.RecordingsDirectory = TxtRecordDir.Text;
        _settings.JpegQuality = (int)SldJpegQuality.Value;

        if (int.TryParse(TxtDelaySeconds.Text, out var delay) && delay > 0 && delay <= 30)
            _settings.DelaySeconds = delay;

        if (CmbFormat.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            _settings.DefaultImageFormat = Enum.Parse<ImageFormat>(item.Tag.ToString()!);
        }

        if (CmbFrameRate.SelectedItem is ComboBoxItem fpsItem && fpsItem.Tag != null)
        {
            if (int.TryParse(fpsItem.Tag.ToString(), out var fps))
                _settings.RecordingFrameRate = fps;
        }

        SettingsSaved?.Invoke(this, _settings);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
