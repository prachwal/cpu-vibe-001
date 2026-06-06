using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;

namespace Cpu.Tui.Rendering.Views;

public class ScreenView : BaseTermView
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    private readonly FrameStyle _frameStyle;
    private ScreenMode _screenMode = ScreenMode.Rows25Cols80;
    public override string Name => "Screen";
    public EchoTerminal Echo => _echo;
    public ScreenMode ScreenMode { get => _screenMode; set => _screenMode = value; }

    private ScreenSize GetRequestedSize() => _screenMode switch
    {
        ScreenMode.Rows24Cols40 => new ScreenSize(24, 40),
        ScreenMode.Rows25Cols40 => new ScreenSize(25, 40),
        _ => new ScreenSize(25, 80)
    };

    public ScreenView(ScreenBuffer screen, EchoTerminal echo, FrameStyle frameStyle)
    { _screen = screen; _echo = echo; _frameStyle = frameStyle; }

    protected override void Seed() { }

    public override void Render(ITerminalRenderer r, TermRect area)
    {
        var requested = GetRequestedSize();
        int cols = area.W >= 80 && requested.Cols == 80 ? 80 : 40;
        int rows = area.H >= 25 && requested.Rows == 25 ? 25 : 24;
        var frame = area.CenterFrame(cols, rows);
        TermFrame.Draw(r, frame, _frameStyle);
        TermArea.Screen(r, frame.Inner, _screen, rows, cols);
        if (_echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, cols - 1), cy = Math.Min(_echo.CursorY, rows - 1);
            char cur = _screen.GetChar(cx, cy);
            r.SetCell(frame.Inner.X + cx, frame.Inner.Y + cy,
                cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }
}

