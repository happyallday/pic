using System.Drawing;
using System.Drawing.Imaging;
using Pic.Shared;

namespace Pic.Capture;

public class RegionSelector
{
    private Rectangle[] _screens = Array.Empty<Rectangle>();
    private Rectangle _selectedRegion;

    public Rectangle? SelectRegion()
    {
        var screens = Screen.AllScreens;
        _screens = new Rectangle[screens.Length];
        for (var i = 0; i < screens.Length; i++)
            _screens[i] = screens[i].Bounds;

        _selectedRegion = Rectangle.Empty;

        using var overlayForm = new RegionOverlayForm(_screens);
        overlayForm.RegionSelected += (s, region) =>
        {
            _selectedRegion = region;
        };

        overlayForm.ShowDialog();

        if (_selectedRegion.Width > 0 && _selectedRegion.Height > 0)
            return _selectedRegion;

        return null;
    }

    public Bitmap? SelectAndCapture()
    {
        var screens = Screen.AllScreens;
        _screens = new Rectangle[screens.Length];
        for (var i = 0; i < screens.Length; i++)
            _screens[i] = screens[i].Bounds;

        _selectedRegion = Rectangle.Empty;

        using var overlayForm = new RegionOverlayForm(_screens);
        overlayForm.RegionSelected += (s, region) =>
        {
            _selectedRegion = region;
        };

        overlayForm.ShowDialog();

        if (_selectedRegion.Width > 0 && _selectedRegion.Height > 0)
        {
            var capture = new ScreenCapture();
            return capture.CaptureRectangle(_selectedRegion);
        }

        return null;
    }
}

internal class RegionOverlayForm : Form
{
    private Rectangle[] _screens;
    private Point _startPoint;
    private Point _endPoint;
    private bool _isSelecting;
    private Rectangle _currentSelection;

    public event EventHandler<Rectangle>? RegionSelected;
    public event EventHandler<Point>? RegionSelecting;

    public RegionOverlayForm(Rectangle[] screens)
    {
        _screens = screens;

        var totalBounds = screens[0];
        for (var i = 1; i < screens.Length; i++)
            totalBounds = Rectangle.Union(totalBounds, screens[i]);

        Bounds = totalBounds;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.Manual;
        Location = totalBounds.Location;
        TopMost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;
        BackColor = Color.Fuchsia;
        AllowTransparency = true;
        TransparencyKey = Color.Fuchsia;

        DoubleBuffered = true;
        KeyDown += OverlayForm_KeyDown;
        MouseDown += OverlayForm_MouseDown;
        MouseMove += OverlayForm_MouseMove;
        MouseUp += OverlayForm_MouseUp;
        Paint += OverlayForm_Paint;
    }

    private void OverlayForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void OverlayForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _startPoint = e.Location;
            _isSelecting = true;
            RegionSelecting?.Invoke(this, e.Location);
        }
    }

    private void OverlayForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            _endPoint = e.Location;
            _currentSelection = GetSelectionRect(_startPoint, _endPoint);
            Invalidate();
        }
    }

    private void OverlayForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_isSelecting && e.Button == MouseButtons.Left)
        {
            _isSelecting = false;
            _endPoint = e.Location;
            _currentSelection = GetSelectionRect(_startPoint, _endPoint);

            var screenRect = GetSelectionRect(_startPoint, _endPoint);
            screenRect.Offset(Location);

            if (screenRect.Width > 5 && screenRect.Height > 5)
            {
                RegionSelected?.Invoke(this, screenRect);
                Close();
            }
        }
    }

    private void OverlayForm_Paint(object? sender, PaintEventArgs e)
    {
        // Draw the dimming overlay
        using var dimBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
        e.Graphics.FillRectangle(dimBrush, ClientRectangle);

        if (_currentSelection.Width > 0 && _currentSelection.Height > 0)
        {
            // Clear the selection area to show the original content
            using var clearBrush = new SolidBrush(Color.FromArgb(140, 255, 255, 255));
            var region = new Region(new Rectangle(0, 0, Width, Height));
            region.Exclude(_currentSelection);
            e.Graphics.FillRegion(clearBrush, region);

            using var borderPen = new Pen(Color.DodgerBlue, 2);
            e.Graphics.DrawRectangle(borderPen, _currentSelection);

            var sizeText = $"{_currentSelection.Width} x {_currentSelection.Height}";
            using var font = new Font("Segoe UI", 9f);
            var textSize = e.Graphics.MeasureString(sizeText, font);
            var infoX = _currentSelection.Right + 5;
            var infoY = _currentSelection.Bottom + 2;

            if (infoX + textSize.Width > Width)
                infoX = _currentSelection.Left;
            if (infoY + textSize.Height > Height)
                infoY = (int)(_currentSelection.Top - textSize.Height - 2);

            using var bgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
            var textRect = new Rectangle((int)infoX - 2, (int)infoY - 2, (int)textSize.Width + 4, (int)textSize.Height + 4);
            e.Graphics.FillRectangle(bgBrush, textRect);
            using var textBrush = new SolidBrush(Color.White);
            e.Graphics.DrawString(sizeText, font, textBrush, infoX, infoY);
        }
    }

    private static Rectangle GetSelectionRect(Point p1, Point p2)
    {
        return new Rectangle(
            Math.Min(p1.X, p2.X),
            Math.Min(p1.Y, p2.Y),
            Math.Abs(p2.X - p1.X),
            Math.Abs(p2.Y - p1.Y)
        );
    }
}
