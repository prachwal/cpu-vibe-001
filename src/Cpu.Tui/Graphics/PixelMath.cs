namespace Cpu.Tui.Graphics;

/// <summary>
/// Pixel math utilities for color blending and error computation.
/// </summary>
public static class PixelMath
{
    public static Pixel Lerp(Pixel a, Pixel b, float t)
    {
        byte r = (byte)(a.R + (b.R - a.R) * t);
        byte g = (byte)(a.G + (b.G - a.G) * t);
        byte b2 = (byte)(a.B + (b.B - a.B) * t);
        return new Pixel(r, g, b2);
    }

    public static Pixel WeightedAverage(Pixel[] pixels, byte[] weights, int count)
    {
        int rSum = 0, gSum = 0, bSum = 0, wSum = 0;
        for (int i = 0; i < count; i++)
        {
            int w = weights[i];
            rSum += pixels[i].R * w;
            gSum += pixels[i].G * w;
            bSum += pixels[i].B * w;
            wSum += w;
        }

        if (wSum == 0) return Pixel.Black;
        return new Pixel(
            (byte)(rSum / wSum),
            (byte)(gSum / wSum),
            (byte)(bSum / wSum));
    }

    public static int DistanceSquared(Pixel a, Pixel b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }

    public static int ComputeTileError(Pixel[] tile, byte[] alpha, Pixel fg, Pixel bg)
    {
        int error = 0;
        for (int i = 0; i < tile.Length; i++)
        {
            float t = alpha[i] / 255f;
            Pixel expected = Lerp(bg, fg, t);
            error += DistanceSquared(tile[i], expected);
        }
        return error;
    }

    /// <summary>
    /// Compute foreground candidate: average of pixels with high alpha.
    /// </summary>
    public static Pixel EstimateForeground(Pixel[] tile, byte[] alpha, int count)
    {
        int rSum = 0, gSum = 0, bSum = 0, wSum = 0;
        for (int i = 0; i < count; i++)
        {
            if (alpha[i] > 128)
            {
                int w = alpha[i];
                rSum += tile[i].R * w;
                gSum += tile[i].G * w;
                bSum += tile[i].B * w;
                wSum += w;
            }
        }
        if (wSum == 0) return Pixel.White;
        return new Pixel((byte)(rSum / wSum), (byte)(gSum / wSum), (byte)(bSum / wSum));
    }

    /// <summary>
    /// Compute background candidate: average of pixels with low alpha.
    /// </summary>
    public static Pixel EstimateBackground(Pixel[] tile, byte[] alpha, int count)
    {
        int rSum = 0, gSum = 0, bSum = 0, wSum = 0;
        for (int i = 0; i < count; i++)
        {
            if (alpha[i] <= 128)
            {
                int w = 255 - alpha[i];
                rSum += tile[i].R * w;
                gSum += tile[i].G * w;
                bSum += tile[i].B * w;
                wSum += w;
            }
        }
        if (wSum == 0) return Pixel.Black;
        return new Pixel((byte)(rSum / wSum), (byte)(gSum / wSum), (byte)(bSum / wSum));
    }
}
