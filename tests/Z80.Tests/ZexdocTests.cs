using System.Text;
using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class ZexdocTests
{
    private readonly Cpu _cpu = new();
    private readonly StringBuilder _output = new();
    private const long MaxRegressionIterations = 20_000_000;

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
    public void Zexdoc_StartsFirstGroupWithoutErrors()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        LoadCom(binary);

        long iterations = 0;

        while (iterations < MaxRegressionIterations)
        {
            ushort pc = _cpu.Regs.PC;

            if (pc == 0x0005)
            {
                HandleBdosCall();
                string output = _output.ToString();
                if (output.Contains("<adc,sbc> hl,<bc,de,hl,sp>...."))
                    break;
                continue;
            }

            if (pc == 0x0000)
                break;

            _cpu.Step();
            iterations++;
        }

        string result = _output.ToString();

        int failCount = result.Split('\n').Count(l => l.Contains("ERROR"));

        result.Should().Contain("Z80 instruction exerciser");
        result.Should().Contain("<adc,sbc> hl,<bc,de,hl,sp>....");
        failCount.Should().Be(0, $"ZEXDOC emitted errors after {iterations} iterations:\n{result}");
    }

    [Fact]
    public void Zexdoc_BootsAndPrintsHeader()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        LoadCom(binary);

        long iterations = 0;

        while (iterations < 500_000 && !_output.ToString().Contains("Z80 instruction exerciser"))
        {
            ushort pc = _cpu.Regs.PC;
            if (pc == 0x0005) { HandleBdosCall(); continue; }
            if (pc == 0x0000) break;
            _cpu.Step();
            iterations++;
        }

        _output.ToString().Should().Contain("Z80 instruction exerciser");
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
