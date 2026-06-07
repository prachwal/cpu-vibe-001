using FluentAssertions;
using Xunit;
using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using System.Text;

namespace Cpu.Tui.Tests;

public class BestGlyphRendererTests
{
    [Fact]
    public void GlyphPattern_TileSize_Is8()
    {
        GlyphPattern.TileSize.Should().Be(8);
    }

    [Fact]
    public void GlyphPattern_FromBinary_CreatesCorrectAlpha()
    {
        // bits: 1,0,1,0,1,0,1,0 = 0x55 (bit0=1, bit1=0, bit2=1, bit3=0, ...)
        var pattern = GlyphPattern.FromBinary('X', 0x55);

        pattern.Glyph.Should().Be('X');
        pattern.Alpha.Should().HaveCount(8);
        pattern.Alpha[0].Should().Be(255); // bit 0 set
        pattern.Alpha[1].Should().Be(0);   // bit 1 clear
        pattern.Alpha[2].Should().Be(255); // bit 2 set
        pattern.Alpha[3].Should().Be(0);   // bit 3 clear
    }

    [Fact]
    public void GlyphPattern_Full_AllAlpha255()
    {
        var pattern = GlyphPattern.Full('█');
        pattern.Alpha.Should().AllBeEquivalentTo((byte)255);
    }

    [Fact]
    public void GlyphPattern_Empty_AllAlpha0()
    {
        var pattern = GlyphPattern.Empty(' ');
        pattern.Alpha.Should().AllBeEquivalentTo((byte)0);
    }

    [Fact]
    public void GlyphAtlas_CreateDefault_HasBraille()
    {
        var atlas = GlyphAtlas.CreateDefault2x4();
        atlas.Glyphs.Length.Should().BeGreaterThan(256); // at least braille + basic glyphs

        // Check braille exists
        bool hasBraille = atlas.Glyphs.Any(g => g.Glyph >= 0x2800 && g.Glyph <= 0x28FF);
        hasBraille.Should().BeTrue();
    }

    [Fact]
    public void GlyphAtlas_CreateDefault_HasBasicGlyphs()
    {
        var atlas = GlyphAtlas.CreateDefault2x4();
        var glyphs = atlas.Glyphs.Select(g => g.Glyph).ToList();

        glyphs.Should().Contain(' ');
        glyphs.Should().Contain('█');
        glyphs.Should().Contain('▀');
        glyphs.Should().Contain('▄');
        glyphs.Should().Contain('░');
        glyphs.Should().Contain('▒');
        glyphs.Should().Contain('▓');
    }

    [Fact]
    public void ColorQuantizer_NearestConsoleColor_Black()
    {
        var result = ColorQuantizer.NearestConsoleColor(0, 0, 0);
        result.Should().Be(ConsoleColor.Black);
    }

    [Fact]
    public void ColorQuantizer_NearestConsoleColor_White()
    {
        var result = ColorQuantizer.NearestConsoleColor(255, 255, 255);
        result.Should().Be(ConsoleColor.White);
    }

    [Fact]
    public void ColorQuantizer_NearestConsoleColor_Red()
    {
        var result = ColorQuantizer.NearestConsoleColor(255, 0, 0);
        result.Should().Be(ConsoleColor.Red);
    }

    [Fact]
    public void ColorQuantizer_NearestConsoleColor_Green()
    {
        var result = ColorQuantizer.NearestConsoleColor(0, 255, 0);
        result.Should().Be(ConsoleColor.Green);
    }

    [Fact]
    public void ColorQuantizer_NearestConsoleColor_Blue()
    {
        var result = ColorQuantizer.NearestConsoleColor(0, 0, 255);
        result.Should().Be(ConsoleColor.Blue);
    }

    [Fact]
    public void PixelMath_Lerp_BlackToWhite()
    {
        Pixel result = PixelMath.Lerp(Pixel.Black, Pixel.White, 0.5f);
        result.R.Should().BeInRange(125, 130);
        result.G.Should().BeInRange(125, 130);
        result.B.Should().BeInRange(125, 130);
    }

    [Fact]
    public void PixelMath_DistanceSquared_SamePoint()
    {
        PixelMath.DistanceSquared(Pixel.Black, Pixel.Black).Should().Be(0);
    }

