using System.Text;
using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class ZexdocTests
{
    private readonly Cpu _cpu = new();
    private readonly StringBuilder _output = new();
    private int _maxCycles = 500_000_000;

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(ZexdocTests).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename} (searched from {assemblyDir})");
    }

    private void LoadCom(byte[] binary)
    {
        _cpu.Reset();
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Load(0x0100, binary);

        _cpu.Memory.Write(0x0005, 0xC9);
        _cpu.Memory.Write(0x0006, 0x00);
        _cpu.Memory.Write(0x0007, 0xF0);
    }

    [Fact]
    public void Zexdoc_AllTestsPass()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        LoadCom(binary);

        bool done = false;
        long iterations = 0;

        while (!done && iterations < _maxCycles)
        {
            ushort pc = _cpu.Regs.PC;

            if (pc == 0x0005)
            {
                HandleBdosCall();
                continue;
            }

            if (pc == 0x0000)
            {
                done = true;
                break;
            }

            _cpu.Step();
            iterations++;
        }

        string result = _output.ToString();

        int passCount = result.Split('\n').Count(l => l.Contains("OK") && !l.Contains("LOOK"));
        int failCount = result.Split('\n').Count(l => l.Contains("ERROR"));
        
        Assert.Fail($"Iterations: {iterations}, pass={passCount}, fail={failCount}\n{result}");
        result.Should().Contain("Z80 instruction exerciser");
        result.Should().Contain("Tests complete");

        bool hasError = result.Contains("ERROR");
        hasError.Should().BeFalse($"ZEXDOC found errors:\n{result}");
    }

    [Fact]
    public void Zexdoc_MemoryDiagnostic()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        LoadCom(binary);

        bool done = false;
        long iterations = 0;

        while (!done && iterations < 500_000)
        {
            ushort pc = _cpu.Regs.PC;
            if (pc == 0x0005) { HandleBdosCall(); continue; }
            if (pc == 0x0000) { done = true; break; }
            _cpu.Step();
            iterations++;
        }

        Assert.Fail($"Iterations: {iterations}, done={done}");
    }

    private void HandleBdosCall()
    {
        byte c = _cpu.Regs.C;

        switch (c)
        {
            case 1:
                _cpu.Regs.A = 0x0D;
                break;

            case 2:
                char ch = (char)_cpu.Regs.E;
                _output.Append(ch);
                break;

            case 9:
                ushort addr = _cpu.Regs.DE;
                var sb = new StringBuilder();
                while (true)
                {
                    byte b = _cpu.Memory.Read(addr);
                    if (b == '$') break;
                    sb.Append((char)b);
                    addr++;
                }
                _output.Append(sb);
                break;

            default:
                break;
        }

        _cpu.Regs.PC = _cpu.StackPop();
    }
}
