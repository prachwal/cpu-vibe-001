using Cpu.Pet.Devices;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public class PetHostKeyMapTests
{
    [Fact]
    public void PanelRows_UsesUniquePetCodes()
    {
        PetHostKeyMap.PanelRows.Select(r => r.PetCode).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData('\b', PetKeyboardCodes.DeleteLeft)]
    [InlineData('\r', PetKeyboardCodes.Return)]
    public void TryMapHostChar_MapsSpecialKeys(char ch, byte expected)
    {
        PetHostKeyMap.TryMapHostChar(ch, out byte code).Should().BeTrue();
        code.Should().Be(expected);
    }

    [Fact]
    public void TryMapConsoleKey_MapsArrowKeys()
    {
        PetHostKeyMap.TryMapConsoleKey(K(ConsoleKey.UpArrow), out byte up).Should().BeTrue();
        up.Should().Be(0x11);

        PetHostKeyMap.TryMapConsoleKey(K(ConsoleKey.DownArrow), out byte down).Should().BeTrue();
        down.Should().Be(0x1D);
    }

    [Fact]
    public void FormatPanelLine_FitsPanelWidth()
    {
        PetHostKeyBinding row = PetHostKeyMap.PanelRows[0];
        string line = PetHostKeyMap.FormatPanelLine(row, 30);
        line.Length.Should().Be(30);
    }

    private static ConsoleKeyInfo K(ConsoleKey key) => new('\0', key, false, false, false);
}
