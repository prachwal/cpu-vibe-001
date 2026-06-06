using FluentAssertions;
using Xunit;
using Cpu.Tui.Graphics;

namespace Cpu.Tui.Tests;

public class AttributeCanvasTests
{
    [Fact]
    public void Constructor_HasCorrectDimensions()
    {
        var c = new AttributeCanvas();
        c.Width.Should().Be(256);
        c.Height.Should().Be(192);
    }

    [Fact]
    public void SetPixel_StoresBit()
    {
        var c = new AttributeCanvas();
        c.SetPixel(10, 20, 1);
        c.GetPixel(10, 20).Should().Be(1);
        c.SetPixel(10, 21, 0);
        c.GetPixel(10, 21).Should().Be(0);
    }

    [Fact]
    public void SetAttr_StoresInkAndPaper()
    {
        var c = new AttributeCanvas();
        c.SetAttr(5, 3, 2, 6); // INK=Red(2), PAPER=Yellow(6)
        c.RenderTo(new PixelBuffer(256, 192));
        // Ink=2→Red(215,0,0), Paper=6→Yellow(215,215,0), not bright
    }

    [Fact]
    public void FillBlock_Fills8x8()
    {
        var c = new AttributeCanvas();
        c.SetAttr(0, 0, 7, 0); // INK=White on PAPER=Black
        c.FillBlock(0, 0, 1);  // all INK
        var buf = new PixelBuffer(256, 192);
        c.RenderTo(buf);
        buf.GetPixel(0, 0).Should().Be(new Pixel(215, 215, 215)); // White
        buf.GetPixel(7, 7).Should().Be(new Pixel(215, 215, 215)); // White
    }

    [Fact]
    public void Attribute_Boundary_UsesCorrectColors()
    {
        var c = new AttributeCanvas();
        c.SetAttr(0, 0, 1, 2, false); // INK=Blue, PAPER=Red
        c.SetAttr(1, 0, 7, 0, true); // INK=Bright White, PAPER=Black

        // Fill first half of block 0 with INK
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 4; x++)
                c.SetPixel(x, y, 1); // INK in left half
        // First pixel of block 1 set to INK
        c.SetPixel(8, 0, 1);

        var buf = new PixelBuffer(256, 192);
        c.RenderTo(buf);

        buf.GetPixel(0, 0).Should().Be(new Pixel(0, 0, 215));    // Blue (INK of block 0)
        buf.GetPixel(7, 0).Should().Be(new Pixel(215, 0, 0));    // Red (PAPER of block 0)
        buf.GetPixel(8, 0).Should().Be(new Pixel(255, 255, 255)); // Bright White (INK of block 1, bright)
    }

    [Fact]
    public void SpectrumPalette_StandardColors()
    {
        SpectrumPalette.GetColor(0).Should().Be(new Pixel(0, 0, 0));
        SpectrumPalette.GetColor(7).Should().Be(new Pixel(215, 215, 215));
    }

    [Fact]
    public void SpectrumPalette_BrightColors()
    {
        var brightBlue = SpectrumPalette.GetColor(9);  // index 9 = color(1) | bright(8)
        brightBlue.Should().Be(new Pixel(0, 0, 255));
    }
}
