using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class ZexdocMcatTest
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(ZexdocMcatTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename}");
    }

    [Fact]
    public void LdSpNn_LoadsCorrectSp()
    {
        _cpu.Reset();
        _cpu.Memory.Write(0x0111, 0x8B);
        _cpu.Memory.Write(0x0112, 0x1D);
        _cpu.Memory.Write(0x0100, 0xED);
        _cpu.Memory.Write(0x0101, 0x7B);
        _cpu.Memory.Write(0x0102, 0x11);
        _cpu.Memory.Write(0x0103, 0x01);
        _cpu.Regs.PC = 0x0100;
        _cpu.Step();
        _cpu.Regs.SP.Should().Be(0x1D8B);
    }

    [Fact]
    public void TestFunction_McatContainsCorrectValues()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        _cpu.Reset();
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Load(0x0100, binary);
        _cpu.Memory.Write(0x0005, 0xC9);

        long iterations = 0;

        while (iterations < 6_000_000)
        {
            ushort pc = _cpu.Regs.PC;

            if (pc == 0x0005)
            {
                _cpu.Regs.PC = _cpu.StackPop();
                iterations++;
                continue;
            }
            if (pc == 0x0000) break;

            // Stop AFTER the test function returns for the first time
            // The test function is at 0x1D2A, and returns with EI at 0x1D59
            // After EI, the code loads HL from msbt and stores to msat
            // Then masks flags, then calls updcrc
            // After all that, it returns to the main loop
            if (pc == 0x1D2A && iterations > 1000)
            {
                // We're about to enter the test function
                // Let's run through it step by step and check what happens
                RunTestFunctionAndCheck();
                return;
            }

            _cpu.Step();
            iterations++;
        }

        Assert.Fail($"Didn't find test function entry after {iterations} iterations");
    }

    private void RunTestFunctionAndCheck()
    {
        // Execute the test function step by step
        for (int i = 0; i < 50; i++)
        {
            ushort pc = _cpu.Regs.PC;
            _cpu.Step();

            if (pc == 0x1D46)
            {
                // Just executed ld (0x1D8B),sp = spat
                ushort spatVal = (ushort)(_cpu.Memory.Read((ushort)0x1D8B) | (_cpu.Memory.Read((ushort)0x1D8C) << 8));
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"After ld (spat),sp: SP={_cpu.Regs.SP:X4}, spat={spatVal:X4}");
                sb.AppendLine($"Memory at 0x1D8B: {_cpu.Memory.Read((ushort)0x1D8B):X2} {_cpu.Memory.Read((ushort)0x1D8C):X2}");

                // Continue executing until we've done the pushes
                for (int j = 0; j < 20; j++)
                {
                    ushort pc2 = _cpu.Regs.PC;
                    _cpu.Step();

                    if (pc2 == 0x1D55)
                    {
                        // Just executed ld sp,(0x1D8D) = restore SP from spsav
                        sb.AppendLine($"After ld sp,(spsav): SP={_cpu.Regs.SP:X4}");
                    }
                }

                // Check msat area
                sb.AppendLine("\nmsat area (0x1D7D..0x1D8A):");
                for (int k = 0; k < 14; k++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1D7D + k)):X2} ");
                sb.AppendLine();

                sb.AppendLine($"\nSP={_cpu.Regs.SP:X4} AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} DE={_cpu.Regs.DE:X4} HL={_cpu.Regs.HL:X4} IX={_cpu.Regs.IX:X4} IY={_cpu.Regs.IY:X4}");

                Assert.Fail(sb.ToString());
                return;
            }
        }

        Assert.Fail("Didn't reach ld (spat),sp");
    }
}
