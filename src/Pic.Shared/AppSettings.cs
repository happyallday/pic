namespace Pic.Shared;

public class AppSettings
{
    public string SaveDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Pic");
    public ImageFormat DefaultImageFormat { get; set; } = ImageFormat.Png;
    public int JpegQuality { get; set; } = 90;
    public int DelaySeconds { get; set; } = 3;
    public bool AutoCopyToClipboard { get; set; } = true;
    public bool OpenEditorAfterCapture { get; set; } = true;
    public bool EditorTopmost { get; set; } = false;
    public bool RunOnStartup { get; set; } = false;
    public bool ShowToolbarOnStartup { get; set; } = true;
    public Dictionary<CaptureMode, string> Hotkeys { get; set; } = new()
    {
        { CaptureMode.FullScreen, "PrintScreen" },
        { CaptureMode.ActiveWindow, "Alt+PrintScreen" },
        { CaptureMode.Region, "Ctrl+Shift+X" },
        { CaptureMode.Scrolling, "Ctrl+Shift+S" },
        { CaptureMode.Delayed, "Ctrl+Shift+D" },
        { CaptureMode.ScreenRecording, "Ctrl+Shift+R" }
    };
    public string RecordingsDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Pic");
    public int RecordingFrameRate { get; set; } = 30;
    public int RecordingQuality { get; set; } = 80;

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pic", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir != null)
                Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
