# Pic - Windows Screenshot Tool

A lightweight, feature-rich screenshot tool for Windows, inspired by FastStone Capture.
Built with C# + WPF on .NET 8.

## Features

| Feature | Description |
|---------|-------------|
| **Region Capture** | Drag to select a rectangular region with crosshair overlay, real-time dimension display |
| **Full Screen Capture** | Capture all monitors at once |
| **Active Window Capture** | Auto-capture the foreground window, restores minimized windows |
| **Scrolling Capture** | Click-to-select target window, auto-scroll via `WM_MOUSEWHEEL`/`WM_VSCROLL`, stitch into single image |
| **Delayed Capture** | Full-screen countdown overlay (3..2..1), then region selector appears |
| **Image Editor** | Pen, highlighter, rectangle, ellipse, arrow, text overlay, undo/redo, color picker |
| **Screen Recording** | Select a region, record to MP4 (H.264), configurable frame rate — requires FFmpeg |
| **Floating Toolbar** | Always-on-top compact toolbar (260x48px), draggable, toggled from system tray |
| **Global Hotkeys** | All modes have configurable hotkeys, registered via Win32 `RegisterHotKey` |
| **System Tray** | Minimizes to notification area, right-click context menu for all actions |
| **Clipboard** | Auto-copy captures to clipboard |
| **Multi-format Save** | Save as PNG, JPEG (adjustable quality 10-100), BMP |

## Tech Stack

- **Language:** C# 12
- **Framework:** .NET 8 (WPF + Windows Forms interop)
- **UI:** WPF for editor/toolbar/settings, WinForms for overlay forms and tray
- **Capture:** GDI32 via source-generated P/Invoke (`BitBlt`)
- **Recording:** FFmpeg for MP4/H.264 encoding (frame capture via GDI)
- **Platform:** Windows 10 / 11 (x64)

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                     Pic.App (WPF)                   │
│  App entry, Tray icon, Floating toolbar, Settings    │
│  Hotkey manager, Capture orchestration               │
└──────┬──────────┬──────────┬──────────┬─────────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
  Pic.Capture  Pic.Editor  Pic.Recorder  Pic.Shared
  ───────────  ──────────  ────────────  ──────────
  Screenshots  Annotation   ScreenRec    Config
  Window enum  Undo/Redo    FFmpeg enc   Enums
  Region sel   Drawing tools  MP4 output  Hotkey defs
  Scroll stit  Arrow/Text    Frame capt   Settings
  Countdown    RelayCommand  FFmpeg check
  WindowPicker ColorPicker
  GDI P/Invoke WPF Canvas
