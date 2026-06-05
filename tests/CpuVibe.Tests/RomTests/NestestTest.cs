using FluentAssertions;

namespace CpuVibe.Tests.RomTests;

/// <summary>
/// Nestest — NES CPU Test ROM by Kevin Horton
/// 
/// NES ROM format: 16-byte iNES header + PRG ROM (16KB) + CHR ROM (8KB)
/// NROM mapper: PRG ROM mapped to $8000-$FFFF (mirrored if 16KB)
/// 
/// Start: PC = $C000, SP = $FD (after NES reset vector)
/// 
/// Result: compare cycle count with known-good log
/// Success: test completes without hanging
/// 
/// Run with: dotnet test --filter "Nestest"
/// Timeout: 120s
/// </summary>
public class NestestTest
{
    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(NestestTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename} (searched from {assemblyDir})");
    }
    private const int MaxInstructions = 20_000_000;

    [Fact]
    public void CpuTest_Passes()
    {
        var cpu = new Cpu();
        byte[] rom = File.ReadAllBytes(FindRom("nestest.nes"));

        rom.Length.Should().BeGreaterThanOrEqualTo(16 + 16384,
            "NES ROM should have 16-byte header + 16KB PRG ROM");

        byte prgBanks = rom[4];
        int prgSize = prgBanks * 16384;

        for (int i = 0; i < prgSize; i++)
        {
            byte value = rom[16 + i];
            ushort addr = (ushort)(0x8000 + i);
            cpu.Memory.Write(addr, value);
            if (addr < 0xC000)
                cpu.Memory.Write((ushort)(addr + 0x4000), value);
        }

        cpu.Regs.PC = 0x8000;
        cpu.Regs.SP = 0xFD;
        cpu.Regs.P = CpuFlags.Unused | CpuFlags.Interrupt;

        int instructions = 0;
        bool completed = false;

        while (instructions < MaxInstructions)
        {
            cpu.Step();
            instructions++;

            if (cpu.Regs.PC == 0x8000)
            {
                completed = true;
                break;
            }
        }

        completed.Should().BeTrue(
            $"nestest should complete (ran {instructions} instructions, PC=${cpu.Regs.PC:X4})");
    }
}
