using System.Text;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.Core;
using WinUIGraphics.ViewModels;
using Windows.Globalization;
using Windows.Foundation;

namespace WinUIGraphics.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();


    }

    private readonly StringBuilder _text = new();
    private string? _selectedText = null;
    private int _lastSpace = 0;
    private int _cursorIndex = 0;
    private Rect _textRect = new Rect(5, 5, 0, 0);
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        canvas.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
    }
    private void canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        if (_text.Length == 0)
        {
            args.DrawingSession.DrawLine(new System.Numerics.Vector2((float)0 + 5, (float)24 + 5), new System.Numerics.Vector2((float)0 + 20, (float)24 + 5), Colors.Gray, 3);
        }
        else
        {
            var layout = new CanvasTextLayout(sender.Device, _text.ToString(), new CanvasTextFormat()
            {
                LineSpacingMode = CanvasLineSpacingMode.Proportional,
                Options = CanvasDrawTextOptions.Default,
                FontSize = 24,
                TrimmingGranularity = CanvasTextTrimmingGranularity.None,
            }, (float)canvas.ActualWidth, (float)canvas.ActualHeight);

            var metrics = layout.ClusterMetrics;

            if (_isTextSelected)
            {
                var startingX = _selectionStartingPoint!.Value.X;

                var widthToIgnore = 0f;
                var charIndex = 0;

                while (widthToIgnore < startingX)
                {
                    widthToIgnore += metrics[charIndex].Width;
                    charIndex++;
                }

                var endingX = _selectionEndingPoint!.Value.X;
                var lastCharIndex = charIndex;
                var endingWidthToIgnore = widthToIgnore;
                var rectWidth = 0f;
                while (endingWidthToIgnore < endingX)
                {
                    endingWidthToIgnore += metrics[lastCharIndex].Width;
                    rectWidth += metrics[lastCharIndex].Width;
                    lastCharIndex++;
                }

                if (charIndex < lastCharIndex)
                {
                    _selectedText = _text.ToString().Substring(charIndex - 1, lastCharIndex - charIndex - 1);
                    if (_selectedText.Length != 0)
                    {
                        var selectionRect = new Rect(widthToIgnore - 5, 5, rectWidth + 1, 29);
                        args.DrawingSession.FillRectangle(selectionRect, Colors.Blue);
                    }
                }    
                
                

            }

            _textRect = layout.LayoutBoundsIncludingTrailingWhitespace;

            var lineLayout = new CanvasTextLayout(sender.Device, _text.ToString().Split('\n').Last()[0.._cursorIndex], new CanvasTextFormat()
            {
                FontSize = 24
            }, (float)canvas.ActualWidth, (float)canvas.ActualHeight);

            var lastChar = lineLayout.LayoutBoundsIncludingTrailingWhitespace.Width;
            var height = layout.DrawBounds.Height + 5 + 1;
            args.DrawingSession.DrawLine(new System.Numerics.Vector2((float)lastChar + 5, (float)height + 5), new System.Numerics.Vector2((float)lastChar + 20, (float)height + 5), Colors.Gray, 3);
        }

        args.DrawingSession.DrawText(_text.ToString(), new System.Numerics.Vector2(5, 5), Colors.Yellow, new CanvasTextFormat()
        {
            FontSize = 24
        });



    }

    private void canvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {

    }


    private bool _ctrl = false;
    private bool _addChar = false;
    private void canvas_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        _addChar = false;
        if (_ctrl && e.Key == Windows.System.VirtualKey.Back)
        {
            var words = _text.ToString().Split(' ');
            var addition = 0;
            var lastWord = words[words.Length - 1];
            if (string.IsNullOrEmpty(lastWord))
            {
                addition = 1;
                lastWord = words[words.Length - 2];
            }
            _text.Remove(_cursorIndex - lastWord.Length, lastWord.Length);
            _cursorIndex -= lastWord.Length + addition;
        }
        else if (e.Key == Windows.System.VirtualKey.Back)
        {
            _lastSpace--;
            if (_lastSpace < 0)
                _lastSpace = 0;
            _text.Remove(_cursorIndex - 1, 1);
            _cursorIndex--;
        }
        else if (e.Key == Windows.System.VirtualKey.Control)
        {
            _ctrl = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _text.Append("\n");
            _cursorIndex = 0;
        }
        else if (e.Key == Windows.System.VirtualKey.Space)
        {
            _text.Insert(_cursorIndex, " ");
            _lastSpace++;
            _cursorIndex++;
        }
        else if (e.Key == Windows.System.VirtualKey.Left)
        {

            _cursorIndex--;
        }
        else if (e.Key == Windows.System.VirtualKey.Right)
        {

            _cursorIndex++;
        }
        else
        {
            _addChar = true;
        }
        canvas.Invalidate();
    }

    private void canvas_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Control)
            _ctrl = false;
    }

    private void canvas_CharacterReceived(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs args)
    {
        var chars = args.Character.ToString();
        if (_addChar)
        { 
            _text.Insert(_cursorIndex, chars);
            _cursorIndex++;
        }
    }

    private Point? _selectionStartingPoint = null;
    private Point? _selectionEndingPoint = null;
    private bool _isSelectionMode = false;
    private bool _isTextSelected = false;

    private void canvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(canvas);
        var isLeftPressed = point.Properties.IsLeftButtonPressed;

        if (isLeftPressed && IsPointWithinRect(point))
        {
            _isSelectionMode = true;
            _selectionStartingPoint = point.Position;
        }
    }

    private bool IsPointWithinRect(PointerPoint point)
    {
        return (point.Position.X > _textRect.X + 5 && point.Position.X < _textRect.Width + 5)
                    && (point.Position.Y > _textRect.Y + 5 && point.Position.Y < _textRect.Height + 5);
    }

    private void canvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isSelectionMode && IsPointWithinRect(e.GetCurrentPoint(canvas)))
        {
            _isSelectionMode = false;
            _selectionEndingPoint = e.GetCurrentPoint(canvas).Position;
            _isTextSelected = true;
        }
        canvas.Invalidate();
    }

    private void canvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isSelectionMode && IsPointWithinRect(e.GetCurrentPoint(canvas)))
        {
            _selectionEndingPoint = e.GetCurrentPoint(canvas).Position;
            _isTextSelected = true;
        }
        canvas.Invalidate();
    }
}