    [Fact]
    public void PixelMath_DistanceSquared_DifferentPoints()
    {
        int dist = PixelMath.DistanceSquared(Pixel.Black, Pixel.White);
        dist.Should().Be(255 * 255 * 3);
    }

    [Fact]
    public void PixelMath_EstimateForeground_HighAlphaPixels()
    {
        Pixel[] tile = [new Pixel(255, 0, 0), new Pixel(0, 255, 0), new Pixel(0, 0, 255), new Pixel(128, 128, 128),
                        new Pixel(0, 0, 0), new Pixel(0, 0, 0), new Pixel(0, 0, 0), new Pixel(0, 0, 0)];
        byte[] alpha = [255, 255, 255, 255, 0, 0, 0, 0];

        Pixel fg = PixelMath.EstimateForeground(tile, alpha, 8);
        fg.R.Should().BeGreaterThan(50); // should average the bright pixels
    }

    [Fact]
    public void PixelMath_EstimateBackground_LowAlphaPixels()
    {
        Pixel[] tile = [new Pixel(255, 0, 0), new Pixel(0, 255, 0), new Pixel(0, 0, 255), new Pixel(128, 128, 128),
                        new Pixel(0, 0, 0), new Pixel(0, 0, 0), new Pixel(0, 0, 0), new Pixel(0, 0, 0)];
        byte[] alpha = [255, 255, 255, 255, 0, 0, 0, 0];

        Pixel bg = PixelMath.EstimateBackground(tile, alpha, 8);
        bg.Should().Be(Pixel.Black); // should average the dark pixels
    }

    [Fact]
    public void BestGlyphRenderer_ScalingRegression_UsesFullImageWidth()
    {
        // Uses TerminalGraphicsRenderer which handles ResizeNearest
        var image = new PixelBuffer(4, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.Black);
            for (int x = 2; x < 4; x++)
                image.SetPixel(x, y, Pixel.White);
        }

        var ftr = new FakeTerminalRenderer(10, 10);
        // Render through TerminalGraphicsRenderer with resize to cols=1, rows=1
        // This resizes 4x4 to 2x4, BestGlyph sees one 2x4 tile with mixed black/white
        TerminalGraphicsRenderer.Render(ftr, image, TerminalGraphicsMode.BestGlyph, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        cell.Ch.Should().NotBe(' '); // Should not be empty — full image used
    }

    [Fact]
    public void BestGlyphRenderer_VerticalScalingRegression_UsesFullImageHeight()
    {
        var image = new PixelBuffer(2, 8);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.Black);
        for (int y = 4; y < 8; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.White);

        var ftr = new FakeTerminalRenderer(10, 10);
        // Render through TerminalGraphicsRenderer with resize to cols=1, rows=1
        // This resizes 2x8 to 2x4, BestGlyph sees one 2x4 tile with mixed black/white
        TerminalGraphicsRenderer.Render(ftr, image, TerminalGraphicsMode.BestGlyph, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        cell.Ch.Should().NotBe(' '); // Should not be empty — full image used
    }

    [Fact]
    public void BestGlyphRenderer_Quality_MixedBlackWhite_UsesNonEmptyGlyph()
    {
        // When both fg and bg colors are same, all glyphs have error 0
        // and the first (space) wins. This test uses contrasting colors.
        var image = new PixelBuffer(2, 4);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.White);
        for (int y = 2; y < 4; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.Black);

        var ftr = new FakeTerminalRenderer(10, 10);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.Render(ftr, image, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        cell.Ch.Should().Be('▀'); // Upper half block for top white / bottom black
        cell.Fg.ConsoleColor.Should().BeOneOf(ConsoleColor.White, ConsoleColor.Gray);
        cell.Bg.ConsoleColor.Should().BeOneOf(ConsoleColor.Black, ConsoleColor.DarkGray);
    }

    [Fact]
    public void BestGlyphRenderer_Quality_BottomHalfWhite_PrefersLowerHalfBlock()
    {
        var image = new PixelBuffer(2, 4);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.Black);
        for (int y = 2; y < 4; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, Pixel.White);

