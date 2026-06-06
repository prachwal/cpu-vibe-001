using FluentAssertions;
using Xunit;
using Z80.Core;

namespace Z80.Tests.OpcodeTests;

public class CounterCorruptionTest
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(CounterCorruptionTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename}");
    }

    private void LoadCom(string rom)
    {
        _cpu.Reset();
        var binary = File.ReadAllBytes(FindRom(rom));
        _cpu.Memory.Load(0x0100, binary);
        _cpu.Memory.Write(0x0005, 0xC9);
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
    }

    private int StepUntil(ushort target, int max = 50_000_000)
    {
        for (int i = 0; i < max; i++)
        {
            if (_cpu.Regs.PC == 0x0005) { _cpu.Regs.PC = _cpu.StackPop(); continue; }
            if (_cpu.Regs.PC == target) return i;
            _cpu.Step();
        }
        return -1;
    }

    private string Hex(ushort addr, int len)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < len; i++)
            sb.Append($"{_cpu.Memory.Read((ushort)(addr + i)):X2} ");
        return sb.ToString();
    }

    private string InstrAt(ushort pc)
    {
        byte op = _cpu.Memory.Read(pc);
        byte op2 = _cpu.Memory.Read((ushort)(pc + 1));
        byte op3 = _cpu.Memory.Read((ushort)(pc + 2));
        byte op4 = _cpu.Memory.Read((ushort)(pc + 3));
        return op switch
        {
            0xF5 => "PUSH AF", 0xC5 => "PUSH BC", 0xD5 => "PUSH DE", 0xE5 => "PUSH HL",
            0xF1 => "POP AF", 0xC1 => "POP BC", 0xD1 => "POP DE", 0xE1 => "POP HL",
            0x31 => $"LD SP,0x{op3:X2}{op2:X2}",
            0xFB => "EI", 0xF3 => "DI", 0xC9 => "RET", 0x00 => "NOP",
            0xDD when op2 == 0xE1 => "POP IX", 0xDD when op2 == 0xE5 => "PUSH IX",
            0xFD when op2 == 0xE1 => "POP IY", 0xFD when op2 == 0xE5 => "PUSH IY",
            0xED when op2 == 0x7B => $"LD SP,(0x{op4:X2}{op3:X2})",
            0xED when op2 == 0x73 => $"LD (0x{op4:X2}{op3:X2}),SP",
            0xED when op2 == 0x42 => "SBC HL,BC",
            0xED when op2 == 0x4A => "ADC HL,BC",
            0x2A => $"LD HL,(0x{op3:X2}{op2:X2})",
            0x22 => $"LD (0x{op3:X2}{op2:X2}),HL",
            0x3A => $"LD A,(0x{op3:X2}{op2:X2})",
            0x32 => $"LD (0x{op3:X2}{op2:X2}),A",
            0xE6 => $"AND 0x{op2:X2}", 0xFE => $"CP 0x{op2:X2}",
            0x06 => $"LD B,0x{op2:X2}", 0x0E => $"LD C,0x{op2:X2}",
            0x16 => $"LD D,0x{op2:X2}", 0x1E => $"LD E,0x{op2:X2}",
            0x26 => $"LD H,0x{op2:X2}", 0x2E => $"LD L,0x{op2:X2}",
            0x21 => $"LD HL,0x{op3:X2}{op2:X2}",
            0x11 => $"LD DE,0x{op3:X2}{op2:X2}",
            0x01 => $"LD BC,0x{op3:X2}{op2:X2}",
            0x5E => "LD E,(HL)", 0x56 => "LD D,(HL)",
            0x77 => "LD (HL),A", 0x7E => "LD A,(HL)",
            0x19 => "ADD HL,DE", 0x09 => "ADD HL,BC",
            0x29 => "ADD HL,HL",
            0xCB => op2 switch
            {
                0x37 => "SWAP A", _ => $"CB {op2:X2}"
            },
            0xB8 => "CP B", 0xB9 => "CP C",
            0xC2 => $"JP NZ,0x{op3:X2}{op2:X2}",
            0xCA => $"JP Z,0x{op3:X2}{op2:X2}",
            0xC3 => $"JP 0x{op3:X2}{op2:X2}",
            0xCD => $"CALL 0x{op3:X2}{op2:X2}",
            0xC4 => $"CALL NZ,0x{op3:X2}{op2:X2}",
            0xCC => $"CALL Z,0x{op3:X2}{op2:X2}",
            0xD8 => "RET C", 0xD0 => "RET NC",
            0xC8 => "RET Z", 0xC0 => "RET NZ",
            0xEB => "EX DE,HL",
            0xA8 => "XOR B", 0xA9 => "XOR C", 0xAA => "XOR D", 0xAB => "XOR E",
            0xAC => "XOR H", 0xAD => "XOR L", 0xAE => "XOR (HL)", 0xAF => "XOR A",
            0xB0 => "OR B", 0xB1 => "OR C", 0xB2 => "OR D", 0xB3 => "OR E",
            0xB4 => "OR H", 0xB5 => "OR L", 0xB6 => "OR (HL)", 0xB7 => "OR A",
            0xA0 => "AND B", 0xA1 => "AND C", 0xA2 => "AND D", 0xA3 => "AND E",
            0xA4 => "AND H", 0xA5 => "AND L", 0xA6 => "AND (HL)", 0xA7 => "AND A",
            0x47 => "LD B,A", 0x4F => "LD C,A",
            0x57 => "LD D,A", 0x5F => "LD E,A",
            0x67 => "LD H,A", 0x6F => "LD L,A",
            0x7F => "LD A,A",
            0x04 => "INC B", 0x0C => "INC C",
            0x14 => "INC D", 0x1C => "INC E",
            0x24 => "INC H", 0x2C => "INC L",
            0x34 => "INC (HL)", 0x3C => "INC A",
            0x05 => "DEC B", 0x0D => "DEC C",
            0x15 => "DEC D", 0x1D => "DEC E",
            0x25 => "DEC H", 0x2D => "DEC L",
            0x35 => "DEC (HL)", 0x3D => "DEC A",
            0x1A => "LD A,(DE)", 0x0A => "LD A,(BC)",
            0x12 => "LD (DE),A",
            0x13 => "INC DE", 0x1B => "DEC DE",
            0x23 => "INC HL", 0x2B => "DEC HL",
            0x33 => "INC SP", 0x3B => "DEC SP",
            0x27 => "DAA",
            0x2F => "CPL",
            0x37 => "SCF",
            0x3F => "CCF",
            0x76 => "HALT",
            _ => $"?? {op:X2}"
        };
    }

    /// <summary>
    /// Single-step from test RET (0x1D7C) and log every instruction until
    /// counter terminal[0] (0x1CEE) changes from 0x00 to non-zero.
    /// </summary>
    [Fact]
    public void Find_Exact_Corruption_Instruction()
    {
        LoadCom("zexall.com");
        StepUntil(0x1D2A).Should().BeGreaterThan(0);
        StepUntil(0x1D7C).Should().BeGreaterThan(0);

        // Verify counter is clean at test RET
        _cpu.Memory.Read(0x1CEE).Should().Be(0x00, "counter terminal should be clean at test RET");

        var log = new System.Text.StringBuilder();
        log.AppendLine("=== Step-by-step from test RET (0x1D7C) ===");

        for (int i = 0; i < 100; i++)
        {
            ushort pc = _cpu.Regs.PC;
            if (pc == 0x0005) { _cpu.Regs.PC = _cpu.StackPop(); continue; }

            byte term0 = _cpu.Memory.Read(0x1CEE);
            string instr = InstrAt(pc);

            log.AppendLine($"  {i,3}: 0x{pc:X4} {instr,-28} SP=0x{_cpu.Regs.SP:X4} BC={_cpu.Regs.BC:X4} HL={_cpu.Regs.HL:X4} DE={_cpu.Regs.DE:X4} term0={term0:X2}");

            _cpu.Step();

            byte newTerm0 = _cpu.Memory.Read(0x1CEE);
            if (term0 != newTerm0)
            {
                log.AppendLine($"  *** CORRUPTION! term[0] changed {term0:X2} → {newTerm0:X2} at 0x{pc:X4} after this instruction");
                break;
            }
        }

        Console.WriteLine(log);
        // Don't fail — we want to see the output
    }

    /// <summary>
    /// Compare: same trace for zexdoc (known working).
    /// </summary>
    [Fact]
    public void Find_Exact_Corruption_Instruction_zexdoc()
    {
        LoadCom("zexdoc.com");
        StepUntil(0x1D2A).Should().BeGreaterThan(0);
        StepUntil(0x1D7C).Should().BeGreaterThan(0);

        var log = new System.Text.StringBuilder();
        log.AppendLine("=== Step-by-step from test RET (0x1D7C) — zexdoc ===");

        for (int i = 0; i < 100; i++)
        {
            ushort pc = _cpu.Regs.PC;
            if (pc == 0x0005) { _cpu.Regs.PC = _cpu.StackPop(); continue; }

            byte term0 = _cpu.Memory.Read(0x1CEE);
            string instr = InstrAt(pc);

            log.AppendLine($"  {i,3}: 0x{pc:X4} {instr,-28} SP=0x{_cpu.Regs.SP:X4} BC={_cpu.Regs.BC:X4} HL={_cpu.Regs.HL:X4} DE={_cpu.Regs.DE:X4} term0={term0:X2}");

            _cpu.Step();

            byte newTerm0 = _cpu.Memory.Read(0x1CEE);
            if (term0 != newTerm0)
            {
                log.AppendLine($"  *** term[0] changed {term0:X2} → {newTerm0:X2} at 0x{pc:X4}");
                break;
            }
        }

        Console.WriteLine(log);
    }
}
