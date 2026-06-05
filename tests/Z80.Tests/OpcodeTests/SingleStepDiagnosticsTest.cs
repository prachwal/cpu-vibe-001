using FluentAssertions;
using Xunit;
using Z80.Core;

namespace Z80.Tests.OpcodeTests;

public class SingleStepDiagnosticsTest
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(SingleStepDiagnosticsTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename}");
    }

    private void LoadZexall()
    {
        _cpu.Reset();
        var binary = File.ReadAllBytes(FindRom("zexall.com"));
        _cpu.Memory.Load(0x0100, binary);
        _cpu.Memory.Write(0x0005, 0xC9);
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
    }

    private int StepUntil(ushort target, int max = 50_000_000)
    {
        for (int i = 0; i < max; i++)
        {
            if (_cpu.Regs.PC == 0x0005)
            {
                _cpu.Regs.PC = _cpu.StackPop();
                continue;
            }
            if (_cpu.Regs.PC == target) return i;
            _cpu.Step();
        }
        return -1;
    }

    private string MemDump(ushort start, int count)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
            sb.Append($"{_cpu.Memory.Read((ushort)(start + i)):X2} ");
        return sb.ToString();
    }

    [Fact]
    public void Trace_0x1D2A_to_0x1D7C()
    {
        LoadZexall();
        StepUntil(0x1D2A).Should().BeGreaterThan(0);

        var log = new System.Text.StringBuilder();
        int instrCount = 0;

        while (_cpu.Regs.PC != 0x1D7C && instrCount < 200)
        {
            ushort pc = _cpu.Regs.PC;
            byte op = _cpu.Memory.Read(pc);
            byte op2 = _cpu.Memory.Read((ushort)(pc + 1));
            byte op3 = _cpu.Memory.Read((ushort)(pc + 2));
            byte op4 = _cpu.Memory.Read((ushort)(pc + 3));

            string instr = op switch
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
                0x2A => $"LD HL,(0x{op3:X2}{op2:X2})",
                0x22 => $"LD (0x{op3:X2}{op2:X2}),HL",
                0x3A => $"LD A,(0x{op3:X2}{op2:X2})",
                0x32 => $"LD (0x{op3:X2}{op2:X2}),A",
                0xE6 => $"AND 0x{op2:X2}",
                0x06 => $"LD B,0x{op2:X2}",
                0x21 => $"LD HL,0x{op3:X2}{op2:X2}",
                0x5E => "LD E,(HL)", 0x16 => $"LD D,0x{op2:X2}", 0x19 => "ADD HL,DE",
                _ => $"?? 0x{op:X2}"
            };

            log.AppendLine($"  {instrCount,3}: 0x{pc:X4} {instr,-24} SP=0x{_cpu.Regs.SP:X4} AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} DE={_cpu.Regs.DE:X4} HL={_cpu.Regs.HL:X4} IX={_cpu.Regs.IX:X4} IY={_cpu.Regs.IY:X4}");

            if (pc == 0x1D4A || pc == 0x1D55 || pc == 0x1D5E || pc == 0x1D6C)
            {
                log.AppendLine($"         MEM 0x1D7D: {MemDump(0x1D7D, 16)}");
                log.AppendLine($"         spsav(0x1D8D): {MemDump(0x1D8D, 2)}");
            }

            _cpu.Step();
            instrCount++;
        }

        log.AppendLine();
        log.AppendLine($"=== FINAL: PC=0x{_cpu.Regs.PC:X4} after {instrCount} instructions ===");
        log.AppendLine($"msat: {MemDump(0x1D7D, 16)}");

        File.WriteAllText("/tmp/zexall_trace.txt", log.ToString());
        Console.WriteLine(log);

        var msatBytes = new byte[14];
        for (int i = 0; i < 14; i++)
            msatBytes[i] = _cpu.Memory.Read((ushort)(0x1D7D + i));
        bool allSame = msatBytes.All(b => b == msatBytes[0]);
        allSame.Should().BeFalse($"msat should have varied bytes, got all 0x{msatBytes[0]:X2}");
    }

    [Fact]
    public void Dump_Counter_After_InitMask()
    {
        LoadZexall();
        StepUntil(0x1D2A).Should().BeGreaterThan(0);

        var log = new System.Text.StringBuilder();
        log.AppendLine("=== COUNTER/SHIFTER AREA AFTER FIRST TEST SETUP ===");
        log.AppendLine($"counter (0x1CDA): {MemDump(0x1CDA, 20)}");
        log.AppendLine($"counter_term (0x1CEE): {MemDump(0x1CEE, 20)}");
        log.AppendLine($"shifter (0x1D02): {MemDump(0x1D02, 20)}");
        log.AppendLine($"shifter_term (0x1D16): {MemDump(0x1D16, 20)}");
        log.AppendLine($"msbt (0x0103): {MemDump(0x0103, 16)}");
        log.AppendLine($"spbt(0x0111): {MemDump(0x0111, 2)}");

        File.WriteAllText("/tmp/zexall_counter.txt", log.ToString());
        Console.WriteLine(log);
    }
}
