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

    /// <summary>
    /// Bilinear interpolation resize. Better quality for photos.
    /// Maps each destination pixel center to source space and interpolates 4 neighbors.
    /// </summary>
    public PixelBuffer ResizeBilinear(int width, int height)
    {
        PixelBuffer resized = new(width, height);

        for (int y = 0; y < height; y++)
        {
            float srcY = (y + 0.5f) * Height / height - 0.5f;
            srcY = Math.Clamp(srcY, 0, Height - 1);
            int iy = (int)srcY;
            int iy1 = Math.Min(iy + 1, Height - 1);
            float fy = srcY - iy;

            for (int x = 0; x < width; x++)
            {
                float srcX = (x + 0.5f) * Width / width - 0.5f;
                srcX = Math.Clamp(srcX, 0, Width - 1);
                int ix = (int)srcX;
                int ix1 = Math.Min(ix + 1, Width - 1);
                float fx = srcX - ix;

                Pixel p00 = GetPixel(ix, iy);
                Pixel p01 = GetPixel(ix1, iy);
                Pixel p10 = GetPixel(ix, iy1);
                Pixel p11 = GetPixel(ix1, iy1);

                byte r = BilinearChannel(p00.R, p01.R, p10.R, p11.R, fx, fy);
                byte g = BilinearChannel(p00.G, p01.G, p10.G, p11.G, fx, fy);
                byte b = BilinearChannel(p00.B, p01.B, p10.B, p11.B, fx, fy);

                resized.SetPixel(x, y, new Pixel(r, g, b));
            }
        }

        return resized;
    }

    private static byte BilinearChannel(byte c00, byte c01, byte c10, byte c11, float fx, float fy)
    {
        float top = c00 + (c01 - c00) * fx;
        float bottom = c10 + (c11 - c10) * fx;
        return (byte)(top + (bottom - top) * fy);
    }

    public PixelBuffer Letterbox(int width, int height)
    {
        if (Width == width && Height == height)
            return this;

        PixelBuffer padded = new(width, height);
        int offsetX = Math.Max(0, (width - Width) / 2);
        int offsetY = Math.Max(0, (height - Height) / 2);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                padded.SetPixel(offsetX + x, offsetY + y, GetPixel(x, y));
        return padded;
    }
}
