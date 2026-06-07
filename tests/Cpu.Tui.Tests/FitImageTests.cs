using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class FitImageTests
{
    [Theory]
    [InlineData(TerminalGraphicsMode.HalfBlockColor, 1.0, 2.0)]
    [InlineData(TerminalGraphicsMode.BrailleMono, 2.0, 4.0)]
    [InlineData(TerminalGraphicsMode.Grayscale, 1.0, 1.0)]
    [InlineData(TerminalGraphicsMode.TrueTone, 1.0, 1.0)]
    [InlineData(TerminalGraphicsMode.BestGlyph, 2.0, 4.0)]
    [InlineData(TerminalGraphicsMode.BestGlyphTrueColor, 2.0, 4.0)]
    public void FitImage_TargetPixelSize_MatchesModeDensity(
        TerminalGraphicsMode mode,
        double expectedColumnDensity,
        double expectedRowDensity)
    {
        var source = new PixelBuffer(320, 200);
        (int cols, int rows) = PresentationSession.FitImage(source, 80, 40, mode);
        (int pixelWidth, int pixelHeight) = TerminalGraphicsModes.TargetPixelSize(cols, rows, mode);

        pixelWidth.Should().Be(Math.Max(1, (int)Math.Ceiling(cols * expectedColumnDensity)));
        pixelHeight.Should().Be(Math.Max(1, (int)Math.Ceiling(rows * expectedRowDensity)));
    }

    [Theory]
    [InlineData(TerminalGraphicsMode.HalfBlockColor)]
    [InlineData(TerminalGraphicsMode.BrailleMono)]
    [InlineData(TerminalGraphicsMode.Grayscale)]
    [InlineData(TerminalGraphicsMode.TrueTone)]
    [InlineData(TerminalGraphicsMode.BestGlyph)]
    [InlineData(TerminalGraphicsMode.BestGlyphTrueColor)]
    public void PrepareCanvas_SingleResize_MatchesTargetPixelSize(TerminalGraphicsMode mode)
    {
        var source = new PixelBuffer(640, 480);
        (PixelBuffer scaled, int cols, int rows) = PresentationSession.PrepareCanvas(source, 60, 30, mode);
        (int targetWidth, int targetHeight) = TerminalGraphicsModes.TargetPixelSize(cols, rows, mode);

        scaled.Width.Should().Be(targetWidth);
        scaled.Height.Should().Be(targetHeight);
    }

    [Fact]
    public void RenderScaled_WithPreparedBuffer_DoesNotRequireSecondResize()
    {
        var source = new PixelBuffer(100, 100);
        for (int y = 0; y < 100; y++)
            for (int x = 0; x < 100; x++)
                source.SetPixel(x, y, new Pixel((byte)x, (byte)y, 0));

        (PixelBuffer scaled, int cols, int rows) = PresentationSession.PrepareCanvas(
            source, 20, 10, TerminalGraphicsMode.TrueTone);
        FakeTerminalRenderer renderer = new(cols, rows);

        TerminalGraphicsRenderer.RenderScaled(renderer, scaled, TerminalGraphicsMode.TrueTone, 0, 0, cols, rows);

        renderer.GetCell(0, 0).Should().NotBe(TerminalCell.Unknown);
        renderer.GetCell(cols - 1, rows - 1).Should().NotBe(TerminalCell.Black);
    }
}
