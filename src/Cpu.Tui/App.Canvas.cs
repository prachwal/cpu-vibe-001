using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private void CycleCanvasRenderMode()
    {
        _imageRenderMode = _imageRenderMode switch
        {
            TerminalGraphicsMode.HalfBlockColor => TerminalGraphicsMode.BrailleMono,
            TerminalGraphicsMode.BrailleMono => TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.Grayscale => TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.BestGlyph => TerminalGraphicsMode.BestGlyphTrueColor,
            _ => TerminalGraphicsMode.HalfBlockColor
        };
        _canvas.Clear(Pixel.Black);
        SeedCanvasDemo(_canvasDemoIndex);
        SetStatus(_imageRenderMode.ToString());
    }

    private void SeedCanvasDemo(int index)
    {
        _canvas.Clear(Pixel.Black);
        switch (index)
        {
            case 0: SeedGradientMandala(); break;
            case 1: SeedSpectrumDemo(); break;
            case 2: _demo3D.BuildCube(); _demo3DTick = 0; _demo3D.Tick(_canvas); break;
        }
    }

    private void SeedGradientMandala()
    {
        var r = Random.Shared;
        for (int y = 0; y < PixelCanvas.CanvasHeight; y++)
        {
            byte val = (byte)(y * 255 / PixelCanvas.CanvasHeight);
            for (int x = 0; x < PixelCanvas.CanvasWidth; x++)
            {
                byte phase = (byte)((x + y) & 0xFF);
                _canvas.SetPixel(x, y, new Pixel(val, phase, (byte)(255 - val)));
            }
        }
        for (int i = 0; i < 20; i++)
        {
            int cx = r.Next(50, 270), cy = r.Next(30, 170), radius = r.Next(10, 60);
            var color = new Pixel((byte)r.Next(200, 256), (byte)r.Next(100, 200), (byte)r.Next(50, 150));
            DrawCircle(cx, cy, radius, color);
        }
    }

    private void SeedSpectrumDemo()
    {
        var spectrum = new AttributeCanvas();
        for (int ax = 0; ax < AttributeCanvas.AttrCols; ax++)
        { spectrum.SetAttr(ax, 0, 7, 1); spectrum.SetAttr(ax, 1, 7, 1); }
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < AttributeCanvas.PixelWidth; x++)
                spectrum.SetPixel(x, y, (byte)((x / 8 + y / 8) & 1));

        int[] inkColors = [2, 6, 4, 5, 3, 1];
        for (int i = 0; i < inkColors.Length; i++)
        {
            int ax = i * 5 + 1;
            spectrum.SetAttr(ax, 3, inkColors[i], 0, true);
            spectrum.SetAttr(ax + 1, 3, inkColors[i], 0, true);
            spectrum.SetAttr(ax, 4, inkColors[i], 0, true);
            spectrum.SetAttr(ax + 1, 4, inkColors[i], 0, true);
            for (int y = 24; y < 40; y++)
                for (int x = ax * 8; x < (ax + 2) * 8; x++)
                    spectrum.SetPixel(x, y, 1);
        }

        for (int ax = 0; ax < AttributeCanvas.AttrCols; ax++)
            for (int ay = 10; ay < 20; ay++)
                spectrum.SetAttr(ax, ay, 7, 0);
        for (int y = 80; y < 84; y++)
            for (int x = 0; x < AttributeCanvas.PixelWidth; x++)
                spectrum.SetPixel(x, y, 1);
        for (int y = 140; y < 144; y++)
            for (int x = 0; x < AttributeCanvas.PixelWidth; x++)
                spectrum.SetPixel(x, y, 1);
        for (int ay = 18; ay < 24; ay++)
            spectrum.SetAttr(0, ay, 5, 0, true);
        for (int y = 144; y < AttributeCanvas.PixelHeight; y++)
            for (int x = 0; x < AttributeCanvas.PixelWidth; x += 16)
                for (int w = 0; w < 8; w++)
                    spectrum.SetPixel(x + w, y, 1);

        var temp = new PixelBuffer(AttributeCanvas.PixelWidth, AttributeCanvas.PixelHeight);
        spectrum.RenderTo(temp);
        var scaled = temp.ResizeNearest(PixelCanvas.CanvasWidth, PixelCanvas.CanvasHeight);
        for (int y = 0; y < PixelCanvas.CanvasHeight; y++)
            for (int x = 0; x < PixelCanvas.CanvasWidth; x++)
                _canvas.SetPixel(x, y, scaled.GetPixel(x, y));
    }

    private void DrawCircle(int cx, int cy, int r, Pixel color)
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
}
