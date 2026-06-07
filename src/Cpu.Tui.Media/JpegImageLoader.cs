using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cpu.Tui.Graphics;

public static partial class JpegImageLoader
{
    public static bool IsFfmpegAvailable()
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process? process = Process.Start(psi);
            if (process == null)
                return false;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static PixelBuffer Load(string path, int targetWidth = 0, int targetHeight = 0)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Image not found: {path}", path);

        if (!IsFfmpegAvailable())
            throw new InvalidOperationException(
                "ffmpeg is not available on PATH. Install ffmpeg to load JPG images in the Image module.");

        (int sourceWidth, int sourceHeight) = ProbeSize(path);
        int width;
        int height;
        if (targetWidth > 0 && targetHeight > 0)
        {
            double scale = Math.Min((double)targetWidth / sourceWidth, (double)targetHeight / sourceHeight);
            width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
            height = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        }
        else
        {
            width = sourceWidth;
            height = sourceHeight;
        }

        byte[] rgb = DecodeRgb(path, width, height);
        PixelBuffer buffer = new(width, height);

        int i = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer.SetPixel(x, y, new Pixel(rgb[i], rgb[i + 1], rgb[i + 2]));
                i += 3;
            }
        }

        return buffer;
    }

    public static (int Width, int Height) ProbeSize(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Image not found: {path}", path);

        if (!IsFfmpegAvailable())
            throw new InvalidOperationException(
                "ffmpeg is not available on PATH. Install ffmpeg to load JPG images in the Image module.");

        ProcessStartInfo psi = new()
        {
            FileName = "ffmpeg",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(path);

        using Process process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.");
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Match match = VideoSizeRegex().Match(stderr);
        if (!match.Success)
            throw new InvalidOperationException($"Could not probe image size with ffmpeg: {path}");

        return (
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static byte[] DecodeRgb(string path, int width, int height)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "ffmpeg",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-loglevel");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(path);
        psi.ArgumentList.Add("-vf");
        psi.ArgumentList.Add($"scale={width}:{height}:flags=bilinear");
        psi.ArgumentList.Add("-frames:v");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("rawvideo");
        psi.ArgumentList.Add("-pix_fmt");
        psi.ArgumentList.Add("rgb24");
        psi.ArgumentList.Add("-");

        using Process process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start ffmpeg.");
        using MemoryStream output = new();
        process.StandardOutput.BaseStream.CopyTo(output);
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg failed decoding image: {stderr}");

        byte[] rgb = output.ToArray();
        int expected = width * height * 3;
        if (rgb.Length != expected)
            throw new InvalidOperationException(
                $"Decoded RGB size mismatch: got {rgb.Length}, expected {expected} for {width}x{height}.");

        return rgb;
    }

    [GeneratedRegex(@"(?:Video|Stream).*?(\d+)x(\d+)", RegexOptions.Compiled)]
    private static partial Regex VideoSizeRegex();
}
