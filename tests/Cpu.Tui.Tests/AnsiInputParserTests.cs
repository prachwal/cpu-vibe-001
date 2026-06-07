using Cpu.Tui.Input;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Cpu.Tui.Tests;

public class AnsiInputParserTests
{
    [Fact]
    public void SgrMouse_Motion_IsNotKey()
    {
        byte[] seq = Encoding.ASCII.GetBytes("\x1b[<35;10;5M");

        AnsiInputParser.TryParse(seq, out int consumed, out TerminalInput input).Should().BeTrue();
        consumed.Should().Be(seq.Length);
        input.Kind.Should().Be(TerminalInputKind.Mouse);
        input.Mouse.IsMotion.Should().BeTrue();
        input.Mouse.X.Should().Be(9);
        input.Mouse.Y.Should().Be(4);
    }

    [Fact]
    public void SgrMouse_LeftPress_DecodesPosition()
    {
        byte[] seq = Encoding.ASCII.GetBytes("\x1b[<0;12;8M");

        AnsiInputParser.TryParse(seq, out _, out TerminalInput input).Should().BeTrue();
        input.Mouse.IsMotion.Should().BeFalse();
        input.Mouse.IsRelease.Should().BeFalse();
        input.Mouse.Button.Should().Be(0);
        input.Mouse.X.Should().Be(11);
        input.Mouse.Y.Should().Be(7);
    }

    [Fact]
    public void SgrMouse_LeftRelease_IsRelease()
    {
        byte[] seq = Encoding.ASCII.GetBytes("\x1b[<0;12;8m");

        AnsiInputParser.TryParse(seq, out _, out TerminalInput input).Should().BeTrue();
        input.Mouse.IsRelease.Should().BeTrue();
    }

    [Fact]
    public void LegacyMouse_Press_ParsesAsMouse()
    {
        byte[] seq = [(byte)'\x1b', (byte)'[', (byte)'M', (byte)(32 + 0), (byte)(32 + 12), (byte)(32 + 8)];

        AnsiInputParser.TryParse(seq, out int consumed, out TerminalInput input).Should().BeTrue();
        consumed.Should().Be(6);
        input.Kind.Should().Be(TerminalInputKind.Mouse);
        input.Mouse.X.Should().Be(11);
        input.Mouse.Y.Should().Be(7);
    }

    [Fact]
    public void ArrowKey_StillParsesAsKey()
    {
        byte[] seq = Encoding.ASCII.GetBytes("\x1b[A");

        AnsiInputParser.TryParse(seq, out _, out TerminalInput input).Should().BeTrue();
        input.Kind.Should().Be(TerminalInputKind.Key);
        input.Key.Key.Should().Be(ConsoleKey.UpArrow);
    }

    [Fact]
    public void PlainLetter_ParsesAsKey()
    {
        byte[] seq = [(byte)'a'];

        AnsiInputParser.TryParse(seq, out _, out TerminalInput input).Should().BeTrue();
        input.Kind.Should().Be(TerminalInputKind.Key);
        input.Key.KeyChar.Should().Be('a');
    }
}