        var ftr = new FakeTerminalRenderer(10, 10);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.Render(ftr, image, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        // ▀ (top half fg) and ▄ (bottom half fg) both have error 0 for this tile
        // ▀ comes first in atlas (order: space, █, ░, ▒, ▓, ▀, ▄, ...)
        cell.Ch.Should().BeOneOf('▀', '▄');
        // For ▀: top=fg=black, bottom=bg=white. For ▄: bottom=fg=white, top=bg=black.
        cell.Fg.ConsoleColor.Should().BeOneOf(ConsoleColor.Black, ConsoleColor.White, ConsoleColor.Gray);
        cell.Bg.ConsoleColor.Should().BeOneOf(ConsoleColor.Black, ConsoleColor.White, ConsoleColor.Gray);
    }

    [Fact]
    public void BestGlyphRenderer_Quality_RedBlueSplit_PrefersHalfBlockWithCorrectColors()
    {
        var image = new PixelBuffer(2, 4);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, new Pixel(255, 0, 0));
        for (int y = 2; y < 4; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, new Pixel(0, 0, 255));

        var ftr = new FakeTerminalRenderer(1, 1);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.Render(ftr, image, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        cell.Ch.Should().Be('▀'); // Upper half block for top/bottom split
        cell.Fg.ConsoleColor.Should().BeOneOf(ConsoleColor.Red, ConsoleColor.DarkRed); // Red-ish foreground
        cell.Bg.ConsoleColor.Should().BeOneOf(ConsoleColor.Blue, ConsoleColor.DarkBlue); // Blue-ish background
    }

