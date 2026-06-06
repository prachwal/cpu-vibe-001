using FluentAssertions;
using Xunit;
using Z80.Core;
using Z80.Instructions.DdFd;

namespace Z80.Tests.OpcodeTests;

public class NopDdFdDiagnostic
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(NopDdFdDiagnostic).Assembly.Location)!;
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

    [Fact]
    public void Track_NopDdFd_Calls_During_Zexall()
    {
        LoadCom("zexall.com");
        var log = new System.Text.StringBuilder();
        long iterations = 0;

        NopDdFd.ResetCount();

        while (iterations < 500_000_000)
        {
            if (_cpu.Regs.PC == 0x0005)
            {
                _cpu.Regs.PC = _cpu.StackPop();
                iterations++;
                continue;
            }
            if (_cpu.Regs.PC == 0x0000) break;

            _cpu.Step();
            iterations++;

            if (iterations % 10_000_000 == 0)
            {
                byte cnt0 = _cpu.Memory.Read(0x1CDA);
                byte cnt1 = _cpu.Memory.Read(0x1CDB);
                byte term0 = _cpu.Memory.Read(0x1CEE);
                byte term1 = _cpu.Memory.Read(0x1CEF);
                byte shifter0 = _cpu.Memory.Read(0x1D02);
                log.AppendLine($"  {iterations/1_000_000}M: PC=0x{_cpu.Regs.PC:X4} IX=0x{_cpu.Regs.IX:X4} counter=[{cnt0:X2} {cnt1:X2}] term=[{term0:X2} {term1:X2}] shifter={shifter0:X2} NopDdFd={NopDdFd.CallCount}");
            }

            if (NopDdFd.CallCount > 1000)
            {
                log.AppendLine($"  *** NopDdFd called {NopDdFd.CallCount} times!");
                log.AppendLine($"  Last subopcode: 0x{NopDdFd.LastSubOpcode:X2}");
                log.AppendLine($"  Last PC: 0x{NopDdFd.LastPC:X4}");
                break;
            }
        }

        log.AppendLine($"  Final: {iterations} iterations, NopDdFd={NopDdFd.CallCount}");
        Console.WriteLine(log);
    }
}
