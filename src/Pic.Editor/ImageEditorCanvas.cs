using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Ink;
using System.Windows.Media.Imaging;

namespace Pic.Editor;

public enum DrawingTool
{
    Pointer,
    Pen,
    Highlighter,
    Rectangle,
    Ellipse,
    Arrow,
    Text,
    Eraser
}

public class EditorState
{
    public DrawingTool CurrentTool { get; set; } = DrawingTool.Pen;
    public System.Windows.Media.Color StrokeColor { get; set; } = Colors.Red;
    public double StrokeThickness { get; set; } = 2;
    public System.Windows.Media.Color FillColor { get; set; } = Colors.Transparent;
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 16;
    public System.Windows.Media.Color TextColor { get; set; } = Colors.Red;
}

public partial class ImageEditorCanvas : Canvas
{
    private readonly EditorState _state = new();
    private System.Windows.Point _startPoint;
    private bool _isDrawing;
    private UIElement? _currentElement;
    private readonly Stack<UIElement> _undoStack = new();
    private readonly Stack<UIElement> _redoStack = new();

    private ImageSource? _backgroundImage;

    public ImageSource? BackgroundImage
    {
        get => _backgroundImage;
        set
        {
            _backgroundImage = value;
            if (value != null)
                Background = new ImageBrush(value);
        }
    }

    public event EventHandler? StateChanged;

    public ImageEditorCanvas()
    {
        Background = Brushes.Transparent;
        ClipToBounds = true;
        Cursor = Cursors.Pen;

        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
    }

    public EditorState GetState() => _state;

    public void SetTool(DrawingTool tool)
    {
        _state.CurrentTool = tool;
        Cursor = tool switch
        {
            DrawingTool.Pen => Cursors.Pen,
            DrawingTool.Highlighter => Cursors.Pen,
            DrawingTool.Rectangle => Cursors.Cross,
            DrawingTool.Ellipse => Cursors.Cross,
            DrawingTool.Arrow => Cursors.Cross,
            DrawingTool.Text => Cursors.IBeam,
            DrawingTool.Eraser => Cursors.No,
            _ => Cursors.Arrow
        };
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetColor(System.Windows.Media.Color color)
    {
        _state.StrokeColor = color;
    }

    public void SetThickness(double thickness)
    {
        _state.StrokeThickness = thickness;
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var element = _undoStack.Pop();
            Children.Remove(element);
            _redoStack.Push(element);
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var element = _redoStack.Pop();
            Children.Add(element);
            _undoStack.Push(element);
        }
    }

