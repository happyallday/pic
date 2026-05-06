using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Pic.Editor;

public partial class ImageEditorWindow : Window
{
    private BitmapSource? _originalImage;
    private readonly Dictionary<string, System.Windows.Controls.Button> _toolButtons = new();
    private Popup? _colorPopup;
    private bool _popupOpen;

    public ImageEditorWindow(BitmapSource? image = null)
    {
        InitializeComponent();
        _originalImage = image;

        _toolButtons["Pointer"] = BtnPointer;
        _toolButtons["Pen"] = BtnPen;
        _toolButtons["Highlighter"] = BtnHighlighter;
        _toolButtons["Rectangle"] = BtnRect;
        _toolButtons["Ellipse"] = BtnEllipse;
        _toolButtons["Arrow"] = BtnArrow;
        _toolButtons["Text"] = BtnText;

        if (image != null)
        {
            EditorCanvas.BackgroundImage = image;
            EditorCanvas.Width = image.Width;
            EditorCanvas.Height = image.Height;
        }

        HighlightToolButton("Pen");
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                if (_popupOpen) { _colorPopup!.IsOpen = false; _popupOpen = false; return; }
                EditorCanvas.SetTool(DrawingTool.Pointer);
                HighlightToolButton("Pointer");
            }
        };
    }

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string toolName) return;

        var tool = toolName switch
        {
            "Pointer" => DrawingTool.Pointer,
            "Pen" => DrawingTool.Pen,
            "Highlighter" => DrawingTool.Highlighter,
            "Rectangle" => DrawingTool.Rectangle,
            "Ellipse" => DrawingTool.Ellipse,
            "Arrow" => DrawingTool.Arrow,
            "Text" => DrawingTool.Text,
            _ => DrawingTool.Pen
        };

        EditorCanvas.SetTool(tool);
        HighlightToolButton(toolName);
    }

    private void HighlightToolButton(string toolName)
    {
        foreach (var kvp in _toolButtons)
        {
            kvp.Value.Background = kvp.Key == toolName
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33));
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_colorPopup == null)
        {
            _colorPopup = new Popup
            {
                PlacementTarget = BtnColor,
                Placement = PlacementMode.Bottom,
                StaysOpen = false
            };

            var panel = new StackPanel
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x30)),
                Width = 200
            };

            var colors = new (string Name, Color Color)[]
            {
                ("Red", Colors.Red), ("Orange", Colors.Orange), ("Yellow", Colors.Yellow),
                ("Green", Colors.LimeGreen), ("Blue", Colors.DodgerBlue), ("Purple", Colors.Purple),
                ("White", Colors.White), ("Black", Colors.Black), ("Gray", Colors.Gray),
            };

            var grid = new UniformGrid { Rows = 2, Columns = 5 };
            foreach (var (name, color) in colors)
            {
                var btn = new System.Windows.Controls.Button
                {
                    Width = 32, Height = 32, Margin = new Thickness(2),
                    Background = new SolidColorBrush(color),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.DarkGray),
                    ToolTip = name
                };
                btn.Click += (s2, e2) =>
                {
                    EditorCanvas.SetColor(color);
                    ColorPreview.Fill = new SolidColorBrush(color);
                    _colorPopup.IsOpen = false;
                    _popupOpen = false;
                };
                grid.Children.Add(btn);
            }
            panel.Children.Add(grid);
            _colorPopup.Child = panel;
            _colorPopup.Closed += (s3, e3) => _popupOpen = false;
        }

        _colorPopup.IsOpen = !_colorPopup.IsOpen;
        _popupOpen = _colorPopup.IsOpen;
    }

    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EditorCanvas != null)
            EditorCanvas.SetThickness(e.NewValue);
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e) => EditorCanvas.Undo();
    private void RedoButton_Click(object sender, RoutedEventArgs e) => EditorCanvas.Redo();

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Screenshot",
            Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap|*.bmp",
            DefaultExt = "png",
            FileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };

        if (dialog.ShowDialog() == true)
        {
            var bitmap = EditorCanvas.RenderToBitmap();
            var encoder = dialog.FileName.EndsWith(".jpg") || dialog.FileName.EndsWith(".jpeg")
                ? (BitmapEncoder)new JpegBitmapEncoder { QualityLevel = 90 }
                : new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = dialog.OpenFile();
            encoder.Save(stream);

            MessageBox.Show($"Saved to: {dialog.FileName}", "Pic", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var bitmap = EditorCanvas.RenderToBitmap();
        Clipboard.SetImage(bitmap);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void EditorCanvas_StateChanged(object? sender, EventArgs e) { }
}
