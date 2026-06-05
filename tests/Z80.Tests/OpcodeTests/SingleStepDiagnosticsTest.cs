using FluentAssertions;
using Xunit;
using Z80.Core;
using Z80.Instructions;

namespace Z80.Tests.OpcodeTests;

/// <summary>
/// Single-step through the test function to find where msat gets corrupted.
/// Focus on: ld (spat),sp → ld sp,spat → push sequence → ld sp,(spsav)
/// </summary>
public class SingleStepDiagnosticsTest
{
    private readonly Cpu _cpu = new();

    private void LoadZexall()
    {
        _cpu.Reset();
        var binary = File.ReadAllBytes("rom/zexall.com");
        Array.Copy(binary, 0, _cpu.Memory.InternalRam, 0x100, binary.Length);
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
    }

    /// <summary>
    /// Step until PC reaches target, return number of steps taken.
    /// </summary>
    private int StepUntil(ushort target, int maxSteps = 50_000_000)
    {
        for (int i = 0; i < maxSteps; i++)
        {
            if (_cpu.Regs.PC == target) return i;
            _cpu.Step();
        }
        return -1;
    }

    /// <summary>
    /// Step until PC matches any of the targets, return (target, steps).
    /// </summary>
    private (ushort hit, int steps) StepUntilAny(ushort[] targets, int maxSteps = 50_000_000)
    {
        for (int i = 0; i < maxSteps; i++)
        {
            foreach (var t in targets)
                if (_cpu.Regs.PC == t) return (t, i);
            _cpu.Step();
        }
        return (0, -1);
    }

    /// <summary>
    /// Capture full state dump at a given point.
    /// </summary>
    private string DumpState(string label)
    {
        var sp = _cpu.Regs.SP;
        var memDump = new System.Text.StringBuilder();
        for (ushort a = 0x1D7D; a <= 0x1D8E; a++)
            memDump.Append($"{_cpu.Memory.Read(a):X2} ");

        return $"""
            [{label}] PC=0x{_cpu.Regs.PC:X4} SP=0x{sp:X4}
            AF=0x{_cpu.Regs.AF:X4} BC=0x{_cpu.Regs.BC:X4} DE=0x{_cpu.Regs.DE:X4} HL=0x{_cpu.Regs.HL:X4}
            IX=0x{_cpu.Regs.IX:X4} IY=0x{_cpu.Regs.IY:X4}
            msat(0x1D7D-0x1D8E): {memDump}
            """;
    }