```

## Project Structure

```
pic/
├── Pic.sln
├── Directory.Build.props
├── .gitignore
├── README.md
├── src/
│   ├── Pic.App/                   # Main WPF application
│   │   ├── App.xaml/.cs           # Entry point, tray, capture orchestration
│   │   ├── FloatingToolbar.xaml/.cs # Always-on-top capture toolbar
│   │   ├── SettingsWindow.xaml/.cs  # Settings with hotkey configuration
│   │   ├── HotkeyManager.cs       # Global keyboard hook (RegisterHotKey)
│   │   └── Resources/app.ico      # Application icon
│   ├── Pic.Capture/               # Screen capture engine
│   │   ├── ScreenCapture.cs       # Core capture (full, window, region)
│   │   ├── RegionSelector.cs      # Overlay region selection + size display
│   │   ├── ScrollingCapture.cs    # Auto-scroll, stitch, duplicate detect
│   │   ├── WindowPicker.cs        # Click-to-select target window
│   │   ├── CountdownOverlay.cs    # Full-screen countdown animation
│   │   └── NativeMethods.cs       # GDI32/User32 P/Invoke declarations
│   ├── Pic.Editor/                # WPF image annotation editor
│   │   ├── ImageEditorWindow.xaml/.cs # Editor window with toolbar
│   │   ├── ImageEditorCanvas.cs   # Drawing canvas (pen, shapes, arrow, text)
│   │   └── RelayCommand.cs        # ICommand implementation for shortcuts
│   ├── Pic.Recorder/              # Screen video recorder
│   │   └── ScreenRecorder.cs      # Frame capture + FFmpeg MP4 encoding
│   └── Pic.Shared/                # Shared models
│       ├── AppSettings.cs         # JSON-based settings persistence
│       ├── CaptureMode.cs         # Capture mode enum
│       └── ImageFormat.cs         # Output format enum
└── tests/
    └── Pic.Tests/                 # MSTest unit test project
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11
- [FFmpeg](https://ffmpeg.org/download.html) (optional, required for screen recording)

### Build & Run

```bash
git clone https://github.com/happyallday/pic.git
cd pic
dotnet build
dotnet run --project src/Pic.App
```

Or open `Pic.sln` in Visual Studio 2022+ / JetBrains Rider and press F5.

### Publish (single-file executable)

```bash
dotnet publish src/Pic.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/
```

## Default Hotkeys

| Action | Hotkey |
|--------|--------|
| Full Screen | `PrintScreen` |
| Active Window | `Alt+PrintScreen` |
| Region | `Ctrl+Shift+X` |
| Scrolling | `Ctrl+Shift+S` |
| Delayed Capture | `Ctrl+Shift+D` |
| Screen Recording | `Ctrl+Shift+R` |

Hotkeys are fully configurable in Settings → Hotkeys section. Click "Record" next to any action and press the desired key combination.

## Editor Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save to file |
| `Ctrl+C` | Copy to clipboard |
| `Esc` | Switch to pointer / close |

## Configuration

Settings are stored at `%LocalAppData%\Pic\settings.json`:

```json
{
  "SaveDirectory": "C:\\Users\\...\\Pictures\\Pic",
  "DefaultImageFormat": "Png",
  "JpegQuality": 90,
  "DelaySeconds": 3,
  "AutoCopyToClipboard": true,
  "OpenEditorAfterCapture": true,
  "EditorTopmost": false,
  "RunOnStartup": false,
  "ShowToolbarOnStartup": true,
  "RecordingsDirectory": "C:\\Users\\...\\Videos\\Pic",
  "RecordingFrameRate": 30,
  "RecordingQuality": 80,
  "Hotkeys": {
    "FullScreen": "PrintScreen",
    "ActiveWindow": "Alt+PrintScreen",
    "Region": "Ctrl+Shift+X",
    "Scrolling": "Ctrl+Shift+S",
    "Delayed": "Ctrl+Shift+D",
    "ScreenRecording": "Ctrl+Shift+R"
  }
}
```

## Development Milestones

All 8 milestones complete:

| # | Milestone | Status |
|---|-----------|--------|
| M1 | Project skeleton, tray icon, single instance, config | ✓ |
| M2 | Full screen, active window, region capture | ✓ |
| M3 | Image editor with annotation tools, undo/redo, keyboard shortcuts | ✓ |
| M4 | Scrolling capture, delayed countdown, global hotkeys | ✓ |
| M5 | Floating always-on-top toolbar | ✓ |
| M6 | Screen recording with FFmpeg MP4 encoding, FFmpeg detection | ✓ |
| M7 | Settings window, hotkey configuration, JPEG quality, editor topmost | ✓ |
| M8 | README, application icon, documentation | ✓ |

## Feature Changelog

### v1.0 Initial Release
- All core capture modes: full screen, active window, region, scrolling, delayed
- Image editor with pen, highlighter, shapes, arrow, text tools
- Screen recording via FFmpeg
- Floating toolbar, system tray, global hotkeys
- Customizable settings with hotkey reconfiguration
- JPEG quality slider, configurable delay seconds, recording frame rate
- Editor always-on-top toggle
- Window picker for scrolling capture with title feedback
- Countdown overlay for delayed capture
- Multi-monitor support
- Auto-copy to clipboard, multi-format save (PNG/JPEG/BMP)
- Application icon

## License

MIT
