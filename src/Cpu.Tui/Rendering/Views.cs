using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;

namespace Cpu.Tui.Rendering.Views;

public class ScreenView : ITermView
{
    private readonly ScreenBuffer _screen;
    private readonly EchoTerminal _echo;
    private readonly FrameStyle _frameStyle;
    public string Name => "Screen";
    public EchoTerminal Echo => _echo;

    public ScreenView(ScreenBuffer screen, EchoTerminal echo, FrameStyle frameStyle)
    { _screen = screen; _echo = echo; _frameStyle = frameStyle; }

    public void Activate(ITerminalRenderer r, TermRect area) { }
    public void Deactivate(ITerminalRenderer r, TermRect area) { }

    public void Render(ITerminalRenderer r, TermRect area)
    {
        var frame = area.CenterFrame(80, 25);
        TermFrame.Draw(r, frame, _frameStyle);
        TermArea.Screen(r, frame.Inner, _screen, 25, 80);
        if (_echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, 79), cy = Math.Min(_echo.CursorY, 24);
            char cur = _screen.GetChar(cx, cy);
            r.SetCell(frame.Inner.X + cx, frame.Inner.Y + cy,
                cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }
}

public class CanvasView : ITermView
{
    private readonly PixelCanvas _canvas = new();
    private readonly Demo3D _demo = new();
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    private int _demoIndex;
    private bool _seeded;

    public string Name => "Canvas";
    public TerminalGraphicsMode Mode { get => _mode; set { _mode = value; _seeded = false; } }
    public int DemoIndex => _demoIndex;
    public PixelCanvas Canvas => _canvas;
    public Demo3D Demo => _demo;

    public CanvasView() { }

    public void Activate(ITerminalRenderer r, TermRect area)
    { if (!_seeded) { Seed(); _seeded = true; } RenderContent(r, area); }

    public void Deactivate(ITerminalRenderer r, TermRect area) => TermArea.Clear(r, area);

    public void Render(ITerminalRenderer r, TermRect area) => RenderContent(r, area);

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
        _seeded = false;
    }

    public void NextDemo() { _demoIndex = (_demoIndex + 1) % 3; _seeded = false; }
    public void PrevDemo() { _demoIndex = (_demoIndex + 2) % 3; _seeded = false; }
    public void Tick3D() { if (_demoIndex == 2) _demo.Tick(_canvas); }

    private void RenderContent(ITerminalRenderer r, TermRect area)
    {
        int mc = Math.Max(1, area.W - 4), mr = Math.Max(1, area.H - 4);
        var (cols, rows) = FitImage(_canvas.Buffer, mc, mr, _mode);
        var frame = area.CenterFrame(cols, rows);
        TermArea.Clear(r, area);
        TermFrame.Draw(r, frame, FrameStyle.Ascii, $"{Names[_demoIndex]} {_mode}");
        TerminalGraphicsRenderer.Render(r, _canvas.Buffer, _mode, frame.Inner.X, frame.Inner.Y, cols, rows);
    }

    private void Seed()
    {
        _canvas.Clear(Pixel.Black);
        if (_demoIndex == 0) SeedGradient();
        else if (_demoIndex == 1) SeedSpectrum();
        else { _demo.BuildCube(); _demo.Tick(_canvas); }
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
