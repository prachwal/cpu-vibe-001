namespace Cpu.Tui.Graphics;

public sealed class PixelBuffer
{
    private readonly Pixel[] _pixels;

    public int Width { get; }
    public int Height { get; }

    public PixelBuffer(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        _pixels = new Pixel[width * height];
    }

    public Pixel GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            return Pixel.Black;

        return _pixels[y * Width + x];
    }

    public void SetPixel(int x, int y, Pixel pixel)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            return;

        _pixels[y * Width + x] = pixel;
    }

    public PixelBuffer ResizeNearest(int width, int height)
    {
        PixelBuffer resized = new(width, height);
        for (int y = 0; y < height; y++)
        {
            int sourceY = y * Height / height;
            for (int x = 0; x < width; x++)
            {
                int sourceX = x * Width / width;
                resized.SetPixel(x, y, GetPixel(sourceX, sourceY));
            }
        }

        return resized;
    }
}
