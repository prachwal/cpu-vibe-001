using FluentAssertions;
using Xunit;
using Cpu.Tui.Graphics;

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
    public void BestGlyphRenderer_Render_DoesNotThrow()
    {
        using var stream = new MemoryStream();
        var renderer = new Cpu.Tui.Rendering.AnsiTerminalRenderer(stream);
        renderer.Resize(10, 10);

        var atlas = GlyphAtlas.CreateDefault2x4();
        var bestGlyph = new BestGlyphRenderer(atlas);

        var image = new PixelBuffer(20, 40);
        // Fill with red
        for (int y = 0; y < 40; y++)
            for (int x = 0; x < 20; x++)
                image.SetPixel(x, y, new Pixel(255, 0, 0));

        Action act = () => bestGlyph.Render(renderer, image, 0, 0, 10, 10);
        act.Should().NotThrow();
    }
}
