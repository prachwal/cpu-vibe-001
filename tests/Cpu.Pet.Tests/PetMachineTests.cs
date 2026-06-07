using Cpu.Board.Core;
using Cpu.Pet.System;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public class PetMachineTests
{
    private static PetMachine LoadMachine()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        return new PetMachine(profile);
    }

    [Fact]
    public void Profile_LoadsCorrectly()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        profile.Name.Should().Be("Commodore PET");
        profile.Memory.Should().HaveCountGreaterThan(4);
    }

    [Fact]
    public void Board_LoadsKernalRom()
    {
        using var machine = LoadMachine();
        byte b = machine.ReadMemory(0xF000);
        b.Should().NotBe(0, "kernal ROM should be loaded at $F000");
    }

    [Fact]
    public void Reset_SetsPcFromResetVector()
    {
        using var machine = LoadMachine();
        machine.Reset();
        byte lo = machine.ReadMemory(0xFFFC);
        byte hi = machine.ReadMemory(0xFFFD);
        ushort expected = (ushort)((hi << 8) | lo);
        machine.Board.Cpu.Regs.PC.Should().Be(expected);
    }

    [Fact]
    public void Run_ReachesReadyPrompt()
    {
        using var machine = LoadMachine();
        machine.Reset();

        bool foundReady = false;
        for (int batch = 0; batch < 3000 && !foundReady; batch++)
        {
            machine.Step(10_000);
            for (int row = 0; row < machine.Rows && !foundReady; row++)
            {
                var line = new char[machine.Columns];
                for (int col = 0; col < machine.Columns; col++)
                    line[col] = machine.GetDisplayCell(col, row);

                string text = new string(line).TrimEnd();
                if (text.Contains("READY", StringComparison.Ordinal))
                    foundReady = true;
            }
        }

        foundReady.Should().BeTrue("PET should boot to READY prompt");
    }

    [Fact]
    public void TryTypeChar_WritesToKeyboardBuffer()
    {
        using var machine = LoadMachine();
        machine.Reset();
        machine.TryTypeChar('P').Should().BeTrue();
        machine.KeyboardBufferCount().Should().Be(1);
        machine.ReadMemory(0x026F).Should().Be((byte)'P');
        machine.ReadMemory(0x0270).Should().NotBe((byte)'P', "first key belongs at $026F when count was 0");
    }
}