    /// <summary>
    /// Run to first test function entry (0x1D2A), capture state, then single-step
    /// through the entire test function instruction by instruction.
    /// </summary>
    [Fact]
    public void Trace_TestFunction_Instruction_By_Instruction()
    {
        LoadZexall();
        var steps = StepUntil(0x1D2A);
        steps.Should().BeGreaterThan(0);

        // State at entry
        var output = new System.Text.StringBuilder();
        output.AppendLine("=== TEST FUNCTION ENTRY (0x1D2A) ===");
        output.AppendLine(DumpState("Entry"));
        output.AppendLine();

        // Execute instructions until RET (0x1D7C) or up to 100 instructions
        for (int i = 0; i < 100 && _cpu.Regs.PC != 0x1D7C; i++)
        {
            ushort pc = _cpu.Regs.PC;
            byte op = _cpu.Memory.Read(pc);
            byte op2 = (pc + 1 < 0x10000) ? _cpu.Memory.Read((ushort)(pc + 1)) : (byte)0;
            byte op3 = (pc + 2 < 0x10000) ? _cpu.Memory.Read((ushort)(pc + 2)) : (byte)0;
            byte op4 = (pc + 3 < 0x10000) ? _cpu.Memory.Read((ushort)(pc + 3)) : (byte)0;

            string instr = op switch
            {
                0xF5 => "PUSH AF",
                0xC5 => "PUSH BC",
                0xD5 => "PUSH DE",
                0xE5 => "PUSH HL",
                0xF1 => "POP AF",
                0xC1 => "POP BC",
                0xD1 => "POP DE",
                0xE1 => "POP HL",
                0x31 => $"LD SP,0x{op3:X2}{op2:X2}",
                0xFB => "EI",
                0xF3 => "DI",
                0xC9 => "RET",
                0x00 => "NOP",
                0xDD when op2 == 0xE1 => "POP IX",
                0xDD when op2 == 0xE5 => "PUSH IX",
                0xFD when op2 == 0xE1 => "POP IY",
                0xFD when op2 == 0xE5 => "PUSH IY",
                0xED when op2 == 0x7B => $"LD SP,(0x{op4:X2}{op3:X2})",
                0xED when op2 == 0x73 => $"LD (0x{op4:X2}{op3:X2}),SP",
                0xED when op2 == 0x42 => "SBC HL,BC",
                0x2A => $"LD HL,(0x{op3:X2}{op2:X2})",
                0x22 => $"LD (0x{op3:X2}{op2:X2}),HL",
                0x3A => $"LD A,(0x{op3:X2}{op2:X2})",
                0x32 => $"LD (0x{op3:X2}{op2:X2}),A",
                0xE6 => $"AND 0x{op2:X2}",
                0x06 => $"LD B,0x{op2:X2}",
                0x21 => $"LD HL,0x{op3:X2}{op2:X2}",
                0x5E => "LD E,(HL)",
                0x16 => $"LD D,0x{op2:X2}",
                0x19 => "ADD HL,DE",
                _ => $"?? op={op:X2} {op2:X2} {op3:X2}"
            };

            output.AppendLine($"  0x{pc:X4}: {instr}");

            // Special dump at key points
            if (pc == 0x1D4A || pc == 0x1D55 || pc == 0x1D5E || pc == 0x1D6C || pc == 0x1D7C)
            {
                output.AppendLine($"    >>> SP=0x{_cpu.Regs.SP:X4} AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} DE={_cpu.Regs.DE:X4} HL={_cpu.Regs.HL:X4} IX={_cpu.Regs.IX:X4} IY={_cpu.Regs.IY:X4}");
                // Dump msat after pushes
                if (pc == 0x1D55 || pc == 0x1D6C || pc == 0x1D7C)
                {
                    var sb = new System.Text.StringBuilder();
                    for (ushort a = 0x1D7D; a <= 0x1D8C; a++)
                        sb.Append($"{_cpu.Memory.Read(a):X2} ");
                    output.AppendLine($"    >>> msat: {sb}");
                }
                // Dump spsav
                output.AppendLine($"    >>> spsav={_cpu.Memory.Read(0x1D8D):X2} {_cpu.Memory.Read(0x1D8E):X2}");
            }

            _cpu.Step();
        }

        output.AppendLine();
        output.AppendLine("=== FINAL STATE AFTER RET ===");
        output.AppendLine(DumpState("AfterRET"));

        // Dump crcval and expected
        var crc = new System.Text.StringBuilder();
        for (ushort a = 0x1E85; a <= 0x1E88; a++)
            crc.Append($"{_cpu.Memory.Read(a):X2} ");
        output.AppendLine($"crcval: {crc}");

        output.ToString().Should().NotContain("??");
        // Verify msat has varied bytes (not all same)
        var msatBytes = new byte[14];
        for (int i = 0; i < 14; i++)
            msatBytes[i] = _cpu.Memory.Read((ushort)(0x1D7D + i));
        bool allSame = msatBytes.All(b => b == msatBytes[0]);
        allSame.Should().BeFalse($"msat should have varied register values, but all are 0x{msatBytes[0]:X2}");
    }

