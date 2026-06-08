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
        using var machine = C16Machine.Load("c16-ntsc.json");
        machine.Should().NotBeNull();
    }

    [Fact]
    public void Machine_TED_WiredCorrectly()
    {
        using var machine = C16Machine.Load("c16-ntsc.json");
        machine.Ted.BaseAddress.Should().Be(0xFF00);
        machine.Chip[TED7360Constants.REG_FF06_CR1].Should().Be(TED7360Constants.Default_FF06);
    }

    [Fact]
    public void MemoryMapper_SwitchesRomAndRamLikeTed()
    {
        using var machine = C16Machine.Load("c16-ntsc.json");

        machine.Board.Bus.Read(0xFFFC).Should().Be(0xF6);
        machine.Board.Bus.Read(0xFFFD).Should().Be(0xFF);

        machine.Board.Bus.Write(0xC000, 0x5A);
        machine.Board.Bus.Read(0xC000).Should().NotBe(0x5A);

        machine.Board.Bus.Write(0xFF3F, 0);
        machine.Memory.RomSelected.Should().BeFalse();
        machine.Board.Bus.Write(0xC000, 0x5A);
        machine.Board.Bus.Read(0xC000).Should().Be(0x5A);

        machine.Board.Bus.Write(0xFF3E, 0);
        machine.Memory.RomSelected.Should().BeTrue();
        machine.Board.Bus.Read(0xC000).Should().NotBe(0x5A);
    }

    [Fact]
    public void KeyboardMatrix_Pio2SelectsRowsAndTedReadsColumns()
    {
        using var machine = C16Machine.Load("c16-ntsc.json");

        machine.PressKey(1, 2);
        machine.Board.Bus.Write(C16MemoryMap.Pio2BaseAddress, 0xFD);

        machine.Board.Bus.Read((ushort)(C16MemoryMap.TedBaseAddress + TED7360Constants.REG_FF08_KEYBOARD))
            .Should().Be(0xFB);

        machine.ReleaseAllKeys();
        machine.Board.Bus.Read((ushort)(C16MemoryMap.TedBaseAddress + TED7360Constants.REG_FF08_KEYBOARD))
            .Should().Be(0xFF);
    }

    [Fact]
    public void KeyboardBuffer_InputAppearsOnBasicScreen()
    {
        using var machine = C16Machine.Load("c16-ntsc.json");
        machine.Run(30_000_000);

        machine.FillKeyboardBuffer((byte)'A');
        machine.Run(1_000_000);

        string screenText = ReadScreen(machine);
        screenText.Should().Contain("READY");
        screenText.Should().Contain("A");
    }

    [Fact]
    public void Boot_ReachesBasicPrompt()
    {
        using var machine = C16Machine.Load("c16-ntsc.json");
        machine.Run(30_000_000);
        var cpu = machine.Board.Cpu;
        _output.WriteLine($"PC={cpu.Regs.PC:X4} SP={cpu.Regs.SP:X2}");
        _output.WriteLine($"ROM={machine.Memory.RomSelected} LO={machine.Memory.LowRomBank} HI={machine.Memory.HighRomBank}");

        machine.RenderVideo();
        var pixels = machine.Video.Pixels;
        int nonBlack = 0;
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i] != 0) nonBlack++;

        _output.WriteLine($"Non-black pixels: {nonBlack}");
        _output.WriteLine($"CR1={machine.Chip[6]:X2} CR2={machine.Chip[7]:X2} FF14={machine.Chip[0x14]:X2}");
        _output.WriteLine($"CHARGEN={machine.Chip[0x13]:X2} B0C={machine.Chip[0x15]:X2}");

        string screenText = ReadScreen(machine, out int screenAddr, out int chars);
        _output.WriteLine($"Screen at ${screenAddr:X4}: {chars} non-space chars");
        _output.WriteLine(screenText[..Math.Min(screenText.Length, 240)]);
        screenText.Should().Contain("READY");
        nonBlack.Should().BeGreaterThan(100);
    }

    private static string ReadScreen(C16Machine machine) => ReadScreen(machine, out _, out _);

    private static string ReadScreen(C16Machine machine, out int screenAddr, out int chars)
    {
        screenAddr = ((machine.Chip[0x14] & 0xF8) << 8) | 0x400;
        chars = 0;
        char[] text = new char[40 * 25];
        for (int i = 0; i < 40 * 25; i++)
        {
            byte ch = machine.Board.Bus.Read((ushort)(screenAddr + i));
            text[i] = ToDisplayChar(ch);
            if (ch != 0 && ch != 0x20)
                chars++;
        }
        return new string(text);
    }

    private static char ToDisplayChar(byte code)
    {
        code &= 0x7F;
        if (code >= 0x01 && code <= 0x1A) return (char)('A' + code - 1);
        if (code == 0x20) return ' ';
        if (code >= 0x21 && code <= 0x3F) return (char)code;
        if (code >= 0x40 && code <= 0x5A) return (char)(code + 0x20);
        return '.';
    }
}
