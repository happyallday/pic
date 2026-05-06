using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pic.Shared;

namespace Pic.App;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private FloatingToolbar? _floatingToolbar;
    private SettingsWindow? _settingsWindow;
    private readonly AppSettings _settings = AppSettings.Load();
    private HotkeyManager? _hotkeyManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            EnsureSingleInstance();
            CreateTrayIcon();
            _hotkeyManager = new HotkeyManager();
            RegisterHotkeys();

            if (_settings.ShowToolbarOnStartup)
                ShowToolbar();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup failed: {ex.Message}", "Pic",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void EnsureSingleInstance()
    {
        using var mutex = new System.Threading.Mutex(true, "Pic_ScreenshotApp_Mutex", out var createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("Pic is already running. Check the system tray.", "Pic",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(0);
            return;
        }
        GC.KeepAlive(mutex);
    }

    private void CreateTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Pic - Screenshot Tool",
            Visible = true,
            ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
        };

        _notifyIcon.DoubleClick += (s, e) => ShowToolbar();

        var menu = _notifyIcon.ContextMenuStrip;
        menu.Items.Add("Show Toolbar", null, (s, e) => ShowToolbar());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var captureMenu = new System.Windows.Forms.ToolStripMenuItem("Capture");
        captureMenu.DropDownItems.Add("Full Screen", null, (s, e) => CaptureFullScreen());
        captureMenu.DropDownItems.Add("Active Window", null, (s, e) => CaptureActiveWindow());
        captureMenu.DropDownItems.Add("Region", null, (s, e) => CaptureRegion());
        captureMenu.DropDownItems.Add("Scrolling Window", null, (s, e) => CaptureScrolling());
        captureMenu.DropDownItems.Add("Delayed Capture", null, (s, e) => CaptureDelayed());
        menu.Items.Add(captureMenu);

        menu.Items.Add("Screen Recording", null, (s, e) => StartRecording());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Settings", null, (s, e) => ShowSettings());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (s, e) => ExitApplication());
    }

    private void RegisterHotkeys()
    {
        if (_hotkeyManager == null) return;

        _hotkeyManager.RegisterHotkey(CaptureMode.FullScreen,
            ParseKey(_settings.Hotkeys.GetValueOrDefault(CaptureMode.FullScreen, "PrintScreen")),
            () => CaptureFullScreen());

        _hotkeyManager.RegisterHotkey(CaptureMode.ActiveWindow,
            ParseKey(_settings.Hotkeys.GetValueOrDefault(CaptureMode.ActiveWindow, "Alt+PrintScreen")),
            () => CaptureActiveWindow());

        _hotkeyManager.RegisterHotkey(CaptureMode.Region,
            ParseKey(_settings.Hotkeys.GetValueOrDefault(CaptureMode.Region, "Ctrl+Shift+X")),
            () => CaptureRegion());

        _hotkeyManager.RegisterHotkey(CaptureMode.Scrolling,
            ParseKey(_settings.Hotkeys.GetValueOrDefault(CaptureMode.Scrolling, "Ctrl+Shift+S")),
            () => CaptureScrolling());

        _hotkeyManager.RegisterHotkey(CaptureMode.Delayed,
            ParseKey(_settings.Hotkeys.GetValueOrDefault(CaptureMode.Delayed, "Ctrl+Shift+D")),
            () => CaptureDelayed());

        _hotkeyManager.RegisterHotkey(CaptureMode.ScreenRecording,
            ParseKey(_settings.Hotkeys.GetValueOrDefault(CaptureMode.ScreenRecording, "Ctrl+Shift+R")),
            () => StartRecording());
    }

    private static HotkeyManager.Hotkey ParseKey(string keyStr) => HotkeyManager.ParseHotkeyString(keyStr);

    private void ShowToolbar()
    {
        if (_floatingToolbar == null)
        {
            _floatingToolbar = new FloatingToolbar(this);
            _floatingToolbar.Closed += (s, e) => _floatingToolbar = null;
        }
        _floatingToolbar.Show();
        _floatingToolbar.Activate();
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(_settings);
            _settingsWindow.SettingsSaved += (s, settings) =>
            {
                settings.Save();
                UpdateHotkeyRegistration();
            };
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void CaptureFullScreen() => CaptureWithResult(() =>
    {
        var capture = new Pic.Capture.ScreenCapture();
        return capture.CaptureFullScreen();
    });

    public void CaptureActiveWindow() => CaptureWithResult(() =>
    {
        var capture = new Pic.Capture.ScreenCapture();
        return capture.CaptureActiveWindow();
    });

    public void CaptureRegion()
    {
        System.Threading.Thread.Sleep(300);
        var selector = new Pic.Capture.RegionSelector();
        var bitmap = selector.SelectAndCapture();
        HandleCaptureResult(bitmap);
    }

    public void CaptureScrolling()
    {
        System.Threading.Thread.Sleep(300);
        var hWnd = Pic.Capture.NativeMethods.GetForegroundWindow();
        var sc = new Pic.Capture.ScrollingCapture();
        var bitmap = sc.CaptureScroll(hWnd);
        HandleCaptureResult(bitmap);
    }

    public void CaptureDelayed()
    {
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.DelaySeconds));
            Dispatcher.Invoke(() => FullScreenWithDelay());
        });
    }

    private void FullScreenWithDelay()
    {
        var capture = new Pic.Capture.ScreenCapture();
        var bitmap = capture.CaptureFullScreen();
        HandleCaptureResult(bitmap);
    }

    private void CaptureWithResult(Func<System.Drawing.Bitmap> captureFunc)
    {
        System.Threading.Thread.Sleep(300);

        System.Drawing.Bitmap? bitmap = null;
        try
        {
            bitmap = captureFunc();
            HandleCaptureResult(bitmap);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Capture failed: {ex.Message}", "Pic", MessageBoxButton.OK, MessageBoxImage.Error);
            bitmap?.Dispose();
        }
    }

    private void HandleCaptureResult(System.Drawing.Bitmap? bitmap)
    {
        if (bitmap == null) return;

        try
        {
            if (_settings.AutoCopyToClipboard)
            {
                CopyToClipboard(bitmap);
            }

            if (_settings.OpenEditorAfterCapture)
            {
                OpenEditor(bitmap);
            }
            else
            {
                SaveBitmap(bitmap);
                bitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Pic", MessageBoxButton.OK, MessageBoxImage.Error);
            bitmap.Dispose();
        }
    }

    private void OpenEditor(System.Drawing.Bitmap bitmap)
    {
        var bitmapSource = ConvertToBitmapSource(bitmap);
        bitmap.Dispose();

        Dispatcher.Invoke(() =>
        {
            var editor = new Pic.Editor.ImageEditorWindow(bitmapSource);
            editor.Show();
            editor.Activate();
        });
    }

    private void SaveBitmap(System.Drawing.Bitmap bitmap)
    {
        var dir = _settings.SaveDirectory;
        Directory.CreateDirectory(dir);
        var ext = _settings.DefaultImageFormat.ToString().ToLower();
        var filename = Path.Combine(dir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}");

        var format = Pic.Capture.ScreenCapture.GetImageFormat(_settings.DefaultImageFormat);
        if (_settings.DefaultImageFormat == Pic.Shared.ImageFormat.Jpeg)
        {
            var codec = GetEncoderInfo("image/jpeg");
            if (codec != null)
            {
                var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality, (long)_settings.JpegQuality);
                bitmap.Save(filename, codec, encoderParams);
            }
            else
            {
                bitmap.Save(filename, format);
            }
        }
        else
        {
            bitmap.Save(filename, format);
        }

        _notifyIcon?.ShowBalloonTip(2000, "Pic", $"Saved: {Path.GetFileName(filename)}",
            System.Windows.Forms.ToolTipIcon.Info);
    }

    private static void CopyToClipboard(System.Drawing.Bitmap bitmap)
    {
        try
        {
            using var bmp = new System.Drawing.Bitmap(bitmap);
            System.Windows.Forms.Clipboard.SetImage(bmp);
        }
        catch { }
    }

    private static BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
    {
        var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
            bitmap.PixelFormat);

        try
        {
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgra32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static System.Drawing.Imaging.ImageCodecInfo? GetEncoderInfo(string mimeType)
    {
        return System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.MimeType == mimeType);
    }

    private Pic.Recorder.ScreenRecorder? _recorder;

    public void StartRecording()
    {
        if (_recorder?.IsRecording == true)
        {
            _recorder.Stop();
            return;
        }

        System.Threading.Thread.Sleep(300);

        var selector = new Pic.Capture.RegionSelector();
        var preview = selector.SelectAndCapture();
        preview?.Dispose();

        var rect = System.Windows.Forms.Cursor.Position;
        var captureRegion = new System.Drawing.Rectangle(rect.X - 200, rect.Y - 150, 400, 300);

        var dir = _settings.RecordingsDirectory;
        Directory.CreateDirectory(dir);
        var filename = Path.Combine(dir, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

        _recorder = new Pic.Recorder.ScreenRecorder(captureRegion, filename, _settings.RecordingFrameRate);
        _recorder.RecordingStarted += (s, path) =>
        {
            Dispatcher.Invoke(() =>
                _notifyIcon?.ShowBalloonTip(2000, "Pic", "Recording started...",
                    System.Windows.Forms.ToolTipIcon.Info));
        };
        _recorder.RecordingStopped += (s, path) =>
        {
            Dispatcher.Invoke(() =>
                _notifyIcon?.ShowBalloonTip(2000, "Pic", $"Recording saved: {Path.GetFileName(path)}",
                    System.Windows.Forms.ToolTipIcon.Info));
        };
        _recorder.Start();
    }

    private void UpdateHotkeyRegistration()
    {
        _hotkeyManager?.UnregisterAll();
        RegisterHotkeys();
    }

    private void ExitApplication()
    {
        _hotkeyManager?.Dispose();
        _notifyIcon?.Dispose();
        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