public class CanvasView : BaseTermView
{
    private readonly PixelCanvas _canvas = new();
    private readonly Demo3D _demo = new();
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    private int _demoIndex;
    private bool _needInvalidate;
    private bool _needFullClear;
    private TermRect _lastFrame;
    private bool _hasLastFrame;
    public override string Name => "Canvas";
    public TerminalGraphicsMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            _needInvalidate = true;
            _needFullClear = true;
            RenderLog.Event("CanvasView.Mode", $"mode={_mode}");
        }
    }
    public int DemoIndex => _demoIndex;
    public PixelCanvas Canvas => _canvas;
    public Demo3D Demo => _demo;

    public CanvasView() { }

    public void RequireFullClear()
    {
        _needInvalidate = true;
        _needFullClear = true;
    }

    public void CycleMode()
    {
        Mode = _mode switch
        {
            TerminalGraphicsMode.HalfBlockColor => TerminalGraphicsMode.BrailleMono,
            TerminalGraphicsMode.BrailleMono => TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.Grayscale => TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.BestGlyph => TerminalGraphicsMode.BestGlyphTrueColor,
            _ => TerminalGraphicsMode.HalfBlockColor
        };
        RenderLog.Event("CanvasView.CycleMode", $"mode={_mode}");
    }

    public void NextDemo() { _demoIndex = (_demoIndex + 1) % 3; ResetContent(); RenderLog.Event("CanvasView.NextDemo", $"demo={Names[_demoIndex]}"); }
    public void PrevDemo() { _demoIndex = (_demoIndex + 2) % 3; ResetContent(); RenderLog.Event("CanvasView.PrevDemo", $"demo={Names[_demoIndex]}"); }
    public void Tick3D()
    {
        if (_demoIndex != 2) return;
        _demo.Tick(_canvas);
        RenderLog.Event("CanvasView.Tick3D", $"frame={_demo.Frame}");
    }
    public void Handle3DKey(ConsoleKey key)
    {
        if (_demoIndex != 2) return;
        switch (key)
        {
            case ConsoleKey.UpArrow: _demo.ZoomIn(); break;
            case ConsoleKey.DownArrow: _demo.ZoomOut(); break;
            case ConsoleKey.W: _demo.RotateUp(); break;
            case ConsoleKey.S: _demo.RotateDown(); break;
            case ConsoleKey.A: _demo.RotateLeft(); break;
            case ConsoleKey.D: _demo.RotateRight(); break;
        }
        _demo.Tick(_canvas);
    }

    protected override void Seed()
    {
        _canvas.Clear(Pixel.Black);
        if (_demoIndex == 0) SeedGradient();
        else if (_demoIndex == 1) SeedSpectrum();
        else { _demo.BuildCube(); _demo.Tick(_canvas); }
    }

    public override void Activate(ITerminalRenderer r, TermRect area)
    {
        RenderLog.Event("CanvasView.Activate", $"area={area}");
        TermArea.Clear(r, area, TerminalCell.Black, "CanvasView.Activate");
        if (r is AnsiTerminalRenderer atr)
            atr.InvalidateArea(area.X, area.Y, area.W, area.H);
        _hasLastFrame = false;
        _needInvalidate = true;
        _needFullClear = false;
        if (!_seeded) { Seed(); _seeded = true; }
        RenderContent(r, area);
    }

    public override void Deactivate(ITerminalRenderer r, TermRect area)
    {
        RenderLog.Event("CanvasView.Deactivate", $"area={area}");
        TermArea.Clear(r, area, TerminalCell.Black, "CanvasView.Deactivate");
    }

    public override void Render(ITerminalRenderer r, TermRect area) => RenderContent(r, area);

    private void RenderContent(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }

        int mc = Math.Max(1, area.W - 4), mr = Math.Max(1, area.H - 4);
        var (cols, rows) = FitImage(_canvas.Buffer, mc, mr, _mode);
        var frame = area.CenterFrame(cols, rows);

        RenderLog.Event("CanvasView.RenderContent",
            $"mode={_mode} demo={Names[_demoIndex]} area={area} cols={cols} rows={rows} frame={frame} inner={frame.Inner}");

        if (_needFullClear)
        {
            RenderLog.Event("CanvasView.FullClear", $"area={area}");
            TermArea.Clear(r, area, TerminalCell.Black, "CanvasView.FullClear");
            _hasLastFrame = false;
            _needFullClear = false;
        }
        else
        {
            ClearOrphaned(r, frame);
        }

        if (_needInvalidate)
        {
            RenderLog.Event("CanvasView.Invalidate", $"frame={frame}");
            if (r is AnsiTerminalRenderer atr)
                atr.InvalidateArea(frame.X, frame.Y, frame.W, frame.H);
            _needInvalidate = false;
        }

        TermArea.Clear(r, frame.Inner, TerminalCell.Black, "CanvasView.RenderContent");
        TermFrame.Draw(r, frame, FrameStyle.Ascii, $"{Names[_demoIndex]} {_mode}");
        TerminalGraphicsRenderer.Render(r, _canvas.Buffer, _mode, frame.Inner.X, frame.Inner.Y, cols, rows);

        _lastFrame = frame;
        _hasLastFrame = true;
    }

    public void ResetContent()
    {
        _seeded = false;
        _needInvalidate = true;
        _needFullClear = true;
        Seed();
        _seeded = true;
    }

    private void ClearOrphaned(ITerminalRenderer r, TermRect newFrame)
    {
        if (!_hasLastFrame)
        {
            RenderLog.Event("ClearOrphaned", $"skipped reason=no-last-frame new={newFrame}");
            return;
        }
        if (_lastFrame == newFrame)
        {
            RenderLog.Event("ClearOrphaned", $"skipped reason=same-frame frame={_lastFrame}");
            return;
        }
        RenderLog.Event("ClearOrphaned", $"old={_lastFrame} new={newFrame}");
        // Clear the union-minus-intersection: cells in old frame but outside new frame
        int x1 = Math.Min(_lastFrame.X, newFrame.X);
        int y1 = Math.Min(_lastFrame.Y, newFrame.Y);
        int x2 = Math.Max(_lastFrame.X2, newFrame.X2);
        int y2 = Math.Max(_lastFrame.Y2, newFrame.Y2);

        for (int y = y1; y < y2; y++)
        {
            for (int x = x1; x < x2; x++)
            {
                bool inOld = x >= _lastFrame.X && x < _lastFrame.X2 && y >= _lastFrame.Y && y < _lastFrame.Y2;
                bool inNew = x >= newFrame.X && x < newFrame.X2 && y >= newFrame.Y && y < newFrame.Y2;
                if (inOld && !inNew)
                    r.SetCell(x, y, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
            }
        }
    }

    private static readonly string[] Names = ["Gradient Mandala", "ZX Spectrum", "3D Shapes"];

    private void SeedGradient()
    {
        var r = Random.Shared;
        for (int y = 0; y < 200; y++)
        {
            byte v = (byte)(y * 255 / 200);
            for (int x = 0; x < 320; x++)
                _canvas.SetPixel(x, y, new Pixel(v, (byte)((x + y) & 0xFF), (byte)(255 - v)));
        }
        for (int i = 0; i < 20; i++)
        {
            int cx = r.Next(50, 270), cy = r.Next(30, 170), rad = r.Next(10, 60);
            var c = new Pixel((byte)r.Next(200, 256), (byte)r.Next(100, 200), (byte)r.Next(50, 150));
            BresenhamCircle(cx, cy, rad, c);
        }
    }

    private void BresenhamCircle(int cx, int cy, int r, Pixel color)
    {
        int x = r, y = 0, err = 0;
        while (x >= y)
        {
            _canvas.SetPixel(cx + x, cy + y, color); _canvas.SetPixel(cx + y, cy + x, color);
            _canvas.SetPixel(cx - y, cy + x, color); _canvas.SetPixel(cx - x, cy + y, color);
            _canvas.SetPixel(cx - x, cy - y, color); _canvas.SetPixel(cx - y, cy - x, color);
            _canvas.SetPixel(cx + y, cy - x, color); _canvas.SetPixel(cx + x, cy - y, color);
            y++; err += 2 * y + 1;
            if (err > 0) { x--; err -= 2 * x + 1; }
        }
    }

    private void SeedSpectrum()
    {
        var sp = new AttributeCanvas();
        for (int ax = 0; ax < 32; ax++) { sp.SetAttr(ax, 0, 7, 1); sp.SetAttr(ax, 1, 7, 1); }
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 256; x++) sp.SetPixel(x, y, (byte)((x / 8 + y / 8) & 1));
        int[] inks = [2, 6, 4, 5, 3, 1];
        for (int i = 0; i < inks.Length; i++)
        {
            int ax = i * 5 + 1;
            sp.SetAttr(ax, 3, inks[i], 0, true); sp.SetAttr(ax + 1, 3, inks[i], 0, true);
            sp.SetAttr(ax, 4, inks[i], 0, true); sp.SetAttr(ax + 1, 4, inks[i], 0, true);
            for (int y = 24; y < 40; y++)
                for (int x = ax * 8; x < (ax + 2) * 8; x++) sp.SetPixel(x, y, 1);
        }
        for (int ax = 0; ax < 32; ax++)
            for (int ay = 10; ay < 20; ay++) sp.SetAttr(ax, ay, 7, 0);
        for (int y = 80; y < 84; y++) for (int x = 0; x < 256; x++) sp.SetPixel(x, y, 1);
        for (int y = 140; y < 144; y++) for (int x = 0; x < 256; x++) sp.SetPixel(x, y, 1);
        for (int ay = 18; ay < 24; ay++) sp.SetAttr(0, ay, 5, 0, true);
        for (int y = 144; y < 192; y++)
            for (int x = 0; x < 256; x += 16)
                for (int w = 0; w < 8; w++) sp.SetPixel(x + w, y, 1);
        var buf = new PixelBuffer(256, 192);
        sp.RenderTo(buf);
        var s = buf.ResizeNearest(320, 200);
        for (int y = 0; y < 200; y++)
            for (int x = 0; x < 320; x++)
                _canvas.SetPixel(x, y, s.GetPixel(x, y));
    }

    private static (int, int) FitImage(PixelBuffer img, int mc, int mr, TerminalGraphicsMode m)
    {
        double pcc = m is TerminalGraphicsMode.BrailleMono or TerminalGraphicsMode.BestGlyph or TerminalGraphicsMode.BestGlyphTrueColor ? 2.0 : 1.0;
        double prc = m switch
        {
            TerminalGraphicsMode.HalfBlockColor => 2.0,
            TerminalGraphicsMode.BrailleMono => 4.0,
            TerminalGraphicsMode.Grayscale => 2.0,
            TerminalGraphicsMode.BestGlyph => 4.0,
            TerminalGraphicsMode.BestGlyphTrueColor => 4.0,
            _ => 1.0
        };
        double sc = Math.Min(mc * pcc / img.Width, mr * prc / img.Height);
        sc = Math.Min(1.0, Math.Max(sc, 0.01));
        return (Math.Clamp((int)Math.Ceiling(img.Width * sc / pcc), 1, mc),
                Math.Clamp((int)Math.Ceiling(img.Height * sc / prc), 1, mr));
    }
}

