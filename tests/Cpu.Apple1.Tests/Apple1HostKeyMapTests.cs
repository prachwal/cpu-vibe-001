using Cpu.Apple1.Devices;
using FluentAssertions;
using Xunit;

namespace Cpu.Apple1.Tests;

public class Apple1HostKeyMapTests
{
    [Fact]
    public void PanelRows_UsesUniqueMachineCodes()
    {
        Apple1HostKeyMap.PanelRows.Select(r => r.MachineCode).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData('\b', Apple1KeyboardCodes.DeleteLeft)]
    [InlineData('\r', Apple1KeyboardCodes.Return)]
    public void TryMapHostChar_MapsUniversalSpecialKeys(char ch, byte expected)
    {
        Apple1HostKeyMap.TryMapHostChar(ch, wozMonitor: false, out byte code).Should().BeTrue();
        code.Should().Be(expected);

        Apple1HostKeyMap.TryMapHostChar(ch, wozMonitor: true, out code).Should().BeTrue();
        code.Should().Be(expected);
    }

    [Fact]
    public void TryMapHostChar_WozProfile_MapsHexAndCommands()
    {
        Apple1HostKeyMap.TryMapHostChar('5', wozMonitor: true, out byte digit).Should().BeTrue();
        digit.Should().Be(0xB5);

        Apple1HostKeyMap.TryMapHostChar('C', wozMonitor: true, out byte letter).Should().BeTrue();
        letter.Should().Be(0xC3);

        Apple1HostKeyMap.TryMapHostChar('.', wozMonitor: true, out byte dot).Should().BeTrue();
        dot.Should().Be(Apple1KeyboardCodes.BlockExamine);

        Apple1HostKeyMap.TryMapHostChar('R', wozMonitor: true, out byte run).Should().BeTrue();
        run.Should().Be(Apple1KeyboardCodes.Run);
    }

    [Fact]
    public void TryMapHostChar_BasicProfile_KeepsAsciiForPrintable()
    {
        Apple1HostKeyMap.TryMapHostChar('R', wozMonitor: false, out byte code).Should().BeTrue();
        code.Should().Be((byte)'R');

        Apple1HostKeyMap.TryMapHostChar('1', wozMonitor: false, out code).Should().BeTrue();
        code.Should().Be((byte)'1');
    }

    [Fact]
    public void FormatPanelLine_FitsPanelWidth()
    {
        Apple1HostKeyBinding row = Apple1HostKeyMap.PanelRows[0];
        string line = Apple1HostKeyMap.FormatPanelLine(row, 30);
        line.Length.Should().Be(30);
    }
}
