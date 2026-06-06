using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui;

public partial class App
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    private readonly PiaDevice _pia;
    private readonly PiaTerminalAdapter _piaAdapter;
    private readonly ITerminalRenderer _renderer;
    private bool _running;
    private bool _showHelp;
    private string _statusText = "Ready";
    private TerminalLayout _lastLayout;
    private bool _dirty = true;
    private bool _fullRedraw = true;

    private bool _echoMode;
    private bool _demoMenu;
    private bool _demoRunning;
    private int _demoIndex;
    private string[] _demoBuffer = [];
    private int _demoLine;
    private int _demoChar;

    private bool _imageMode;
    private int _imageIndex;
    private string[] _imagePaths = [];
    private PixelBuffer? _loadedImage;
    private string? _loadedImagePath;

    private bool _canvasMode;
    private readonly TermViewManager _views;
    private readonly CanvasView _canvasView;
    private readonly ScreenView _screenView;
    private readonly DemoMenuView _demoMenuView;
    private readonly HelpView _helpView;

    private TerminalGraphicsMode _imageRenderMode = TerminalGraphicsMode.HalfBlockColor;
    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;
    private FrameStyle _frameStyle = FrameStyle.Ascii;

    public App(
        ITerminalRenderer renderer,
        ScreenBuffer screen,
        EchoTerminal echo,
        PiaDevice pia,
        PiaTerminalAdapter piaAdapter)
    {
        _renderer = renderer;
        _screen = screen;
        _echo = echo;
        _pia = pia;
        _piaAdapter = piaAdapter;
        _views = new TermViewManager(renderer);
        _screenView = new ScreenView(screen, echo, _frameStyle);
        _canvasView = new CanvasView();
        _demoMenuView = new DemoMenuView();
        _helpView = new HelpView();
        SeedScreen();
    }

    public void Run()
    {
        Console.TreatControlCAsInput = true;
        _running = true;

        try
        {
            while (_running)
            {
                var layout = TerminalLayout.From(Console.WindowWidth, Console.WindowHeight);
                bool layoutChanged = layout != _lastLayout;
                if (layoutChanged)
                    _renderer.Resize(layout.Width, layout.Height);

                if (_dirty || layoutChanged)
                {
                    Render(layout, layoutChanged || _fullRedraw);
                    _lastLayout = layout;
                    _dirty = false;
                    _fullRedraw = false;
                }

                ReadInput();

                if (_demoRunning) TickDemo();

                if (_echoMode && _echo.TickBlink())
                    _dirty = true;

                if (_canvasMode && _canvasView.DemoIndex == 2)
                {
                    _canvasView.Tick3D();
                    _dirty = true;
                }

                Thread.Sleep(20);
            }
        }
        finally
        {
            _renderer.Dispose();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }
}
