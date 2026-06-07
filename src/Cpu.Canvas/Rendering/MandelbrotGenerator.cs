namespace Cpu.Canvas.Rendering;

using Cpu.Tui.Graphics;

public static class MandelbrotGenerator
{
    private const int MaxIterations = 96;
    private const double MinRe = -2.5;
    private const double MaxRe = 0.5;
    private const double MinIm = -1.0;
    private const double MaxIm = 1.0;

    public static void Render(PixelCanvas canvas)
    {
        int width = canvas.Width;
        int height = canvas.Height;
        canvas.Clear(Pixel.Black);

        for (int y = 0; y < height; y++)
        {
            double ci = YToIm(y, height);
            for (int x = 0; x < width; x++)
            {
                double cr = XToRe(x, width);
                canvas.SetPixel(x, y, Colorize(Iterate(cr, ci)));
            }
        }
    }

    private static double XToRe(int x, int width) =>
        MinRe + (MaxRe - MinRe) * x / Math.Max(1, width - 1);

    private static double YToIm(int y, int height) =>
        MinIm + (MaxIm - MinIm) * y / Math.Max(1, height - 1);

    private static int Iterate(double cr, double ci)
    {
        double zr = 0;
        double zi = 0;
        for (int i = 0; i < MaxIterations; i++)
        {
            double zr2 = zr * zr;
            double zi2 = zi * zi;
            if (zr2 + zi2 > 4.0)
                return i;
            zi = 2.0 * zr * zi + ci;
            zr = zr2 - zi2 + cr;
        }
        return MaxIterations;
    }

    private static Pixel Colorize(int iter)
    {
        if (iter >= MaxIterations)
            return Pixel.Black;
        double t = iter / (double)MaxIterations;
        byte r = (byte)(255 * Math.Sin(t * Math.PI));
        byte g = (byte)(255 * Math.Sin(t * Math.PI * 2.0 + 1.0));
        byte b = (byte)(128 + 127 * Math.Cos(t * Math.PI * 3.0));
        return new Pixel(r, g, b);
    }
}
