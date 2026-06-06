using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class ZexdocTraceTest
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(ZexdocTraceTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename}");
    }

    [Fact(Skip = "Diagnostic trace; run manually when investigating ZEXDOC machine-state capture failures.")]
    public void TraceFirstAdc16Test()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        _cpu.Reset();
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Load(0x0100, binary);
        _cpu.Memory.Write(0x0005, 0xC9);

        long iterations = 0;

        // Run to find the FIRST time we enter the test function (at 0x1D2A)
        while (iterations < 6_000_000)
        {
            if (_cpu.Regs.PC == 0x0005)
            {
                _cpu.Regs.PC = _cpu.StackPop();
                iterations++;
                continue;
            }
            if (_cpu.Regs.PC == 0x0000) break;

            if (_cpu.Regs.PC == 0x1D2A && iterations > 500)
            {
                TraceTestFunction();
                return;
            }

            _cpu.Step();
            iterations++;
        }
        Assert.Fail("Didn't reach test function");
    }

    private void TraceTestFunction()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Tracing test function (ADC HL,BC) ===");
        sb.AppendLine($"Entry: SP={_cpu.Regs.SP:X4} AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} DE={_cpu.Regs.DE:X4} HL={_cpu.Regs.HL:X4} IX={_cpu.Regs.IX:X4} IY={_cpu.Regs.IY:X4}");

        // Execute step by step until we're past the pushes
        for (int i = 0; i < 50; i++)
        {
            ushort pc = _cpu.Regs.PC;
            string instr = GetInstrName(pc);

            if (pc == 0x1D5A)
            {
                // About to execute ld hl,(msbt)
                sb.AppendLine($"  [{pc:X4}] {instr}  HL={_cpu.Regs.HL:X4}");
                _cpu.Step();
                sb.AppendLine($"  After ld hl,(msbt): HL={_cpu.Regs.HL:X4} = mem[{_cpu.Memory.Read((ushort)0x0103):X2} {_cpu.Memory.Read((ushort)0x0104):X2}]");

                // Execute ld (msat),hl
                _cpu.Step();
                sb.AppendLine($"  After ld (msat),hl: msat[0..1] = {_cpu.Memory.Read((ushort)0x1D7D):X2} {_cpu.Memory.Read((ushort)0x1D7E):X2}");
                break;
            }

            if (pc == 0x1D4D)
            {
                sb.AppendLine($"  Before pushes: SP={_cpu.Regs.SP:X4}");
                sb.AppendLine($"  AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} DE={_cpu.Regs.DE:X4} HL={_cpu.Regs.HL:X4} IX={_cpu.Regs.IX:X4} IY={_cpu.Regs.IY:X4}");
            }

            if (pc == 0x1D46)
            {
                sb.AppendLine($"  [{pc:X4}] {instr}  SP={_cpu.Regs.SP:X4}");
                _cpu.Step();
                sb.AppendLine($"  After ld (spat),sp: spat={_cpu.Memory.Read((ushort)0x1D8B):X2} {_cpu.Memory.Read((ushort)0x1D8C):X2} (should be SP={_cpu.Regs.SP:X4})");
                continue;
            }

            if (pc == 0x1D4A)
            {
                sb.AppendLine($"  [{pc:X4}] {instr}  SP={_cpu.Regs.SP:X4}");
                _cpu.Step();
                sb.AppendLine($"  After ld sp,spat: SP={_cpu.Regs.SP:X4}");
                continue;
            }

            if (pc == 0x1D55)
            {
                sb.AppendLine($"  [{pc:X4}] {instr}  SP={_cpu.Regs.SP:X4}");
                _cpu.Step();
                sb.AppendLine($"  After ld sp,(spsav): SP={_cpu.Regs.SP:X4}");
                continue;
            }

            _cpu.Step();

            if (pc >= 0x1D2A && pc <= 0x1D55)
            {
                sb.AppendLine($"  [{pc:X4}] {instr}  SP={_cpu.Regs.SP:X4} AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} HL={_cpu.Regs.HL:X4}");
            }
        }

        // Now check what's in msat area
        sb.AppendLine("\n=== msat area after test function ===");
        sb.AppendLine($"  Expected (from msbt): 2C 83 88 4F 2B F2 ?? ?? 1F 7E 63 15 ?? ?? (HL and F changed by ADC HL,BC)");
        sb.Append("  Actual:   ");
        for (int k = 0; k < 14; k++)
            sb.Append($"{_cpu.Memory.Read((ushort)(0x1D7D + k)):X2} ");
        sb.AppendLine();

        sb.Append("  msat raw: ");
        for (int k = 0; k < 16; k++)
            sb.Append($"{_cpu.Memory.Read((ushort)(0x1D7D + k)):X2} ");
        sb.AppendLine();

        Assert.Fail(sb.ToString());
    }

    private static string GetInstrName(ushort pc)
    {
        return pc switch
        {
            0x1D2A => "push af",
            0x1D2B => "push bc",
            0x1D2C => "push de",
            0x1D2D => "push hl",
            0x1D2E => "di",
            0x1D2F => "ld (spsav),sp",
            0x1D33 => "ld sp,msbt+2",
            0x1D36 => "pop iy",
            0x1D38 => "pop ix",
            0x1D3A => "pop hl",
            0x1D3B => "pop de",
            0x1D3C => "pop bc",
            0x1D3D => "pop af",
            0x1D3E => "ld sp,(spbt)",
            0x1D42 => "iut",
            0x1D46 => "ld (spat),sp",
            0x1D4A => "ld sp,spat",
            0x1D4D => "push af (result)",
            0x1D4E => "push bc (result)",
            0x1D4F => "push de (result)",
            0x1D50 => "push hl (result)",
            0x1D51 => "push ix",
            0x1D53 => "push iy",
            0x1D55 => "ld sp,(spsav)",
            0x1D59 => "ei",
            0x1D5A => "ld hl,(msbt)",
            0x1D5D => "ld (msat),hl",
            _ => $"0x{pc:X4}"
        };
    }
}