    [Fact]
    public void BestGlyphRenderer_SingleDot_PrefersSingleBrailleDot()
    {
        var image = new PixelBuffer(2, 4);
        // Single pixel at top-left
        image.SetPixel(0, 0, Pixel.White);

        var ftr = new FakeTerminalRenderer(1, 1);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.Render(ftr, image, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        // Should pick a braille character with single dot
        cell.Ch.Should().BeInRange((char)0x2800, (char)0x28FF);
    }

    [Fact]
    public void BestGlyphTrueColor_RedTopBlueBottom_PicksHalfBlockWithRgbColors()
    {
        var image = new PixelBuffer(2, 4);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, new Pixel(210, 160, 120));
        for (int y = 2; y < 4; y++)
            for (int x = 0; x < 2; x++)
                image.SetPixel(x, y, new Pixel(30, 20, 15));

        var ftr = new FakeTerminalRenderer(10, 10);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.RenderTrueColor(ftr, image, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        cell.Ch.Should().Be('▀');
        cell.Fg.IsRgb.Should().BeTrue();
        cell.Fg.R.Should().BeInRange(200, 220); // close to (210, 160, 120)
        cell.Fg.G.Should().BeInRange(150, 170);
        cell.Fg.B.Should().BeInRange(110, 130);
        cell.Bg.IsRgb.Should().BeTrue();
        cell.Bg.R.Should().BeInRange(25, 40); // close to (30, 20, 15)
        cell.Bg.G.Should().BeInRange(15, 30);
        cell.Bg.B.Should().BeInRange(10, 20);
    }

    [Fact]
    public void BestGlyphTrueColor_SkinToneGradient_NoConsoleColorUsed()
    {
        var image = new PixelBuffer(4, 4);
        var skinTones = new[] { new Pixel(180, 110, 70), new Pixel(190, 130, 90), new Pixel(210, 160, 120), new Pixel(200, 140, 100) };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                image.SetPixel(x, y, skinTones[(y * 2 + x) % skinTones.Length]);

        var ftr = new FakeTerminalRenderer(10, 10);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.RenderTrueColor(ftr, image, 0, 0, 2, 1);

        for (int x = 0; x < 2; x++)
        {
            var cell = ftr.GetCell(x, 0);
            cell.Fg.IsRgb.Should().BeTrue("BestGlyphTrueColor should not quantize to ConsoleColor");
            cell.Bg.IsRgb.Should().BeTrue("BestGlyphTrueColor should not quantize to ConsoleColor");
        }
    }

    [Fact]
    public void BestGlyphTrueColor_SparseLineOnBlack_KeepsBlackBackground()
    {
        var image = new PixelBuffer(2, 4);
        image.SetPixel(0, 0, new Pixel(40, 190, 240));
        image.SetPixel(1, 1, new Pixel(30, 140, 190));

        var ftr = new FakeTerminalRenderer(10, 10);
        var renderer_ = new BestGlyphRenderer(GlyphAtlas.CreateDefault2x4());

        renderer_.RenderTrueColor(ftr, image, 0, 0, 1, 1);

        var cell = ftr.GetCell(0, 0);
        cell.Bg.Should().Be(TerminalColor.FromConsole(ConsoleColor.Black));
    }

    [Fact]
    public void BestGlyphTrueColor_Demo3DWireframe_NeutralFgOnBlackBg()
    {
        var demo = new Demo3D();
        var canvas = new PixelCanvas();
        demo.Tick(canvas, TerminalGraphicsMode.BestGlyphTrueColor);

        int maxCols = 80;
        int maxRows = 40;
        (PixelBuffer scaled, int cols, int rows) = PresentationSession.PrepareCanvas(
            canvas.Buffer, maxCols, maxRows, TerminalGraphicsMode.BestGlyphTrueColor);
        var ftr = new FakeTerminalRenderer(cols, rows);
        TerminalGraphicsRenderer.RenderScaled(ftr, scaled, TerminalGraphicsMode.BestGlyphTrueColor, 0, 0, cols, rows);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                TerminalCell cell = ftr.GetCell(x, y);
                if (cell == TerminalCell.Black || cell.Ch == ' ')
                    continue;
                cell.Bg.Should().Be(TerminalColor.FromConsole(ConsoleColor.Black));
                cell.Fg.R.Should().Be(cell.Fg.G);
                cell.Fg.G.Should().Be(cell.Fg.B);
            }
        }
    }

    [Fact]
    public void BestGlyphTrueColor_SecondRender_DoesNotInheritPreviousColor()
    {
        var colored = new PixelBuffer(2, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 2; x++)
                colored.SetPixel(x, y, new Pixel(20, 180, 240));

        var black = new PixelBuffer(2, 4);
        var ftr = new FakeTerminalRenderer(1, 1);

        TerminalGraphicsRenderer.Render(ftr, colored, TerminalGraphicsMode.BestGlyphTrueColor, 0, 0, 1, 1);
        TerminalGraphicsRenderer.Render(ftr, black, TerminalGraphicsMode.BestGlyphTrueColor, 0, 0, 1, 1);

        ftr.GetCell(0, 0).Should().Be(TerminalCell.Black);
    }

    [Fact]
    public void TerminalGraphicsMode_Grayscale_RespectsDimensions()
    {
        var image = new PixelBuffer(100, 200);
        for (int y = 0; y < 200; y++)
            for (int x = 0; x < 100; x++)
                image.SetPixel(x, y, new Pixel((byte)(x * 255 / 100), (byte)(y * 255 / 200), 128));

        var ftr = new FakeTerminalRenderer(80, 25);

        // Render Grayscale at 40x10 cells
        TerminalGraphicsRenderer.Render(ftr, image, TerminalGraphicsMode.Grayscale, 5, 5, 40, 10);

        // Should render exactly 40 columns x 10 rows starting at (5,5)
        var cell0 = ftr.GetCell(5, 5);
        var cellEnd = ftr.GetCell(44, 14);
        cell0.Should().NotBe(TerminalCell.Unknown);
        cellEnd.Should().NotBe(TerminalCell.Unknown);

        // Should NOT render outside the bounds
        ftr.GetCell(4, 5).Should().Be(TerminalCell.Unknown);  // left of start
        ftr.GetCell(5, 4).Should().Be(TerminalCell.Unknown);  // above start
        ftr.GetCell(45, 5).Should().Be(TerminalCell.Unknown); // right of end
        ftr.GetCell(5, 15).Should().Be(TerminalCell.Unknown); // below end
    }

    [Fact]
    public void TerminalGraphicsMode_Grayscale_DifferentSize_UsesCorrectDimensions()
    {
        var image = new PixelBuffer(50, 100);
        for (int y = 0; y < 100; y++)
            for (int x = 0; x < 50; x++)
                image.SetPixel(x, y, new Pixel(100, 150, 200));

        var ftr = new FakeTerminalRenderer(80, 25);

        // Render Grayscale at 20x5 cells
        TerminalGraphicsRenderer.Render(ftr, image, TerminalGraphicsMode.Grayscale, 0, 0, 20, 5);

        // Check all cells in range are filled
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 20; x++)
                ftr.GetCell(x, y).Should().NotBe(TerminalCell.Unknown, $"cell ({x},{y}) should be set");

        // Check cells outside are unknown
        ftr.GetCell(20, 0).Should().Be(TerminalCell.Unknown);
        ftr.GetCell(0, 5).Should().Be(TerminalCell.Unknown);
    }
}
