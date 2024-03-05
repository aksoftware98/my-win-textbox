using System.Text;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.Core;
using WinUIGraphics.ViewModels;
using Windows.Globalization;

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

    private StringBuilder _text = new StringBuilder();
    private int _lastSpace = 0;
    private int _cursorIndex = 0;

    protected async override void OnNavigatedTo(NavigationEventArgs e)
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
}
