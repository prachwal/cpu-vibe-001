using System.Text;
using FluentAssertions;
using Xunit;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class AnsiBuilderTests
{
    [Fact]
    public void MoveTo_GeneratesCorrectSequence()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.MoveTo(0, 0);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[1;1H");
    }

    [Fact]
    public void MoveTo_RowCol10()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.MoveTo(9, 9);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[10;10H");
    }

    [Fact]
    public void MoveTo_LargeCoords()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.MoveTo(79, 24);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[25;80H");
    }

    [Fact]
    public void SetColor_FgOnly()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Black;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.SetColor(ConsoleColor.DarkRed, ConsoleColor.Black, ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[31m"); // DarkRed→ANSI 1→31
    }

    [Fact]
    public void SetColor_BrightFg_UsesBrightForegroundSgr()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Black;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.SetColor(ConsoleColor.Blue, ConsoleColor.Black, ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[94m");
    }

    [Fact]
    public void SetColor_BgOnly()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Gray;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.SetColor(ConsoleColor.Gray, ConsoleColor.DarkBlue, ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[44m"); // DarkBlue→ANSI 4→44
    }

    [Fact]
    public void SetColor_BrightBg_UsesBrightBackgroundSgr()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Gray;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.SetColor(ConsoleColor.Gray, ConsoleColor.Blue, ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[104m");
    }

    [Fact]
    public void SetColor_BothFgBg()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Black;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.SetColor(ConsoleColor.DarkRed, ConsoleColor.DarkBlue, ref curFg, ref curBg);

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[31;44m"); // DarkRed→1→31, DarkBlue→4→44
    }

    [Fact]
    public void SetColor_SameColors_NoOutput()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Gray;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.SetColor(ConsoleColor.Gray, ConsoleColor.Black, ref curFg, ref curBg);

        builder.Length.Should().Be(0);
    }

    [Fact]
    public void WriteChar_SingleChar()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.WriteChar('A');

        buf[0].Should().Be((byte)'A');
        builder.Length.Should().Be(1);
    }

    [Fact]
    public void WriteChar_UnicodeBoxDrawing_EncodesUtf8()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.WriteChar('┌');

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("┌");
        builder.Length.Should().Be(3);
    }

    [Fact]
    public void WriteAscii_MultipleChars()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.WriteAscii("HELLO");

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("HELLO");
    }

    [Fact]
    public void Combined_MoveTo_SetColor_WriteChar()
    {
        Span<byte> buf = stackalloc byte[128];
        var builder = new AnsiBuilder(buf);
        ConsoleColor curFg = ConsoleColor.Black;
        ConsoleColor curBg = ConsoleColor.Black;

        builder.MoveTo(5, 10);
        builder.SetColor(ConsoleColor.DarkGreen, ConsoleColor.Black, ref curFg, ref curBg);
        builder.WriteChar('X');

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[11;6H\x1b[32mX"); // DarkGreen→2→32, bg unchanged
    }

    [Fact]
    public void BufferOverflow_DoesNotCrash()
    {
        Span<byte> buf = stackalloc byte[4];
        var builder = new AnsiBuilder(buf);

        builder.MoveTo(0, 0); // needs 6 bytes

        builder.Length.Should().Be(4); // capped at buffer size
    }

    [Fact]
    public void WriteInt_SingleDigit()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.MoveTo(0, 0); // row=1, col=1

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Contain("1;1");
    }

    [Fact]
    public void WriteInt_ThreeDigits()
    {
        Span<byte> buf = stackalloc byte[64];
        var builder = new AnsiBuilder(buf);

        builder.MoveTo(99, 99); // row=100, col=100

        string result = Encoding.UTF8.GetString(buf.Slice(0, builder.Length));
        result.Should().Be("\x1b[100;100H");
    }
}