    public BitmapSource RenderToBitmap()
    {
        var bounds = new Rect(0, 0, ActualWidth > 0 ? ActualWidth : 800, ActualHeight > 0 ? ActualHeight : 600);

        var rtb = new RenderTargetBitmap(
            (int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);

        rtb.Render(this);

        return rtb;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        _isDrawing = true;
        CaptureMouse();

        switch (_state.CurrentTool)
        {
            case DrawingTool.Text:
                AddText(_startPoint);
                _isDrawing = false;
                ReleaseMouseCapture();
                break;
        }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDrawing) return;
        var currentPoint = e.GetPosition(this);

        switch (_state.CurrentTool)
        {
            case DrawingTool.Pen:
            case DrawingTool.Highlighter:
                var line = new Line
                {
                    X1 = _startPoint.X,
                    Y1 = _startPoint.Y,
                    X2 = currentPoint.X,
                    Y2 = currentPoint.Y,
                    Stroke = new SolidColorBrush(_state.CurrentTool == DrawingTool.Highlighter
                        ? System.Windows.Media.Color.FromArgb(80, _state.StrokeColor.R, _state.StrokeColor.G, _state.StrokeColor.B)
                        : _state.StrokeColor),
                    StrokeThickness = _state.CurrentTool == DrawingTool.Highlighter ? 20 : _state.StrokeThickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                Children.Add(line);
                _undoStack.Push(line);
                _startPoint = currentPoint;
                break;

            case DrawingTool.Rectangle:
            case DrawingTool.Ellipse:
            case DrawingTool.Arrow:
                if (_currentElement != null)
                    Children.Remove(_currentElement);

                var rect = new Rect(
                    Math.Min(_startPoint.X, currentPoint.X),
                    Math.Min(_startPoint.Y, currentPoint.Y),
                    Math.Abs(currentPoint.X - _startPoint.X),
                    Math.Abs(currentPoint.Y - _startPoint.Y));

                if (rect.Width > 0 && rect.Height > 0)
                {
                    if (_state.CurrentTool == DrawingTool.Rectangle)
                    {
                        _currentElement = new Rectangle
                        {
                            Width = rect.Width,
                            Height = rect.Height,
                            Stroke = new SolidColorBrush(_state.StrokeColor),
                            StrokeThickness = _state.StrokeThickness,
                            Fill = new SolidColorBrush(_state.FillColor)
                        };
                    }
                    else if (_state.CurrentTool == DrawingTool.Ellipse)
                    {
                        _currentElement = new Ellipse
                        {
                            Width = rect.Width,
                            Height = rect.Height,
                            Stroke = new SolidColorBrush(_state.StrokeColor),
                            StrokeThickness = _state.StrokeThickness,
                            Fill = new SolidColorBrush(_state.FillColor)
                        };
                    }
                    else
                    {
                        _currentElement = CreateArrow(_startPoint, currentPoint);
                    }

                    SetLeft(_currentElement!, rect.Left);
                    SetTop(_currentElement!, rect.Top);
                    Children.Add(_currentElement!);
                }
                break;

            case DrawingTool.Eraser:
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var hitResult = InputHitTest(currentPoint) as UIElement;
                    if (hitResult != null && hitResult != this)
                    {
                        Children.Remove(hitResult);
                        _undoStack.Push(hitResult);
                    }
                }
                break;
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;
        ReleaseMouseCapture();

        if (_currentElement != null)
        {
            _undoStack.Push(_currentElement);
            _currentElement = null;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddText(System.Windows.Point position)
    {
        var textBlock = new TextBlock
        {
            Text = "Text",
            FontFamily = new System.Windows.Media.FontFamily(_state.FontFamily),
            FontSize = _state.FontSize,
            Foreground = new SolidColorBrush(_state.TextColor),
            Background = Brushes.Transparent
        };

        var border = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(_state.StrokeColor),
            Child = textBlock,
            Background = Brushes.Transparent
        };

        SetLeft(border, position.X);
        SetTop(border, position.Y);
        Children.Add(border);
        _undoStack.Push(border);
    }

    private static UIElement CreateArrow(System.Windows.Point start, System.Windows.Point end)
    {
        var geometryGroup = new GeometryGroup();

        var lineGeom = new LineGeometry(start, end);
        geometryGroup.Children.Add(lineGeom);

        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var arrowLength = 12;
        var arrowAngle = Math.PI / 6;

        var p1 = new System.Windows.Point(
            end.X - arrowLength * Math.Cos(angle - arrowAngle),
            end.Y - arrowLength * Math.Sin(angle - arrowAngle));
        var p2 = new System.Windows.Point(
            end.X - arrowLength * Math.Cos(angle + arrowAngle),
            end.Y - arrowLength * Math.Sin(angle + arrowAngle));

        var arrowHead = new PathGeometry();
        var arrowFigure = new PathFigure { StartPoint = end };
        arrowFigure.Segments.Add(new LineSegment(p1, true));
        arrowFigure.Segments.Add(new LineSegment(p2, true));
        arrowFigure.IsClosed = true;
        arrowHead.Figures.Add(arrowFigure);
        geometryGroup.Children.Add(arrowHead);

        return new System.Windows.Shapes.Path
        {
            Data = geometryGroup,
            Stroke = Brushes.Red,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Colors.Red),
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
    }
}
