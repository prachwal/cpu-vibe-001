using Cpu.Board.Core;
using Cpu.Board.Core.Adapters;
using CpuBase;
using Cpu.Tui.Devices.Pia;
using FluentAssertions;
using Xunit;

namespace Cpu.Board.Tests;

public class Apple1BoardTests
{
    private static MachineProfile LoadApple1Profile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "apple-1.json");
        return MachineBoard.LoadProfile(path);
    }

    [Fact]
    public void Profile_LoadsCorrectly()
    {
        var profile = LoadApple1Profile();
        profile.Name.Should().Be("Apple 1");
        profile.Cpu.Type.Should().Be("mos6502");
        profile.Memory.Should().HaveCount(2);
        profile.Pia.Should().NotBeNull();
        profile.Pia!.BaseAddress.Should().Be(0xD010);
    }

    [Fact]
    public void Board_CreatesCpuAndMemory()
    {
        var profile = LoadApple1Profile();
        using var board = new MachineBoard(profile);

        board.Cpu.Should().NotBeNull();
        board.Bus.Should().NotBeNull();
        board.Devices.Should().HaveCount(2);
    }

    [Fact]
    public void WozMonitor_LoadedAtCorrectAddress()
    {
        var profile = LoadApple1Profile();
        using var board = new MachineBoard(profile);

        // Woz Monitor starts at 0xFF00, should have non-zero data in first byte
        byte firstByte = board.Cpu.Memory.Read(0xFF00);
        firstByte.Should().NotBe(0, "Woz Monitor ROM should be loaded");

        // Reset vector should be at 0xFFFC-0xFFFD
        byte resetLo = board.Cpu.Memory.Read(0xFFFC);
        byte resetHi = board.Cpu.Memory.Read(0xFFFD);
        ushort resetVector = (ushort)((resetHi << 8) | resetLo);
        resetVector.Should().BeInRange(0xFF00, 0xFFFF, "reset should point into Woz Monitor");
    }

    [Fact]
    public void Reset_SetsPcToResetVector()
    {
        var profile = LoadApple1Profile();
        using var board = new MachineBoard(profile);

        board.Reset();

        // PC should be set from the reset vector at 0xFFFC
        byte lo = board.Cpu.Memory.Read(0xFFFC);
        byte hi = board.Cpu.Memory.Read(0xFFFD);
        ushort expectedPc = (ushort)((hi << 8) | lo);
        board.Cpu.Regs.PC.Should().Be(expectedPc);
    }

    [Fact]
    public void Step_AdvancesCycles()
    {
        var profile = LoadApple1Profile();
        using var board = new MachineBoard(profile);

        board.Reset();
        long before = board.Cpu.Cycles;
        board.Step();
        board.Cpu.Cycles.Should().BeGreaterThan(before);
    }

    [Fact]
    public void PIA_AttachedToBus()
    {
        var profile = LoadApple1Profile();
        using var board = new MachineBoard(profile);

        var pia = new PiaDevice(profile.Pia!.BaseAddress);
        board.AttachDevice(pia);

        // PIA occupies 6 consecutive register addresses
        pia.Accepts(0xD010).Should().BeTrue();
        pia.Accepts(0xD013).Should().BeTrue();
        pia.Accepts(0xD015).Should().BeTrue();  // last register at base+5
        pia.Accepts(0xD016).Should().BeFalse();
    }

    [Fact]
    public void PIA_RoutesToDisplayAdapter()
    {
        var display = new Apple1DisplayAdapter(40, 24);

        display.Write((byte)'A');

        display.GetChar(0, 0).Should().Be('A');
    }

    [Fact]
    public void DisplayAdapter_NewlineMovesCursor()
    {
        var display = new Apple1DisplayAdapter(40, 24);
        display.Write((byte)'A');
        display.Write(0x0D);
        display.Write((byte)'B');

        display.GetChar(0, 0).Should().Be('A');
        display.GetChar(0, 1).Should().Be('B');
    }

    [Fact]
    public void DisplayAdapter_ScrollsAtBottom()
    {
        var display = new Apple1DisplayAdapter(40, 24);

        for (int i = 0; i < 23; i++)
        {
            display.Write((byte)('A' + i));
            display.Write(0x0D);
        }

        display.GetChar(0, 22).Should().Be('W', "row 22 should have 'W' (letter #23)");

        display.Write((byte)'X');
        display.GetChar(0, 23).Should().Be('X', "row 23 should have 'X' (letter #24)");

        display.Write(0x0D);

        display.GetChar(0, 22).Should().Be('X', "row 22 should now have 'X' (scrolled from row 23)");
        display.GetChar(0, 23).Should().Be(' ', "row 23 should be blank after scroll");
    }
}
