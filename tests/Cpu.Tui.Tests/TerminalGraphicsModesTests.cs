using Cpu.Tui;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class TerminalGraphicsModesTests
{
    [Theory]
    [InlineData(TerminalGraphicsMode.HalfBlockColor, TerminalGraphicsMode.BrailleMono)]
    [InlineData(TerminalGraphicsMode.BrailleMono, TerminalGraphicsMode.Grayscale)]
    [InlineData(TerminalGraphicsMode.Grayscale, TerminalGraphicsMode.TrueTone)]
    [InlineData(TerminalGraphicsMode.TrueTone, TerminalGraphicsMode.BestGlyph)]
    [InlineData(TerminalGraphicsMode.BestGlyph, TerminalGraphicsMode.BestGlyphTrueColor)]
    [InlineData(TerminalGraphicsMode.BestGlyphTrueColor, TerminalGraphicsMode.HalfBlockColor)]
    public void Next_AdvancesCycle(TerminalGraphicsMode current, TerminalGraphicsMode expected) =>
        TerminalGraphicsModes.Next(current).Should().Be(expected);

    [Fact]
    public void PixelsPerCellRow_GrayscaleAndTrueTone_AreOneToOne()
    {
        TerminalGraphicsModes.PixelsPerCellRow(TerminalGraphicsMode.Grayscale).Should().Be(1.0);
        TerminalGraphicsModes.PixelsPerCellRow(TerminalGraphicsMode.TrueTone).Should().Be(1.0);
    }
}
