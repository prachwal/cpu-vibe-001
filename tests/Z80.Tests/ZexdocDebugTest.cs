using System.Text;
using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class ZexdocDebugTest
{
    private readonly Cpu _cpu = new();

    private static string FindRom(string filename)
    {
        string assemblyDir = Path.GetDirectoryName(typeof(ZexdocDebugTest).Assembly.Location)!;
        string path = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "roms", filename));
        if (File.Exists(path)) return path;
        throw new FileNotFoundException($"ROM not found: {filename} (searched from {assemblyDir})");
    }

    [Fact]
    public void SelfModifyingCode_InlineExec()
    {
        _cpu.Reset();
        _cpu.Regs.SP = 0xF000;
        ushort iutAddr = 0x0110;

        _cpu.Memory.Write(0x0100, 0x3E); _cpu.Memory.Write(0x0101, 0xC6);
        _cpu.Memory.Write(0x0102, 0x32);
        _cpu.Memory.Write(0x0103, (byte)(iutAddr & 0xFF));
        _cpu.Memory.Write(0x0104, (byte)(iutAddr >> 8));
        _cpu.Memory.Write(0x0105, 0x3E); _cpu.Memory.Write(0x0106, 0x42);
        _cpu.Memory.Write(0x0107, 0x32);
        _cpu.Memory.Write(0x0108, (byte)((iutAddr + 1) & 0xFF));
        _cpu.Memory.Write(0x0109, (byte)((iutAddr + 1) >> 8));
        _cpu.Memory.Write(iutAddr, 0x00); _cpu.Memory.Write((ushort)(iutAddr + 1), 0x00);
        _cpu.Memory.Write((ushort)(iutAddr + 2), 0x76);
        _cpu.Memory.Write(0x010A, 0x3E); _cpu.Memory.Write(0x010B, 0x00);
        _cpu.Memory.Write(0x010C, 0xC3);
        _cpu.Memory.Write(0x010D, (byte)(iutAddr & 0xFF));
        _cpu.Memory.Write(0x010E, (byte)(iutAddr >> 8));
        _cpu.Regs.PC = 0x0100;
        for (int i = 0; i < 20; i++) _cpu.Step();
        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LDIR_BasicTest()
    {
        _cpu.Reset();
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x21); _cpu.Memory.Write(0x0101, 0x00); _cpu.Memory.Write(0x0102, 0x02);
        _cpu.Memory.Write(0x0103, 0x11); _cpu.Memory.Write(0x0104, 0x10); _cpu.Memory.Write(0x0105, 0x02);
        _cpu.Memory.Write(0x0106, 0x01); _cpu.Memory.Write(0x0107, 0x04); _cpu.Memory.Write(0x0108, 0x00);
        _cpu.Memory.Write(0x0109, 0xED); _cpu.Memory.Write(0x010A, 0xB0);
        _cpu.Memory.Write(0x010B, 0x76);
        _cpu.Memory.Write(0x0200, 0xAA); _cpu.Memory.Write(0x0201, 0xBB);
        _cpu.Memory.Write(0x0202, 0xCC); _cpu.Memory.Write(0x0203, 0xDD);
        for (int i = 0; i < 50; i++) _cpu.Step();
        _cpu.Memory.Read(0x0210).Should().Be(0xAA);
        _cpu.Memory.Read(0x0211).Should().Be(0xBB);
        _cpu.Memory.Read(0x0212).Should().Be(0xCC);
        _cpu.Memory.Read(0x0213).Should().Be(0xDD);
    }

    [Fact(Skip = "Diagnostic dump; run manually when investigating ZEXDOC CRC failures.")]
    public void Zexdoc_DiagnoseCRC()
    {
        byte[] binary = File.ReadAllBytes(FindRom("zexdoc.com"));
        _cpu.Reset();
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Load(0x0100, binary);
        _cpu.Memory.Write(0x0005, 0xC9);
        _cpu.Memory.Write(0x0006, 0x00);
        _cpu.Memory.Write(0x0007, 0xF0);

        ushort iutAddr = 0x1D42;
        ushort msatAddr = 0x1D7D;
        ushort spatAddr = 0x1D8B;
        ushort msbtAddr = 0x0103;
        ushort spbtAddr = 0x0111;
        ushort crcvalAddr = 0; // need to find

        long iterations = 0;
        bool foundFirstError = false;
        var sb = new StringBuilder();

        // Run until we find the first call to BDOS C=9 (print string)
        // then check memory state right after the test function returns

        while (iterations < 5_000_000)
        {
            ushort pc = _cpu.Regs.PC;

            if (pc == 0x0005)
            {
                byte c = _cpu.Regs.C;
                if (c == 9 && !foundFirstError)
                {
                    ushort addr = _cpu.Regs.DE;
                    var nameSb = new StringBuilder();
                    while (true)
                    {
                        byte b = _cpu.Memory.Read(addr);
                        if (b == '$') break;
                        nameSb.Append((char)b);
                        addr++;
                    }
                    string name = nameSb.ToString();
                    if (name.Contains("ERROR"))
                    {
                        foundFirstError = true;
                        sb.AppendLine($"\n=== First ERROR: '{name.TrimEnd('.')}' ===");

                        // Dump msbt (machine state before test)
                        sb.Append("msbt (0x0103): ");
                        for (int i = 0; i < 16; i++)
                            sb.Append($"{_cpu.Memory.Read((ushort)(msbtAddr + i)):X2} ");
                        sb.AppendLine();

                        // Dump iut
                        sb.Append("iut (0x1D42): ");
                        for (int i = 0; i < 4; i++)
                            sb.Append($"{_cpu.Memory.Read((ushort)(iutAddr + i)):X2} ");
                        sb.AppendLine();

                        // Dump msat (machine state after test)
                        sb.Append("msat (0x1D7D): ");
                        for (int i = 0; i < 16; i++)
                            sb.Append($"{_cpu.Memory.Read((ushort)(msatAddr + i)):X2} ");
                        sb.AppendLine();

                        // Dump spat
                        sb.Append("spat (0x1D8B): ");
                        for (int i = 0; i < 2; i++)
                            sb.Append($"{_cpu.Memory.Read((ushort)(spatAddr + i)):X2} ");
                        sb.AppendLine();

                        // Search for crcval (should be 4 bytes of FF after init)
                        sb.AppendLine("\nSearching for crcval (FFFFFFFF pattern):");
                        for (ushort a = 0x2200; a < 0x2400; a++)
                        {
                            if (_cpu.Memory.Read(a) == 0xFF &&
                                _cpu.Memory.Read((ushort)(a + 1)) == 0xFF &&
                                _cpu.Memory.Read((ushort)(a + 2)) == 0xFF &&
                                _cpu.Memory.Read((ushort)(a + 3)) == 0xFF)
                            {
                                sb.AppendLine($"  Found FFFFFFFF at 0x{a:X4}");
                            }
                        }

                        // Also look for the CRC value that was computed
                        // The crcval should be near the CRC table
                        // CRC table is at 0x1E89, 1024 bytes = ends at 0x2289
                        sb.AppendLine($"\nMemory 0x2280-0x2290:");
                        for (ushort a = 0x2280; a < 0x2290; a++)
                            sb.Append($"{_cpu.Memory.Read(a):X2} ");
                        sb.AppendLine();

                        // Check the flgsat address
                        // flgsat = spat - 2 = 0x1D8B - 2 = 0x1D89
                        ushort flgsatAddr = (ushort)(spatAddr - 2);
                        sb.AppendLine($"\nflgsat (0x{flgsatAddr:X4}): {_cpu.Memory.Read(flgsatAddr):X2}");

                        // Verify: push order is AF,BC,DE,HL,IX,IY starting from spat
                        // spat is at msat + 14 = 0x1D7D + 14 = 0x1D8B
                        // Push AF: SP = spat-2, writes F to spat-2, A to spat-1
                        // Push BC: SP = spat-4, writes C to spat-4, B to spat-3
                        // Push DE: SP = spat-6, writes E to spat-6, D to spat-5
                        // Push HL: SP = spat-8, writes L to spat-8, H to spat-7
                        // Push IX: SP = spat-10, writes XL to spat-10, XH to spat-9
                        // Push IY: SP = spat-12, writes YL to spat-12, YH to spat-11
                        sb.AppendLine("\nVerifying push layout (msat + 14 = spat):");
                        sb.AppendLine($"  msat[0..1] (memop): {_cpu.Memory.Read(msatAddr):X2} {_cpu.Memory.Read((ushort)(msatAddr + 1)):X2}");
                        sb.AppendLine($"  msat[2..3] (IY):    {_cpu.Memory.Read((ushort)(msatAddr + 2)):X2} {_cpu.Memory.Read((ushort)(msatAddr + 3)):X2}");
                        sb.AppendLine($"  msat[4..5] (IX):    {_cpu.Memory.Read((ushort)(msatAddr + 4)):X2} {_cpu.Memory.Read((ushort)(msatAddr + 5)):X2}");
                        sb.AppendLine($"  msat[6..7] (HL):    {_cpu.Memory.Read((ushort)(msatAddr + 6)):X2} {_cpu.Memory.Read((ushort)(msatAddr + 7)):X2}");
                        sb.AppendLine($"  msat[8..9] (DE):    {_cpu.Memory.Read((ushort)(msatAddr + 8)):X2} {_cpu.Memory.Read((ushort)(msatAddr + 9)):X2}");
                        sb.AppendLine($"  msat[10..11] (BC):  {_cpu.Memory.Read((ushort)(msatAddr + 10)):X2} {_cpu.Memory.Read((ushort)(msatAddr + 11)):X2}");
                        sb.AppendLine($"  msat[12] (F):       {_cpu.Memory.Read((ushort)(msatAddr + 12)):X2}");
                        sb.AppendLine($"  msat[13] (A):       {_cpu.Memory.Read((ushort)(msatAddr + 13)):X2}");
                        sb.AppendLine($"  spat[0..1] (SP):    {_cpu.Memory.Read(spatAddr):X2} {_cpu.Memory.Read((ushort)(spatAddr + 1)):X2}");

                        // What was the expected CRC?
                        // Look at the HL register state during BDOS call
                        sb.AppendLine($"\nRegister state at BDOS call:");
                        sb.AppendLine($"  AF={_cpu.Regs.AF:X4} BC={_cpu.Regs.BC:X4} DE={_cpu.Regs.DE:X4} HL={_cpu.Regs.HL:X4}");
                        sb.AppendLine($"  IX={_cpu.Regs.IX:X4} IY={_cpu.Regs.IY:X4} SP={_cpu.Regs.SP:X4}");
                    }
                }
                _cpu.Regs.PC = _cpu.StackPop();
                iterations++;
                continue;
            }
            if (pc == 0x0000) break;

            _cpu.Step();
            iterations++;
        }

        sb.AppendLine($"\nIterations: {iterations}");
        Assert.Fail(sb.ToString());
    }
}
