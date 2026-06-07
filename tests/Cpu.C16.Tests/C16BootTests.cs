using Cpu.C16.System;
using Cpu.Chips.TED7360;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Cpu.C16.Tests;

public class C16BootTests
{
    private readonly ITestOutputHelper _output;
    public C16BootTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Machine_LoadsProfile()
    {
        using var machine = C16Machine.Load("c16.json");
        machine.Should().NotBeNull();
    }

    [Fact]
    public void Machine_TED_WiredCorrectly()
    {
        using var machine = C16Machine.Load("c16.json");
        machine.Ted.BaseAddress.Should().Be(0xFF00);
        machine.Chip[TED7360Constants.REG_FF06_CR1].Should().Be(TED7360Constants.Default_FF06);
    }

    [Fact]
    public void Boot_ReachesBasicPrompt()
    {
        using var machine = C16Machine.Load("c16.json");
        machine.Run(8_000_000);
        var cpu = machine.Board.Cpu;
        _output.WriteLine($"PC={cpu.Regs.PC:X4} SP={cpu.Regs.SP:X2}");

        machine.RenderVideo();
        var pixels = machine.Video.Pixels;
        int nonBlack = 0;
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i] != 0) nonBlack++;

        _output.WriteLine($"Non-black pixels: {nonBlack}");
        _output.WriteLine($"CR1={machine.Chip[6]:X2} CR2={machine.Chip[7]:X2} FF14={machine.Chip[0x14]:X2}");
        _output.WriteLine($"CHARGEN={machine.Chip[0x13]:X2} B0C={machine.Chip[0x15]:X2}");

        int screenAddr = ((machine.Chip[0x14] & 0xF8) << 8) | 0x400;
        int chars = 0;
        for (int i = 0; i < 40 * 25; i++)
        {
            byte ch = machine.Board.Bus.Read((ushort)(screenAddr + i));
            if (ch != 0 && ch != 0x20) chars++;
        }
        _output.WriteLine($"Screen at ${screenAddr:X4}: {chars} non-space chars");
        Assert.True(nonBlack > 100 || chars > 5,
            $"Boot should show text (pixels={nonBlack} chars={chars} at ${screenAddr:X4})");
    }
}