public class HelpView : BaseTermView
{
    public override string Name => "Help";
    protected override void Seed() { }
    public override void Render(ITerminalRenderer r, TermRect area)
    {
        string[] lines =
        [
            "CPU-VIBE terminal", "",
            "F1  Help  F2  Cycle screen", "F3  Echo mode  F4  Demo menu",
            "F5  Refresh  F6  Frame style", "F7  Image viewer  F9  Canvas",
            "Esc Quit", "",
            "Echo: type text, arrows move cursor.", "Canvas: left/right switch demo, F10 cycle mode"
        ];
        TermArea.Centered(r, area, lines, ConsoleColor.Gray, ConsoleColor.Black);
        if (lines.Length > 0)
        {
            int top = Math.Max(0, (area.H - lines.Length) / 2);
            int left = Math.Max(0, (area.W - Math.Min(lines[0].Length, area.W)) / 2);
            TermArea.Write(r, area, left, top, lines[0], ConsoleColor.Cyan, ConsoleColor.Black);
        }
    }
}

public class DemoMenuView : BaseTermView
{
    private int _selectedIndex;
    public override string Name => "Demo Menu";
    public int SelectedIndex { get => _selectedIndex; set => _selectedIndex = value; }

    public DemoMenuView() { }
    protected override void Seed() { }