    /// <summary>
    /// Verify that ld (spat),sp stores the correct SP value.
    /// The spat address is 0x1D8B. After the iut (SBC HL,BC), SP should still be
    /// the value loaded from spbt (0x465E for adc16 base case).
    /// </summary>
    [Fact]
    public void Verify_Spat_Storage()
    {
        LoadZexall();
        var steps = StepUntil(0x1D2A);
        steps.Should().BeGreaterThan(0);

        // Run through the first part of test function until second ld (spat),sp (0x1D46)
        while (_cpu.Regs.PC != 0x1D46 && _cpu.Regs.PC != 0x1D7C)
            _cpu.Step();

        if (_cpu.Regs.PC == 0x1D46)
        {
            // Execute the ld (spat),sp instruction
            _cpu.Step();

            ushort spatVal = (ushort)(_cpu.Memory.Read(0x1D8B) | (_cpu.Memory.Read(0x1D8C) << 8));
            spatVal.Should().Be(0x465E, "spat should contain 0x465E (SP value from spbt for adc16)");
        }
    }

    /// <summary>
    /// After ld sp,spat sets SP=0x1D8B, verify SP is correct before the push sequence.
    /// </summary>
    [Fact]
    public void Verify_SP_After_LoadSpat()
    {
        LoadZexall();
        var steps = StepUntil(0x1D2A);
        steps.Should().BeGreaterThan(0);

        // Run to ld sp,spat (0x1D4A)
        while (_cpu.Regs.PC != 0x1D4A && _cpu.Regs.PC != 0x1D7C)
            _cpu.Step();

        if (_cpu.Regs.PC == 0x1D4A)
        {
            // Execute LD SP,0x1D8B
            _cpu.Step();
            _cpu.Regs.SP.Should().Be(0x1D8B, "SP should be 0x1D8B (spat address) after ld sp,spat");

            // Now dump state before push sequence
            var output = new System.Text.StringBuilder();
            output.AppendLine($"Before pushes: SP=0x{_cpu.Regs.SP:X4}");
            output.AppendLine($"  AF=0x{_cpu.Regs.AF:X4} BC=0x{_cpu.Regs.BC:X4} DE=0x{_cpu.Regs.DE:X4} HL=0x{_cpu.Regs.HL:X4}");
            output.AppendLine($"  IX=0x{_cpu.Regs.IX:X4} IY=0x{_cpu.Regs.IY:X4}");

            // Execute 6 pushes (PUSH AF, PUSH BC, PUSH DE, PUSH HL, PUSH IX, PUSH IY)
            for (int i = 0; i < 6; i++)
            {
                ushort pcBefore = _cpu.Regs.PC;
                byte op = _cpu.Memory.Read(pcBefore);
                string instr = op switch
                {
                    0xF5 => "PUSH AF",
                    0xC5 => "PUSH BC",
                    0xD5 => "PUSH DE",
                    0xE5 => "PUSH HL",
                    0xDD when _cpu.Memory.Read((ushort)(pcBefore + 1)) == 0xE5 => "PUSH IX",
                    0xFD when _cpu.Memory.Read((ushort)(pcBefore + 1)) == 0xE5 => "PUSH IY",
                    _ => $"?? 0x{op:X2}"
                };
                output.AppendLine($"  [{i}] 0x{pcBefore:X4}: {instr} → SP=0x{_cpu.Regs.SP:X4}");
                _cpu.Step();
            }

            // Dump memory written by pushes
            var mem = new System.Text.StringBuilder();
            for (ushort a = 0x1D7F; a <= 0x1D8A; a++)
                mem.Append($"{_cpu.Memory.Read(a):X2} ");
            output.AppendLine($"  Memory 0x1D7F-0x1D8A after pushes: {mem}");

            // Expected values from msbt (machine state before test)
            output.AppendLine("  Expected (from msbt):");
            output.AppendLine("    0x1D7F-80: IY (0x88,0x4F)");
            output.AppendLine("    0x1D81-82: IX (0x2B,0xF2)");
            output.AppendLine("    0x1D83-84: HL (0x39,0xB3)");
            output.AppendLine("    0x1D85-86: DE (0x1F,0x7E)");
            output.AppendLine("    0x1D87-88: BC (0x63,0x15)");
            output.AppendLine("    0x1D89-8A: AF (0xD3,0x89)");

            Console.WriteLine(output);

            // Verify the pushes wrote correct values
            _cpu.Memory.Read((ushort)0x1D7F).Should().Be(0x88, "YL from IY=0x4F88");
            _cpu.Memory.Read((ushort)0x1D80).Should().Be(0x4F, "YH from IY=0x4F88");
            _cpu.Memory.Read((ushort)0x1D89).Should().Be(0xD3, "F from AF");
            _cpu.Memory.Read((ushort)0x1D8A).Should().Be(0x89, "A from AF");
        }
    }

