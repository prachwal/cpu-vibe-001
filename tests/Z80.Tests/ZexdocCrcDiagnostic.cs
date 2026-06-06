using System.Text;
using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class ZexdocCrcDiagnostic
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(ZexdocCrcDiagnostic).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename}");
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

    private void HandleBdosCall()
    {
        byte c = _cpu.Regs.C;
        switch (c)
        {
            case 1: _cpu.Regs.A = 0x0D; break;
            case 2: break;
            case 9:
                ushort addr = _cpu.Regs.DE;
                while (_cpu.Memory.Read(addr) != '$') addr++;
                break;
        }
        _cpu.Regs.PC = _cpu.StackPop();
    }

    [Fact(Skip = "Diagnostic dump; run manually when investigating ZEXDOC CRC failures.")]
    public void DumpCrcAfterFirstTestGroup()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        LoadCom(binary);

        var sb = new StringBuilder();
        long iterations = 0;

        // Run until the first time we hit cmpcrc (0x1E32) after all test cases
        // Actually, let's track: how many times does the test function get called?
        // And what's crcval after the first test group's CRC loop?

        // Strategy: run until we return from the test function after the last test case
        // of the first group (adc16). The test function returns at 0x1D7C.
        // We need to detect when we're about to compare CRCs.

        // Better strategy: run until cmpcrc is called (0x1E32)
        bool reachedTestFunc = false;
        int testFuncCount = 0;

        while (iterations < 50_000_000)
        {
            ushort pc = _cpu.Regs.PC;

            if (pc == 0x0005) { HandleBdosCall(); iterations++; continue; }
            if (pc == 0x0000) break;

            // Detect entry to test function (0x1D2A) - count iterations
            if (pc == 0x1D2A)
            {
                testFuncCount++;
                reachedTestFunc = true;
            }

            // After the CRC loop in the test function, it returns at 0x1D7C.
            // But we want to catch when cmpcrc is called.
            // cmpcrc is at 0x1E32, called from 0x1B4C.

            // Detect return from test function followed by the count/shift/cmpcrc logic
            // The tlp loop calls test, then count, then shift, then checks if done.
            // When done, it calls cmpcrc at 0x1B4C.

            // Let me detect when we reach the cmpcrc area (0x1E32)
            if (pc == 0x1E32 && testFuncCount > 0)
            {
                // Dump everything
                sb.AppendLine($"\n=== CRC comparison reached after {testFuncCount} test function calls ===");

                // Dump crcval
                sb.Append("crcval (0x1E85): ");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1E85 + i)):X2} ");
                sb.AppendLine();

                // Dump expected CRC (HL should point to it)
                sb.Append("expected (HL=0x{_cpu.Regs.HL:X4}): ");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(_cpu.Regs.HL + i)):X2} ");
                sb.AppendLine();

                // Dump msat (last test case state)
                sb.Append("msat (0x1D7D): ");
                for (int i = 0; i < 16; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1D7D + i)):X2} ");
                sb.AppendLine();

                // Dump iut
                sb.Append("iut (0x1D42): ");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1D42 + i)):X2} ");
                sb.AppendLine();

                // Dump msbt
                sb.Append("msbt (0x0103): ");
                for (int i = 0; i < 16; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x0103 + i)):X2} ");
                sb.AppendLine();

                // Dump CRC table first entry
                sb.Append("crctab[0] (0x1E89): ");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1E89 + i)):X2} ");
                sb.AppendLine();

                // Dump counter and shifter
                sb.Append("counter (0x1CDA): ");
                for (int i = 0; i < 20; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1CDA + i)):X2} ");
                sb.AppendLine();
                sb.Append("counter_term (0x1CEE): ");
                for (int i = 0; i < 20; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1CEE + i)):X2} ");
                sb.AppendLine();

                // CRC table entry for crcval[3] XOR first msat byte
                byte firstMsatByte = _cpu.Memory.Read(0x1D7D);
                byte crcByte3 = _cpu.Memory.Read(0x1E88);
                byte xorResult = (byte)(firstMsatByte ^ crcByte3);
                sb.AppendLine($"\nCRC analysis:");
                sb.AppendLine($"  First msat byte: 0x{firstMsatByte:X2}");
                sb.AppendLine($"  crcval[3] (initial FFFFFFFF): 0x{crcByte3:X2}");
                sb.AppendLine($"  XOR result: 0x{xorResult:X2}");
                sb.AppendLine($"  Table index: {xorResult} * 4 = {xorResult * 4}");
                ushort tblIdx = (ushort)(xorResult * 4);
                sb.Append($"  crctab[{xorResult}] (0x{0x1E89 + tblIdx:X4}): ");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1E89 + tblIdx + i)):X2} ");
                sb.AppendLine();

                // Now compute what CRC should be after processing all msat bytes
                // We need to know ALL the msat values that were fed to the CRC
                // But we only have the last one. Let me at least compute what
                // the CRC of the last msat should be (starting from FFFFFFFF)
                sb.AppendLine($"\n  Computing CRC of last msat from FFFFFFFF:");
                byte[] crc = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 16; i++)
                {
                    byte b = _cpu.Memory.Read((ushort)(0x1D7D + i));
                    crc = ComputeCrc32Zexdoc(crc, b);
                }
                sb.AppendLine($"  Expected CRC: {crc[0]:X2} {crc[1]:X2} {crc[2]:X2} {crc[3]:X2}");
                sb.Append($"  Actual CRC:   ");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1E85 + i)):X2} ");
                sb.AppendLine();

                break;
            }

            _cpu.Step();
            iterations++;
        }

        sb.AppendLine($"\nTotal iterations: {iterations}");
        Assert.Fail(sb.ToString());
    }

    [Fact(Skip = "Diagnostic dump; run manually when investigating ZEXDOC CRC failures.")]
    public void TraceFirstNTestCases()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        LoadCom(binary);

        var sb = new StringBuilder();
        long iterations = 0;

        // Track CRC after each test case for the first test group (adc16)
        // The test function enters at 0x1D2A, the CRC loop is at 0x1D6F-0x1D75
        // After the loop, at 0x1D78, we can read crcval

        int testCount = 0;
        bool inTestFunc = false;

        while (iterations < 50_000_000)
        {
            ushort pc = _cpu.Regs.PC;

            if (pc == 0x0005) { HandleBdosCall(); iterations++; continue; }
            if (pc == 0x0000) break;

            // We want to capture crcval after each test function execution
            // The test function returns at 0x1D7C (RET)
            if (pc == 0x1D7C && testCount < 5)
            {
                testCount++;
                sb.Append($"Test {testCount}: crcval=");
                for (int i = 0; i < 4; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1E85 + i)):X2} ");
                sb.Append("  msat=");
                for (int i = 0; i < 16; i++)
                    sb.Append($"{_cpu.Memory.Read((ushort)(0x1D7D + i)):X2} ");
                sb.AppendLine();
            }

            _cpu.Step();
            iterations++;
        }

        Assert.Fail(sb.ToString());
    }

    private static byte[] ComputeCrc32Zexdoc(byte[] crc, byte input)
    {
        byte[] crctab = GetCrcTable();
        byte xorResult = (byte)(input ^ crc[3]);
        int index = xorResult * 4;

        byte[] newCrc = new byte[4];
        byte acc = 0;
        for (int i = 0; i < 4; i++)
        {
            newCrc[i] = (byte)(crctab[index + i] ^ acc);
            acc = crc[i];
        }
        return newCrc;
    }

    private static byte[]? _crcTable;
    private static byte[] GetCrcTable()
    {
        if (_crcTable != null) return _crcTable;
        byte[] data = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "roms", "zexdoc.com"));
        int tableOffset = 0x1E89 - 0x100;
        _crcTable = new byte[1024];
        Array.Copy(data, tableOffset, _crcTable, 0, 1024);
        return _crcTable;
    }
}
