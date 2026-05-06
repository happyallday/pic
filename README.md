# Pic - Windows Screenshot Tool

A lightweight, feature-rich screenshot tool for Windows, inspired by FastStone Capture.
Built with C# + WPF on .NET 8.

## Features

| Feature | Description |
|---------|-------------|
| **Region Capture** | Select a rectangular region to capture with a crosshair overlay, showing real-time dimensions |
| **Full Screen Capture** | Capture all monitors at once |
| **Active Window Capture** | Automatically captures the foreground window |
| **Scrolling Capture** | Auto-scroll and stitch long pages/documents into a single image |
| **Delayed Capture** | Configurable countdown timer before capture |
| **Image Editor** | Built-in annotation tools: pen, highlighter, rectangle, ellipse, arrow, text overlay |
| **Screen Recording** | Record a screen region to MP4 (requires FFmpeg in PATH) |
| **Floating Toolbar** | Always-on-top compact toolbar for quick capture actions |
| **Global Hotkeys** | Configurable keyboard shortcuts for all capture modes |
| **System Tray** | Minimizes to tray with right-click context menu for all actions |
| **Clipboard Support** | Auto-copy captures to clipboard |
| **Multi-format Save** | Save as PNG, JPEG, BMP |

## Tech Stack

- **Language:** C# 12
- **Framework:** .NET 8 (WPF + Windows Forms interop)
- **UI:** WPF for editor and toolbar, WinForms for region overlay and tray
- **Capture:** GDI32 via P/Invoke (`BitBlt`)
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
  Scroll stit  Arrow/Text
  GDI P/Invoke WPF Canvas
```

## Project Structure

```
pic/
├── Pic.sln
├── Directory.Build.props
├── README.md
├── src/
│   ├── Pic.App/                # Main WPF application
│   │   ├── App.xaml/.cs        # Entry point, tray, capture orchestration
│   │   ├── FloatingToolbar.xaml/.cs  # Always-on-top capture toolbar
│   │   ├── SettingsWindow.xaml/.cs   # Application settings dialog
│   │   └── HotkeyManager.cs    # Global keyboard hook manager
│   ├── Pic.Capture/            # Screen capture engine
│   │   ├── ScreenCapture.cs    # Core capture (full, window, region)
│   │   ├── RegionSelector.cs   # Overlay region selection form
│   │   ├── ScrollingCapture.cs # Auto-scroll & stitch
│   │   └── NativeMethods.cs    # GDI32/User32 P/Invoke
│   ├── Pic.Editor/             # WPF image annotation editor
│   │   ├── ImageEditorWindow.xaml/.cs  # Editor window with toolbar
│   │   └── ImageEditorCanvas.cs       # Drawing canvas with tools
│   ├── Pic.Recorder/           # Screen video recorder
│   │   └── ScreenRecorder.cs   # Frame capture + FFmpeg encoding
│   └── Pic.Shared/             # Shared models and utilities
│       ├── AppSettings.cs      # JSON-based settings persistence
│       ├── CaptureMode.cs      # Capture mode enum
│       └── ImageFormat.cs      # Output format enum
└── tests/
    └── Pic.Tests/              # MSTest unit tests
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11
- [FFmpeg](https://ffmpeg.org/download.html) (optional, required for screen recording)

### Build & Run

```bash
# Clone
git clone https://github.com/happyallday/pic.git
cd pic

# Build
dotnet build

# Run (from build output)
dotnet run --project src/Pic.App
```

Or open `Pic.sln` in Visual Studio 2022+ / JetBrains Rider and press F5.

### Publish (single-file executable)

```bash
dotnet publish src/Pic.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/
```

## Development Milestones

### M1 - Project Skeleton (completed)
- [x] Solution structure with 6 projects
- [x] System tray icon with context menu (capture modes, settings, exit)
- [x] Single-instance enforcement via named Mutex
- [x] JSON-based settings persistence in `%LocalAppData%\Pic\settings.json`

### M2 - Basic Capture (completed)
- [x] Full screen capture via `BitBlt` on virtual screen bounds
- [x] Active window capture with auto-restore of minimized windows
- [x] Region capture with semi-transparent overlay and crosshair cursor
- [x] Real-time dimension display during region selection
- [x] Multi-monitor support

### M3 - Image Editor (completed)
- [x] WPF-based annotation editor window
- [x] Drawing tools: Pen, Highlighter, Rectangle, Ellipse, Arrow, Text
- [x] Undo/Redo via element stack
- [x] Color picker (9 preset colors via popup)
- [x] Adjustable line thickness
- [x] Save to PNG/JPEG/BMP
- [x] Copy to clipboard
- [x] Keyboard shortcuts: Ctrl+Z (undo), Ctrl+Y (redo), Ctrl+S (save), Ctrl+C (copy), Esc (close)

### M4 - Advanced Capture (completed)
- [x] Scrolling capture: auto-scroll via `WM_MOUSEWHEEL` and `PostMessage`, stitch multiple captures
- [x] Delayed capture with configurable countdown
- [x] Global hotkeys using `RegisterHotKey`/`UnregisterHotKey` Win32 API
- [x] Default hotkeys:
  - PrintScreen → Full Screen
  - Alt+PrintScreen → Active Window
  - Ctrl+Shift+X → Region
  - Ctrl+Shift+S → Scrolling
  - Ctrl+Shift+D → Delayed
  - Ctrl+Shift+R → Screen Recording

### M5 - Floating Toolbar (completed)
- [x] Compact always-on-top window (260x48px)
- [x] Quick-access buttons: Region, Full Screen, Active Window, Scrolling, Recording, Settings
- [x] Draggable window (click-drag anywhere on toolbar)
- [x] Centered at bottom of primary screen on first launch
- [x] Toggle visibility from system tray

### M6 - Screen Recording (completed)
- [x] Screen region recording via GDI frame capture
- [x] Frame accumulation to temp directory as PNG frames
- [x] FFmpeg encoding to MP4 (H.264, yuv420p)
- [x] Configurable frame rate (default 30fps)
- [x] Recording start/stop via hotkey or toolbar
- [x] Output saved to `%UserProfile%\Videos\Pic\`

### M7 - Settings & Polish (completed)
- [x] Settings window with WPF UI
- [x] Configurable save directory
- [x] Output format selection (PNG/JPEG/BMP)
- [x] Auto-copy to clipboard toggle
- [x] Auto-open editor toggle
- [x] Show toolbar on startup toggle

### M8 - Release Preparation (current)
- [x] README documentation
- [ ] Application icon
- [ ] MSIX/installer packaging
- [ ] GitHub release

## Default Hotkeys

| Action | Hotkey |
|--------|--------|
| Full Screen | `PrintScreen` |
| Active Window | `Alt+PrintScreen` |
| Region | `Ctrl+Shift+X` |
| Scrolling | `Ctrl+Shift+S` |
| Delayed Capture | `Ctrl+Shift+D` |
| Screen Recording | `Ctrl+Shift+R` |

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
  "RunOnStartup": false,
  "ShowToolbarOnStartup": true,
  "RecordingsDirectory": "C:\\Users\\...\\Videos\\Pic",
  "RecordingFrameRate": 30
}
```

## License

MIT
