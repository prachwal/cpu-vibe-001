using Cpu.Vic20.Devices;
using FluentAssertions;
using Xunit;

namespace Cpu.Vic20.Tests;

public class VicHostKeyMapTests
{
    [Theory]
    [InlineData('a', 0, 1)]
    [InlineData('1', 6, 1)]
    [InlineData(' ', 4, 0)]
    public void TryMapHostChar_MapsPrintableKeys(char ch, int expectedRow, int expectedCol)
    {
        VicHostKeyMap.TryMapHostChar(ch, out int row, out int col).Should().BeTrue();
        row.Should().Be(expectedRow);
        col.Should().Be(expectedCol);
    }

    [Fact]
    public void TryMapConsoleKey_MapsLetterWhenKeyCharIsZero()
    {
        var key = new ConsoleKeyInfo('\0', ConsoleKey.A, false, false, false);

        VicHostKeyMap.TryMapConsoleKey(key, out int row, out int col).Should().BeTrue();
        row.Should().Be(0);
        col.Should().Be(1);
    }

    [Fact]
    public void TryMapConsoleKey_MapsDigitWhenKeyCharIsZero()
    {
        var key = new ConsoleKeyInfo('\0', ConsoleKey.D1, false, false, false);

        VicHostKeyMap.TryMapConsoleKey(key, out int row, out int col).Should().BeTrue();
        row.Should().Be(6);
        col.Should().Be(1);
    }

    [Fact]
    public void TryMapConsoleKey_MapsShiftedLetterViaConsoleKey()
    {
        var key = new ConsoleKeyInfo('\0', ConsoleKey.B, true, false, false);

        VicHostKeyMap.TryMapConsoleKey(key, out int row, out int col).Should().BeTrue();
        row.Should().Be(0);
        col.Should().Be(2);
    }
}
