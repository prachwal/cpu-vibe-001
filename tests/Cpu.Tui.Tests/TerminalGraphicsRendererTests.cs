using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class TerminalGraphicsRendererTests
{
    [Fact]
    public void RenderHalfBlock_UsesForegroundAndBackgroundForTwoPixels()
    {
        using MemoryStream stream = new();
        using AnsiTerminalRenderer renderer = new(stream);
        renderer.Resize(2, 1);
        stream.SetLength(0);

        PixelBuffer pixels = new(1, 2);
        pixels.SetPixel(0, 0, new Pixel(255, 0, 0));
        pixels.SetPixel(0, 1, new Pixel(0, 0, 255));

        TerminalGraphicsRenderer.RenderHalfBlock(renderer, pixels, 0, 0, 1, 2);
        renderer.Flush();

        string output = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("▀");
        output.Should().Contain("\x1b[91;104m");
    }

    [Fact]
    public void RenderBrailleMono_White2x4BlockProducesFullBrailleCell()
    {
        using MemoryStream stream = new();
        using AnsiTerminalRenderer renderer = new(stream);
        renderer.Resize(1, 1);
        stream.SetLength(0);

        PixelBuffer pixels = new(2, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 2; x++)
                pixels.SetPixel(x, y, Pixel.White);

        TerminalGraphicsRenderer.RenderBrailleMono(renderer, pixels, 0, 0, 2, 4);
        renderer.Flush();

        string output = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("⣿");
    }

    [Fact]
    public void RenderGrayscale_MapsBlackAndWhiteToShadeRange()
    {
        using MemoryStream stream = new();
        using AnsiTerminalRenderer renderer = new(stream);
        renderer.Resize(2, 1);
        stream.SetLength(0);

        PixelBuffer pixels = new(2, 1);
        pixels.SetPixel(0, 0, Pixel.Black);
        pixels.SetPixel(1, 0, Pixel.White);

        TerminalGraphicsRenderer.RenderGrayscale(renderer, pixels, 0, 0, 2, 1);
        renderer.Flush();

        string output = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("█");
    }

    [Fact]
    public void PixelBuffer_ResizeBilinear_AveragesCorners()
    {
        var pixels = new PixelBuffer(2, 2);
        pixels.SetPixel(0, 0, Pixel.Black);
        pixels.SetPixel(1, 0, Pixel.White);
        pixels.SetPixel(0, 1, Pixel.White);
        pixels.SetPixel(1, 1, Pixel.Black);

        var resized = pixels.ResizeBilinear(1, 1);

        var avg = resized.GetPixel(0, 0);
        avg.R.Should().BeInRange(125, 130); // (0+255+255+0)/4 ≈ 127
        avg.G.Should().BeInRange(125, 130);
        avg.B.Should().BeInRange(125, 130);
    }

    [Fact]
    public void PixelBuffer_ResizeNearest_PreservesCorners()
    {
        PixelBuffer pixels = new(2, 2);
        pixels.SetPixel(0, 0, new Pixel(255, 0, 0));
        pixels.SetPixel(1, 1, new Pixel(0, 0, 255));

        PixelBuffer resized = pixels.ResizeNearest(4, 4);

        resized.GetPixel(0, 0).Should().Be(new Pixel(255, 0, 0));
        resized.GetPixel(3, 3).Should().Be(new Pixel(0, 0, 255));
    }
}