    /// <summary>
    /// Verify the full push+ld sequence produces correct msat.
    /// Run to the CRC loop entry (0x1D6C) and check msat.
    /// </summary>
    [Fact]
    public void Verify_Msat_After_Pushes_Before_CRC()
    {
        LoadZexall();
        var steps = StepUntil(0x1D2A);
        steps.Should().BeGreaterThan(0);

        // Run to the ld (msat),hl instruction (0x1D5E: 2A 03 01 = LD HL,(0x0103))
        while (_cpu.Regs.PC != 0x1D5E && _cpu.Regs.PC != 0x1D7C)
            _cpu.Step();

        if (_cpu.Regs.PC == 0x1D5E)
        {
            // Execute LD HL,(0x0103) - loads memop from msbt
            _cpu.Step();
            // Execute LD (0x1D7D),HL - stores memop to msat
            _cpu.Step();

            // Now execute AND 0xD3 and LD (0x1D89),A (flag masking)
            _cpu.Step(); // AND 0xD3
            _cpu.Step(); // LD (0x1D89),A

            // Dump full msat
            var output = new System.Text.StringBuilder();
            output.AppendLine("=== MSAT AFTER PUSHES + MEMOP COPY + FLAG MASKING ===");
            var msat = new System.Text.StringBuilder();
            for (ushort a = 0x1D7D; a <= 0x1D8C; a++)
                msat.Append($"{_cpu.Memory.Read(a):X2} ");
            output.AppendLine($"msat: {msat}");

            // Expected from adc16 base case:
            // memop = 2C 83, IY=4F88, IX=F22B, HL=B339, DE=7E1F, BC=1563, AF=D389
            // After push order (AF,BC,DE,HL,IX,IY at decreasing addresses from 0x1D8B):
            // 0x1D7F: IY_L=88, 0x1D80: IY_H=4F
            // 0x1D81: IX_L=2B, 0x1D82: IX_H=F2
            // 0x1D83: HL_L=39, 0x1D84: HL_H=B3
            // 0x1D85: DE_L=1F, 0x1D86: DE_H=7E
            // 0x1D87: BC_L=63, 0x1D88: BC_H=15
            // 0x1D89: F=D3(&D3)=D3, 0x1D8A: A=89
            output.AppendLine("Expected: 2C 83 88 4F 2B F2 39 B3 1F 7E 63 15 D3 89");
            Console.WriteLine(output);

            // Verify
            _cpu.Memory.Read((ushort)0x1D7D).Should().Be(0x2C, "memop_lo");
            _cpu.Memory.Read((ushort)0x1D7E).Should().Be(0x83, "memop_hi");
            _cpu.Memory.Read((ushort)0x1D7F).Should().Be(0x88, "IY_L");
            _cpu.Memory.Read((ushort)0x1D80).Should().Be(0x4F, "IY_H");
            _cpu.Memory.Read((ushort)0x1D89).Should().Be(0xD3, "F masked");
            _cpu.Memory.Read((ushort)0x1D8A).Should().Be(0x89, "A");
        }
    }
}
