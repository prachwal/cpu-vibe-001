namespace Cpu.Tui.Graphics;

/// <summary>
/// Pseudo-canvas 320×200 with simple drawing primitives.
/// Designed to emulate C64 VIC-II resolution for terminal display.
/// </summary>
public sealed class PixelCanvas
{
    public const int CanvasWidth = 320;
    public const int CanvasHeight = 200;

    private readonly PixelBuffer _buffer;

    public int Width => _buffer.Width;
    public int Height => _buffer.Height;
    public PixelBuffer Buffer => _buffer;

    public PixelCanvas()
    {
        _buffer = new PixelBuffer(CanvasWidth, CanvasHeight);
        Clear(Pixel.Black);
    }

    public void SetPixel(int x, int y, Pixel color)
    {
        if ((uint)x < CanvasWidth && (uint)y < CanvasHeight)
            _buffer.SetPixel(x, y, color);
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        if ((uint)x < CanvasWidth && (uint)y < CanvasHeight)
            _buffer.SetPixel(x, y, new Pixel(r, g, b));
    }

    public Pixel GetPixel(int x, int y)
    {
        if ((uint)x < CanvasWidth && (uint)y < CanvasHeight)
            return _buffer.GetPixel(x, y);
        return Pixel.Black;
    }

    public void Clear(Pixel color)
    {
        for (int y = 0; y < CanvasHeight; y++)
            for (int x = 0; x < CanvasWidth; x++)
                _buffer.SetPixel(x, y, color);
    }

    public void FillRect(int x, int y, int w, int h, Pixel color)
    {
        int x1 = Math.Clamp(x, 0, CanvasWidth);
        int y1 = Math.Clamp(y, 0, CanvasHeight);
        int x2 = Math.Clamp(x + w, 0, CanvasWidth);
        int y2 = Math.Clamp(y + h, 0, CanvasHeight);

        for (int ry = y1; ry < y2; ry++)
            for (int rx = x1; rx < x2; rx++)
                _buffer.SetPixel(rx, ry, color);
    }

    public void DrawLine(int x0, int y0, int x1, int y1, Pixel color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void DrawRect(int x, int y, int w, int h, Pixel color)
    {
        DrawLine(x, y, x + w - 1, y, color);
        DrawLine(x + w - 1, y, x + w - 1, y + h - 1, color);
        DrawLine(x + w - 1, y + h - 1, x, y + h - 1, color);
        DrawLine(x, y + h - 1, x, y, color);
    }
}
