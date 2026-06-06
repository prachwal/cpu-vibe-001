using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;

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
    private PixelBuffer? _loadedImage;
    private string? _loadedImagePath;
    private static readonly string[] _imagePaths = [];

    private bool _canvasMode;
    private readonly PixelCanvas _canvas;
    private int _canvasDemoIndex;
    private readonly Demo3D _demo3D;
    private int _demo3DTick;
    private static readonly string[] _canvasDemos = ["Gradient Mandala", "ZX Spectrum", "3D Shapes"];

    private TerminalGraphicsMode _imageRenderMode = TerminalGraphicsMode.HalfBlockColor;
    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;
    private FrameStyle _frameStyle = FrameStyle.Ascii;

    public App(
        ITerminalRenderer renderer,
        ScreenBuffer screen,
        EchoTerminal echo,
        PiaDevice pia,
        PiaTerminalAdapter piaAdapter,
        PixelCanvas canvas,
        Demo3D demo3D)
    {
        _renderer = renderer;
        _screen = screen;
        _echo = echo;
        _pia = pia;
        _piaAdapter = piaAdapter;
        _canvas = canvas;
        _demo3D = demo3D;
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

                if (_canvasMode && _canvasDemoIndex == 2)
                {
                    _demo3DTick++;
                    if (_demo3DTick % 3 == 0)
                    {
                        _demo3D.Tick(_canvas);
                        _dirty = true;
                    }
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
