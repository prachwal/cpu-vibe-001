using FluentAssertions;
using Xunit;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class TerminalColorTests
{
    [Fact]
    public void FromConsole_SetsIsRgbFalse()
    {
        var c = TerminalColor.FromConsole(ConsoleColor.Red);
        c.IsRgb.Should().BeFalse();
        c.ConsoleColor.Should().Be(ConsoleColor.Red);
    }

    [Fact]
    public void FromRgb_SetsIsRgbTrue()
    {
        var c = TerminalColor.FromRgb(255, 10, 20);
        c.IsRgb.Should().BeTrue();
        c.R.Should().Be(255);
        c.G.Should().Be(10);
        c.B.Should().Be(20);
    }

    [Fact]
    public void Equality_SameRgb_ReturnsTrue()
    {
        var a = TerminalColor.FromRgb(100, 200, 50);
        var b = TerminalColor.FromRgb(100, 200, 50);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentRgb_ReturnsFalse()
    {
        var a = TerminalColor.FromRgb(100, 200, 50);
        var b = TerminalColor.FromRgb(101, 200, 50);
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_SameConsoleColor_ReturnsTrue()
    {
        var a = TerminalColor.FromConsole(ConsoleColor.Red);
        var b = TerminalColor.FromConsole(ConsoleColor.Red);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentConsoleColor_ReturnsFalse()
    {
        var a = TerminalColor.FromConsole(ConsoleColor.Red);
        var b = TerminalColor.FromConsole(ConsoleColor.Blue);
        a.Should().NotBe(b);
    }

    [Fact]
    public void RgbAndConsoleColor_AreNotEqual()
    {
        var rgb = TerminalColor.FromRgb(255, 0, 0);
        var cc = TerminalColor.FromConsole(ConsoleColor.Red);
        rgb.Should().NotBe(cc);
    }
}
