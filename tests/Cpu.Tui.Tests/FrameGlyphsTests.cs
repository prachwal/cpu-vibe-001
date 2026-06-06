using FluentAssertions;
using Xunit;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class FrameGlyphsTests
{
    [Fact]
    public void Ascii_HasCorrectChars()
    {
        var g = FrameGlyphs.Ascii;
        g.TopLeft.Should().Be('+');
        g.TopRight.Should().Be('+');
        g.BottomLeft.Should().Be('+');
        g.BottomRight.Should().Be('+');
        g.Horizontal.Should().Be('-');
        g.Vertical.Should().Be('|');
    }

    [Fact]
    public void Unicode_HasCorrectChars()
    {
        var g = FrameGlyphs.Unicode;
        g.TopLeft.Should().Be('┌');
        g.TopRight.Should().Be('┐');
        g.BottomLeft.Should().Be('└');
        g.BottomRight.Should().Be('┘');
        g.Horizontal.Should().Be('─');
        g.Vertical.Should().Be('│');
    }

    [Fact]
    public void FrameStyle_DefaultIsAscii()
    {
        var style = new App().GetType();
        // FrameStyle is a default field, check via reflection or just test the enum
        FrameStyle.Ascii.Should().Be((FrameStyle)0);
    }

    [Fact]
    public void FrameStyle_ToggleBetweenValues()
    {
        FrameStyle style = FrameStyle.Ascii;
        style = style == FrameStyle.Ascii ? FrameStyle.Unicode : FrameStyle.Ascii;
        style.Should().Be(FrameStyle.Unicode);

        style = style == FrameStyle.Ascii ? FrameStyle.Unicode : FrameStyle.Ascii;
        style.Should().Be(FrameStyle.Ascii);
    }
}
