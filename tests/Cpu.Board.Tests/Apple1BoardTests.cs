using Cpu.Board.Core;
using Cpu.Board.Core.Adapters;
using CpuBase;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;
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

    private static MachineProfile LoadApple1BasicProfile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "apple-1-basic.json");
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

    [Fact]
    public void KeyboardAdapter_QueuesKeysAndNormalizesToUppercase()
    {
        var keyboard = new Apple1KeyboardAdapter();

        foreach (char ch in "e000r\r")
            keyboard.EnqueueKey((byte)ch);

        byte[] actual = new byte[6];
        for (int i = 0; i < actual.Length; i++)
            actual[i] = keyboard.ReadKey();

        actual.Should().Equal((byte)'E', (byte)'0', (byte)'0', (byte)'0', (byte)'R', (byte)'\r');
        keyboard.HasKey.Should().BeFalse();
    }

    [Fact]
    public void BasicRom_EntryJumpsToInitializer()
    {
        var profile = LoadApple1BasicProfile();
        using var board = new MachineBoard(profile);

        board.Reset();
        board.Cpu.Memory.Read(0xE000).Should().Be(0x4C);
        board.Cpu.Memory.Read(0xE001).Should().Be(0xB0);
        board.Cpu.Memory.Read(0xE002).Should().Be(0xE2);

        board.Cpu.Regs.PC = 0xE000;
        board.Step();

        board.Cpu.Regs.PC.Should().Be(0xE2B0);
    }

    [Fact]
    public void BasicProfile_E000R_StartsBasicRom()
    {
        var profile = LoadApple1BasicProfile();
        using var board = new MachineBoard(profile);
        var pia = new PiaDevice(profile.Pia!.BaseAddress);
        board.AttachDevice(pia);

        var display = new Apple1DisplayAdapter(40, 24);
        var keyboard = new Apple1KeyboardAdapter();
        var view = new Apple1View(board, pia, display, keyboard, "BASIC");
        using var stream = new MemoryStream();
        using var renderer = new AnsiTerminalRenderer(stream);

        renderer.Resize(100, 30);
        view.Activate(renderer, new TermRect(0, 0, 100, 29));
        view.EnqueueText("E000R\r");

        for (int i = 0; i < 500; i++)
            view.StepCpu(5000);

        board.Cpu.Regs.PC.Should().Be(0xE003);
        board.Cpu.Memory.Read(0xE003).Should().Be(0xAD);
        board.Cpu.Memory.Read(0xE004).Should().Be(0x11);
        board.Cpu.Memory.Read(0xE005).Should().Be(0xD0);
    }
}
