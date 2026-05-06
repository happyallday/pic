using System.Drawing;

namespace Pic.Capture;

public class WindowPicker
{
    public IntPtr PickWindow()
    {
        var screens = Screen.AllScreens;
        var screensRect = new Rectangle[screens.Length];
        for (var i = 0; i < screens.Length; i++)
            screensRect[i] = screens[i].Bounds;

        var totalBounds = screensRect[0];
        for (var i = 1; i < screensRect.Length; i++)
            totalBounds = Rectangle.Union(totalBounds, screensRect[i]);

        using var form = new WindowPickForm(totalBounds);
        form.ShowDialog();
        return form.SelectedWindow;
    }

    private class WindowPickForm : Form
    {
        public IntPtr SelectedWindow { get; private set; }

        public WindowPickForm(Rectangle totalBounds)
        {
            Bounds = totalBounds;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.Manual;
            Location = totalBounds.Location;
            TopMost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            BackColor = Color.FromArgb(1, 0, 0, 0);
            AllowTransparency = true;
            TransparencyKey = BackColor;
            DoubleBuffered = true;

            Paint += (s, e) =>
            {
                using var font = new Font("Segoe UI", 14, FontStyle.Bold);
                var text = "Click on the window to scroll-capture...";
                var size = e.Graphics.MeasureString(text, font);
                var x = (Width - size.Width) / 2;
                var y = (Height - size.Height) / 2 - 40;

                using var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
                var textRect = new Rectangle((int)x - 10, (int)y - 5, (int)size.Width + 20, (int)size.Height + 10);
                e.Graphics.FillRectangle(bgBrush, textRect);

                using var textBrush = new SolidBrush(Color.White);
                e.Graphics.DrawString(text, font, textBrush, x, y);
            };

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var screenPt = new NativeMethods.POINT { X = totalBounds.Left + e.X, Y = totalBounds.Top + e.Y };
                    var hWnd = NativeMethods.WindowFromPoint(screenPt);

                    var realExStyle = (ulong)NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                    var parent = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
                    var root = GetAncestor(hWnd, 2);

                    if (root != IntPtr.Zero && root != hWnd && NativeMethods.IsWindowVisible(root))
                    {
                        SelectedWindow = root;
                    }
                    else if (parent != IntPtr.Zero && NativeMethods.IsWindowVisible(parent))
                    {
                        SelectedWindow = parent;
                    }
                    else
                    {
                        SelectedWindow = hWnd;
                    }

                    Close();
                }
            };

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Close();
            };
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
}
