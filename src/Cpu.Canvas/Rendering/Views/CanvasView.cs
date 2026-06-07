using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Graphics;

using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Canvas.Rendering.Views;

public class CanvasView : BaseTermView, ITuiSettingsConsumer
{
    private readonly PixelCanvas _canvas = new();
    private readonly Demo3D _demo = new();
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    private int _demoIndex;
    private bool _needInvalidate;
    private bool _needFullClear;
    private bool _hasLastFrame;
    private TermRect _lastFrame;
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

    public void ApplySettings(TuiAppSettings settings)
    {
        if (_frameStyle != settings.FrameStyle)
        {
            _frameStyle = settings.FrameStyle;
            _needInvalidate = true;
        }
    }

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
        _demo.Tick(_canvas, _mode);
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
        _demo.Tick(_canvas, _mode);
    }

    protected override void Seed()
    {
        _canvas.Clear(Pixel.Black);
        if (_demoIndex == 0) SeedGradient();
        else if (_demoIndex == 1) SeedSpectrum();
        else { _demo.BuildCube(); _demo.Tick(_canvas, _mode); }
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
    }

    public override void Deactivate(ITerminalRenderer r, TermRect area)
    {
        RenderLog.Event("CanvasView.Deactivate", $"area={area}");
        TermArea.Clear(r, area, TerminalCell.Black, "CanvasView.Deactivate");
    }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        if (!_seeded) { Seed(); _seeded = true; }

        int mc = Math.Max(1, area.W - 4), mr = Math.Max(1, area.H - 4);
        var (cols, rows) = PresentationSession.FitImage(_canvas.Buffer, mc, mr, _mode);
        var frame = session.CenterFrame(cols, rows);

        if (_needFullClear)
        {
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
            if (r is AnsiTerminalRenderer atr)
                atr.InvalidateArea(frame.X, frame.Y, frame.W, frame.H);
            _needInvalidate = false;
        }

        TermArea.Clear(r, frame.Inner, TerminalCell.Black, "CanvasView.Render");
        session.DrawFrame(frame, _frameStyle, $"{Names[_demoIndex]} {_mode}");
        session.RenderCanvas(_canvas.Buffer, _mode, frame.Inner);

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
        if (!_hasLastFrame) return;
        if (_lastFrame == newFrame) return;
        int x1 = Math.Min(_lastFrame.X, newFrame.X);
        int y1 = Math.Min(_lastFrame.Y, newFrame.Y);
        int x2 = Math.Max(_lastFrame.X2, newFrame.X2);
        int y2 = Math.Max(_lastFrame.Y2, newFrame.Y2);
        for (int y = y1; y < y2; y++)
            for (int x = x1; x < x2; x++)
            {
                bool inOld = x >= _lastFrame.X && x < _lastFrame.X2 && y >= _lastFrame.Y && y < _lastFrame.Y2;
                bool inNew = x >= newFrame.X && x < newFrame.X2 && y >= newFrame.Y && y < newFrame.Y2;
                if (inOld && !inNew)
                    r.SetCell(x, y, TerminalCell.Black.Ch, TerminalCell.Black.Fg, TerminalCell.Black.Bg);
            }
    }

    private static readonly string[] Names = ["Gradient Mandala", "ZX Spectrum", "3D Shapes"];

    private void SeedGradient()
    {
        var rng = Random.Shared;
        for (int y = 0; y < 200; y++)
        {
            byte v = (byte)(y * 255 / 200);
            for (int x = 0; x < 320; x++)
                _canvas.SetPixel(x, y, new Pixel(v, (byte)((x + y) & 0xFF), (byte)(255 - v)));
        }
        for (int i = 0; i < 20; i++)
        {
            int cx = rng.Next(50, 270), cy = rng.Next(30, 170), rad = rng.Next(10, 60);
            var c = new Pixel((byte)rng.Next(200, 256), (byte)rng.Next(100, 200), (byte)rng.Next(50, 150));
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
}
