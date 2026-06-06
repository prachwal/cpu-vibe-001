using FluentAssertions;
using Xunit;
using Cpu.Tui.Graphics;

namespace Cpu.Tui.Tests;

public class PixelCanvasTests
{
    [Fact]
    public void Constructor_Creates320x200()
    {
        var c = new PixelCanvas();
        c.Width.Should().Be(320);
        c.Height.Should().Be(200);
    }

    [Fact]
    public void SetPixel_WritesPixel()
    {
        var c = new PixelCanvas();
        c.SetPixel(10, 20, Pixel.Red);
        c.GetPixel(10, 20).Should().Be(Pixel.Red);
    }

    [Fact]
    public void SetPixel_OutOfBounds_Ignored()
    {
        var c = new PixelCanvas();
        var action = () => c.SetPixel(999, 999, Pixel.White);
        action.Should().NotThrow();
    }

    [Fact]
    public void Clear_FillsAllBlack()
    {
        var c = new PixelCanvas();
        c.SetPixel(100, 100, Pixel.White);
        c.Clear(Pixel.Black);
        c.GetPixel(100, 100).Should().Be(Pixel.Black);
        c.GetPixel(0, 0).Should().Be(Pixel.Black);
    }

    [Fact]
    public void FillRect_FillsRectangle()
    {
        var c = new PixelCanvas();
        c.FillRect(50, 50, 100, 80, Pixel.Blue);
        c.GetPixel(50, 50).Should().Be(Pixel.Blue);
        c.GetPixel(149, 129).Should().Be(Pixel.Blue);
        c.GetPixel(49, 50).Should().Be(Pixel.Black); // outside
        c.GetPixel(50, 49).Should().Be(Pixel.Black); // outside
    }

    [Fact]
    public void DrawLine_Horizontal()
    {
        var c = new PixelCanvas();
        c.DrawLine(10, 20, 50, 20, Pixel.Green);
        c.GetPixel(10, 20).Should().Be(Pixel.Green);
        c.GetPixel(30, 20).Should().Be(Pixel.Green);
        c.GetPixel(50, 20).Should().Be(Pixel.Green);
    }

    [Fact]
    public void DrawLine_Vertical()
    {
        var c = new PixelCanvas();
        c.DrawLine(30, 10, 30, 50, Pixel.Green);
        c.GetPixel(30, 10).Should().Be(Pixel.Green);
        c.GetPixel(30, 30).Should().Be(Pixel.Green);
        c.GetPixel(30, 50).Should().Be(Pixel.Green);
    }

    [Fact]
    public void DrawRect_DrawsOutline()
    {
        var c = new PixelCanvas();
        c.DrawRect(100, 100, 50, 40, Pixel.Red);
        c.GetPixel(100, 100).Should().Be(Pixel.Red);
        c.GetPixel(149, 100).Should().Be(Pixel.Red);
        c.GetPixel(100, 139).Should().Be(Pixel.Red);
        c.GetPixel(149, 139).Should().Be(Pixel.Red);
        c.GetPixel(125, 120).Should().Be(Pixel.Black); // inside should be empty
    }

    [Fact]
    public void Buffer_Property_ReturnsSameBuffer()
    {
        var c = new PixelCanvas();
        c.Buffer.Width.Should().Be(320);
        c.Buffer.Height.Should().Be(200);
    }
}
