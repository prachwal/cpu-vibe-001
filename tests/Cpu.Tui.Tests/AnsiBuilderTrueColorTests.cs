using System.Text;
using FluentAssertions;
using Xunit;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class AnsiBuilderTrueColorTests
{
    [Fact]
    public void SetColor_RgbForeground_EmitsCorrectSequence()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        var curFg = TerminalColor.FromConsole(ConsoleColor.Black);
        var curBg = TerminalColor.FromConsole(ConsoleColor.Black);

        builder.SetColor(
            TerminalColor.FromRgb(255, 10, 20),
            TerminalColor.FromConsole(ConsoleColor.Black),
            ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[38;2;255;10;20m");
    }

    [Fact]
    public void SetColor_RgbBackground_EmitsCorrectSequence()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        var curFg = TerminalColor.FromConsole(ConsoleColor.Black);
        var curBg = TerminalColor.FromConsole(ConsoleColor.Black);

        builder.SetColor(
            TerminalColor.FromConsole(ConsoleColor.Gray),
            TerminalColor.FromRgb(1, 2, 3),
            ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[37;48;2;1;2;3m");
    }

    [Fact]
    public void SetColor_BothRgb_EmitsBoth()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        var curFg = TerminalColor.FromConsole(ConsoleColor.Black);
        var curBg = TerminalColor.FromConsole(ConsoleColor.Black);

        builder.SetColor(
            TerminalColor.FromRgb(10, 20, 30),
            TerminalColor.FromRgb(40, 50, 60),
            ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[38;2;10;20;30;48;2;40;50;60m");
    }

    [Fact]
    public void SetColor_SameRgb_NothingEmitted()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        var curFg = TerminalColor.FromRgb(100, 200, 50);
        var curBg = TerminalColor.FromConsole(ConsoleColor.Black);

        builder.SetColor(
            TerminalColor.FromRgb(100, 200, 50),
            TerminalColor.FromConsole(ConsoleColor.Black),
            ref curFg, ref curBg);

        builder.Length.Should().Be(0);
    }

    [Fact]
    public void SetColor_RgbThenConsole_MixedCorrectly()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        var curFg = TerminalColor.FromConsole(ConsoleColor.Black);
        var curBg = TerminalColor.FromConsole(ConsoleColor.Black);

        builder.SetColor(TerminalColor.FromRgb(255, 0, 0), TerminalColor.FromConsole(ConsoleColor.White), ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[38;2;255;0;0;107m"); // 107 = White background
    }
}