    public override void Render(ITerminalRenderer r, TermRect area)
    {
        string[] names = Cpu.Tui.Devices.Pia.PiaDemos.Names;
        int top = Math.Max(1, (area.H - names.Length - 2) / 2);
        TermArea.Write(r, area, Math.Max(0, (area.W - 10) / 2), top, "PIA Demos", ConsoleColor.Cyan, ConsoleColor.Black);
        top += 2;
        for (int i = 0; i < names.Length; i++)
        {
            bool sel = i == _selectedIndex;
            string line = (sel ? " > " : "   ") + names[i];
            TermArea.Write(r, area, Math.Max(0, (area.W - line.Length) / 2), top + i, line,
                sel ? ConsoleColor.Black : ConsoleColor.Gray,
                sel ? ConsoleColor.Gray : ConsoleColor.Black);
        }
        TermArea.Write(r, area, Math.Max(0, (area.W - 16) / 2), top + names.Length + 1,
            "Enter: run  Esc: back", ConsoleColor.DarkGray, ConsoleColor.Black);
    }
}

public class ImageView : BaseTermView
{
    private string[] _paths = [];
    private int _index;
    private PixelBuffer? _loaded;
    private string? _loadedPath;
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    public override string Name => "Image Viewer";
    public string[] Paths { get => _paths; set => _paths = value; }
    public int Index { get => _index; set { _index = value; _loaded = null; _loadedPath = null; } }
    public TerminalGraphicsMode Mode { get => _mode; set => _mode = value; }

    public ImageView() { }
    protected override void Seed() { }

    public void NextImage() { if (_paths.Length > 0) Index = (_index + 1) % _paths.Length; }
    public void PrevImage() { if (_paths.Length > 0) Index = (_index + _paths.Length - 1) % _paths.Length; }

    public void CycleMode()
    {
        _mode = _mode switch
        {
            TerminalGraphicsMode.HalfBlockColor => TerminalGraphicsMode.BrailleMono,
            TerminalGraphicsMode.BrailleMono => TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.Grayscale => TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.BestGlyph => TerminalGraphicsMode.BestGlyphTrueColor,
            _ => TerminalGraphicsMode.HalfBlockColor
        };
    }

    public override void Render(ITerminalRenderer r, TermRect area)
    {
        if (_paths.Length == 0)
        {
            TermArea.Write(r, area, 2, 2, "No JPG files found in samples/", ConsoleColor.Yellow, ConsoleColor.Black);
            return;
        }
        try
        {
            string path = _paths[_index];
            if (_loaded == null || _loadedPath != path)
            {
                _loaded = JpegImageLoader.Load(path);
                _loadedPath = path;
            }
            if (_loaded == null) return;
            int mc = Math.Max(1, area.W - 4), mr = Math.Max(1, area.H - 4);
            double pcc = _mode is TerminalGraphicsMode.BrailleMono or TerminalGraphicsMode.BestGlyph or TerminalGraphicsMode.BestGlyphTrueColor ? 2.0 : 1.0;
            double prc = _mode switch
            {
                TerminalGraphicsMode.HalfBlockColor => 2.0,
                TerminalGraphicsMode.BrailleMono => 4.0,
                TerminalGraphicsMode.Grayscale => 2.0,
                TerminalGraphicsMode.BestGlyph => 4.0,
                TerminalGraphicsMode.BestGlyphTrueColor => 4.0,
                _ => 1.0
            };
            double sc = Math.Min(mc * pcc / _loaded.Width, mr * prc / _loaded.Height);
            sc = Math.Min(1.0, Math.Max(sc, 0.01));
            int cols = Math.Max(1, (int)(_loaded.Width * sc / pcc));
            int rows = Math.Max(1, (int)(_loaded.Height * sc / prc));
            var frame = area.CenterFrame(cols, rows);
            TermArea.Clear(r, area, TerminalCell.Black, "ImageView.Render");
            TermFrame.Draw(r, frame, FrameStyle.Ascii, $"{Path.GetFileName(path)} {_loaded.Width}x{_loaded.Height} {_mode}");
            TerminalGraphicsRenderer.Render(r, _loaded, _mode, frame.Inner.X, frame.Inner.Y, cols, rows);
        }
        catch (Exception ex)
        {
            TermArea.Write(r, area, 2, 2, "Image render failed", ConsoleColor.White, ConsoleColor.DarkRed);
            string msg = ex.Message;
            if (msg.Length > area.W - 4) msg = msg[..(area.W - 4)];
            TermArea.Write(r, area, 2, 4, msg, ConsoleColor.Yellow, ConsoleColor.Black);
        }
    }
}
