using FluentAssertions;

namespace Mos6502.Tests.RomTests;

/// <summary>
/// Klaus Dormann 6502 Functional Test
/// https://github.com/Klaus2m5/6502_65C02_functional_tests
/// 
/// Binary is a flat 64KB memory image starting at $0000.
/// Code begins at $0400 (code_segment).
/// Data segment is at $0000-$03FF.
/// Success: PC reaches $3469 (jmp * loop)
/// Failure: PC reaches a trap (jmp * in error handler)
/// 
/// Run with: dotnet test --filter "KlausDormann"
/// </summary>
public class KlausDormannTest
{
    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(KlausDormannTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename} (searched from {assemblyDir})");
    }

    private const ushort SuccessAddress = 0x3469;
    private const int MaxInstructions = 100_000_000;

    [Fact]
    public void FunctionalTest_Passes()
    {
        var cpu = new Cpu();
        byte[] rom = File.ReadAllBytes(FindRom("6502_functional_test.bin"));

        rom.Length.Should().Be(65536, "ROM should be exactly 64KB flat memory image");

        for (int i = 0; i < rom.Length; i++)
            cpu.Memory.Write((ushort)i, rom[i]);

        cpu.Regs.PC = 0x0400;
        cpu.Regs.SP = 0xFF;

        int instructions = 0;
        while (instructions < MaxInstructions)
        {
            if (cpu.Regs.PC == SuccessAddress)
                break;
            cpu.Step();
            instructions++;
        }

        cpu.Regs.PC.Should().Be(SuccessAddress,
            $"test should reach success loop at ${SuccessAddress:X4} (ran {instructions} instructions, PC=${cpu.Regs.PC:X4})");
    }
}
