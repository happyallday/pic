using System.Drawing;

namespace Pic.Capture;

public class CountdownOverlay
{
    public bool Show(int seconds)
    {
        var screens = Screen.AllScreens;
        var primaryBounds = screens[0].Bounds;

        for (var i = 1; i <= seconds; i++)
        {
            using var form = new Form
            {
                Bounds = primaryBounds,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = primaryBounds.Location,
                TopMost = true,
                ShowInTaskbar = false,
                BackColor = Color.Fuchsia,
                AllowTransparency = true,
                TransparencyKey = Color.Fuchsia
            };

            var remaining = seconds - i + 1;
            form.Paint += (s, e) =>
            {
                var centerX = form.Width / 2;
                var centerY = form.Height / 2;

                // Dim background
                using var dimBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
                e.Graphics.FillRectangle(dimBrush, form.ClientRectangle);

                // Draw countdown number
                using var font = new Font("Segoe UI", 96, FontStyle.Bold);
                var text = remaining.ToString();
                var size = e.Graphics.MeasureString(text, font);
                var x = centerX - size.Width / 2;
                var y = centerY - size.Height / 2;

                // Shadow
                using var shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
                e.Graphics.DrawString(text, font, shadowBrush, x + 4, y + 4);

                // Main text
                using var textBrush = new SolidBrush(Color.White);
                e.Graphics.DrawString(text, font, textBrush, x, y);

                // "Taking screenshot..." subtitle
                if (remaining == 1)
                {
                    using var subFont = new Font("Segoe UI", 16);
                    var subText = "Taking screenshot...";
                    var subSize = e.Graphics.MeasureString(subText, subFont);
                    e.Graphics.DrawString(subText, subFont, textBrush,
                        centerX - subSize.Width / 2, y + size.Height + 20);
                }
            };

            form.Show();
            form.Refresh();
            System.Threading.Thread.Sleep(1000);
        }

        return true;
    }
}
