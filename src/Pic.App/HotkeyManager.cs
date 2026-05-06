using System.Runtime.InteropServices;
using Pic.Shared;

namespace Pic.App;

public class HotkeyManager : IDisposable
{
    private readonly Dictionary<int, (CaptureMode Mode, Action Handler)> _hotkeys = new();
    private int _currentId;
    private readonly IntPtr _hwnd;
    private readonly HotkeyWindow _hotkeyWindow;
    private bool _disposed;

    public HotkeyManager()
    {
        _hotkeyWindow = new HotkeyWindow(this);
        _hwnd = _hotkeyWindow.Handle;
    }

    public void RegisterHotkey(CaptureMode mode, Hotkey hotkey, Action handler)
    {
        var modifiers = (uint)hotkey.Modifiers;
        var key = (uint)hotkey.Key;
        var id = Interlocked.Increment(ref _currentId);

        if (RegisterHotKey(_hwnd, id, modifiers, key))
        {
            _hotkeys[id] = (mode, handler);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Keys)
        {
            UnregisterHotKey(_hwnd, id);
        }
        _hotkeys.Clear();
    }

    internal void HandleHotkeyMessage(int id)
    {
        if (_hotkeys.TryGetValue(id, out var entry))
        {
            entry.Handler?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
        _hotkeyWindow.Destroy();
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public record struct Hotkey(uint Key, KeyModifiers Modifiers);

    public static Hotkey ParseHotkeyString(string keyStr)
    {
        var parts = keyStr.Split('+').Select(p => p.Trim()).ToArray();
        var modifiers = KeyModifiers.None;
        var key = 0u;

        foreach (var part in parts)
        {
            switch (part.ToLower())
            {
                case "ctrl": modifiers |= KeyModifiers.Control; break;
                case "alt": modifiers |= KeyModifiers.Alt; break;
                case "shift": modifiers |= KeyModifiers.Shift; break;
                case "win": modifiers |= KeyModifiers.Windows; break;
                case "printscreen": key = 0x2C; break;
                case "x": key = (uint)System.Windows.Forms.Keys.X; break;
                case "s": key = (uint)System.Windows.Forms.Keys.S; break;
                case "d": key = (uint)System.Windows.Forms.Keys.D; break;
                case "r": key = (uint)System.Windows.Forms.Keys.R; break;
                default: key = (uint)part[0]; break;
            }
        }

        return new Hotkey(key, modifiers);
    }

    private class HotkeyWindow : NativeWindow
    {
        private readonly HotkeyManager _manager;
        private const int WM_HOTKEY = 0x0312;

        public HotkeyWindow(HotkeyManager manager)
        {
            _manager = manager;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                _manager.HandleHotkeyMessage(m.WParam.ToInt32());
            }
            base.WndProc(ref m);
        }

        public void Destroy()
        {
            DestroyHandle();
        }
    }
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
    NoRepeat = 0x4000
}
