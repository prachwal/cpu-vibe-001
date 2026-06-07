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
    [InlineData(TerminalGraphicsMode.ColorShade, 1.0, 1.0)]
    [InlineData(TerminalGraphicsMode.TrueTone, 1.0, 1.0)]
    [InlineData(TerminalGraphicsMode.BestGlyph, 2.0, 4.0)]
    [InlineData(TerminalGraphicsMode.BestGlyphTrueColor, 2.0, 4.0)]
    public void FitImage_TargetPixelSize_MatchesRenderDensity(
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
    [InlineData(TerminalGraphicsMode.Grayscale)]
    [InlineData(TerminalGraphicsMode.ColorShade)]
    [InlineData(TerminalGraphicsMode.TrueTone)]
    [InlineData(TerminalGraphicsMode.BestGlyph)]
    public void FitImage_PreservesSourceAspectRatioInLayout(TerminalGraphicsMode mode)
    {
        const int imgWidth = 640;
        const int imgHeight = 480;
        (int cols, int rows) = PresentationSession.FitImage(imgWidth, imgHeight, 80, 40, mode);
        double sourceAspect = imgWidth / (double)imgHeight;
        double layoutAspect = cols * TerminalGraphicsModes.LayoutPixelsPerCellColumn(mode)
            / (rows * TerminalGraphicsModes.LayoutPixelsPerCellRow(mode));
        layoutAspect.Should().BeApproximately(sourceAspect, 0.02);
    }

    [Fact]
    public void FitImage_TrueTone_UsesSameCellGridAsHalfBlock()
    {
        (int hbCols, int hbRows) = PresentationSession.FitImage(320, 200, 80, 40, TerminalGraphicsMode.HalfBlockColor);
        (int ttCols, int ttRows) = PresentationSession.FitImage(320, 200, 80, 40, TerminalGraphicsMode.TrueTone);
        ttCols.Should().Be(hbCols);
        ttRows.Should().Be(hbRows);
    }

    [Fact]
    public void ScaleForCells_FillsTargetBufferWithLetterboxWhenNeeded()
    {
        var source = new PixelBuffer(320, 200);
        for (int y = 80; y < 120; y++)
            for (int x = 140; x < 180; x++)
                source.SetPixel(x, y, Pixel.White);
        (int cols, int rows) = PresentationSession.FitImage(source, 80, 40, TerminalGraphicsMode.TrueTone);
        (int targetWidth, int targetHeight) = TerminalGraphicsModes.TargetPixelSize(cols, rows, TerminalGraphicsMode.TrueTone);
        PixelBuffer scaled = TerminalGraphicsRenderer.ScaleForCells(source, cols, rows, TerminalGraphicsMode.TrueTone);
        scaled.Width.Should().Be(targetWidth);
        scaled.Height.Should().Be(targetHeight);
        scaled.GetPixel(0, 0).Should().Be(Pixel.Black);
        scaled.GetPixel(targetWidth / 2, targetHeight / 2).Should().NotBe(Pixel.Black);
    }

    [Theory]
    [InlineData(TerminalGraphicsMode.HalfBlockColor)]
    [InlineData(TerminalGraphicsMode.BrailleMono)]
    [InlineData(TerminalGraphicsMode.Grayscale)]
    [InlineData(TerminalGraphicsMode.ColorShade)]
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

        renderer.GetCell(cols / 2, rows / 2).Should().NotBe(TerminalCell.Black);
    }
}
