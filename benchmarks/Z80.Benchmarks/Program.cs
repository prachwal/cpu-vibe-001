using System.Diagnostics;
using Z80.Core;

namespace Z80.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           CPU-VIBE-001  Z80 Emulator Benchmark            ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  Runtime : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  OS      : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        Console.WriteLine();

        double[] mipsResults = new double[6];
        string[] labels = { "NOP", "LD r,r", "ADD A,r", "AND/OR/XOR", "INC/DEC", "JP/JR" };

        mipsResults[0] = BenchmarkNop();
        mipsResults[1] = BenchmarkLdRegister();
        mipsResults[2] = BenchmarkAddARegister();
        mipsResults[3] = BenchmarkLogical();
        mipsResults[4] = BenchmarkIncDec();
        mipsResults[5] = BenchmarkJump();

        double avgMips = mipsResults.Average();
        int bestIdx = Array.IndexOf(mipsResults, mipsResults.Max());
        int worstIdx = Array.IndexOf(mipsResults, mipsResults.Min());

        Console.WriteLine();
        Console.WriteLine("┌──────────────────────────────────────────────────────────┐");
        Console.WriteLine("│                     PODSUMOWANIE                         │");
        Console.WriteLine("├──────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│  Srednia MIPS   :  {avgMips,8:F2}                              │");
        Console.WriteLine($"│  Najlepszy      :  {mipsResults.Max(),8:F2}  ({labels[bestIdx],-8})                  │");
        Console.WriteLine($"│  Najgorszy      :  {mipsResults.Min(),8:F2}  ({labels[worstIdx],-8})                  │");
        Console.WriteLine("└──────────────────────────────────────────────────────────┘");

        Console.WriteLine();
        Console.WriteLine("┌──────────────────────────────────────────────────────────┐");
        Console.WriteLine("│           POROWNANIE Z PRAWDZIWYMI PROCESORAMI           │");
        Console.WriteLine("├────────────────────────┬──────────┬──────────────────────┤");
        Console.WriteLine("│  System                │  MHz     │  Przewaga x          │");
        Console.WriteLine("├────────────────────────┼──────────┼──────────────────────┤");

        var targets = new (string name, double mhz)[]
        {
            ("Zilog Z80 4MHz", 4.0),
            ("Zilog Z80 8MHz", 8.0),
            ("Zilog Z80 20MHz", 20.0),
            ("Amstrad CPC 4MHz", 4.0),
            ("MSX 3.58MHz", 3.58),
            ("ZX Spectrum 3.5MHz", 3.5),
        };

        foreach (var (name, mhz) in targets)
        {
            double ratio = avgMips / mhz;
            Console.WriteLine($"│  {name,-21} │ {mhz,7:F2}  │  {ratio,7:F1}x            │");
        }

        Console.WriteLine("└────────────────────────┴──────────┴──────────────────────┘");
    }

    static double RunBench(Cpu cpu, int iterations, int warmup = 100_000)
    {
        cpu.Regs.PC = 0x0200;
        for (int i = 0; i < warmup; i++) cpu.Step();

        cpu.Regs.PC = 0x0200;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            cpu.Step();
        sw.Stop();

        return iterations / (sw.ElapsedMilliseconds / 1000.0) / 1_000_000;
    }

    static double BenchmarkNop()
    {
        var cpu = new Cpu();
        // NOP + JP 0x0200
        cpu.Memory.Write(0x0200, 0x00); // NOP
        cpu.Memory.Write(0x0201, 0xC3); cpu.Memory.Write(0x0202, 0x00); cpu.Memory.Write(0x0203, 0x02); // JP 0x0200

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  NOP x5M         :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkLdRegister()
    {
        var cpu = new Cpu();
        // LD B,A + LD C,B + JP 0x0200
        cpu.Memory.Write(0x0200, 0x47); // LD B,A
        cpu.Memory.Write(0x0201, 0x48); // LD C,B
        cpu.Memory.Write(0x0202, 0xC3); cpu.Memory.Write(0x0203, 0x00); cpu.Memory.Write(0x0204, 0x02); // JP 0x0200

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  LD r,r x5M      :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkAddARegister()
    {
        var cpu = new Cpu();
        // ADD A,B + ADC A,C + JP 0x0200
        cpu.Memory.Write(0x0200, 0x80); // ADD A,B
        cpu.Memory.Write(0x0201, 0x89); // ADC A,C
        cpu.Memory.Write(0x0202, 0xC3); cpu.Memory.Write(0x0203, 0x00); cpu.Memory.Write(0x0204, 0x02); // JP 0x0200
        cpu.Regs.A = 0x42;
        cpu.Regs.B = 0x01;
        cpu.Regs.C = 0x02;

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  ADD/ADC A,r x5M :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkLogical()
    {
        var cpu = new Cpu();
        // AND B + OR C + XOR D + JP 0x0200
        cpu.Memory.Write(0x0200, 0xA0); // AND B
        cpu.Memory.Write(0x0201, 0xB1); // OR C
        cpu.Memory.Write(0x0202, 0xAA); // XOR D
        cpu.Memory.Write(0x0203, 0xC3); cpu.Memory.Write(0x0204, 0x00); cpu.Memory.Write(0x0205, 0x02); // JP 0x0200
        cpu.Regs.A = 0xFF;
        cpu.Regs.B = 0x0F;
        cpu.Regs.C = 0xF0;
        cpu.Regs.D = 0xAA;

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  AND/OR/XOR x5M  :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkIncDec()
    {
        var cpu = new Cpu();
        // INC B + DEC C + JP 0x0200
        cpu.Memory.Write(0x0200, 0x04); // INC B
        cpu.Memory.Write(0x0201, 0x0D); // DEC C
        cpu.Memory.Write(0x0202, 0xC3); cpu.Memory.Write(0x0203, 0x00); cpu.Memory.Write(0x0204, 0x02); // JP 0x0200

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  INC/DEC x5M     :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkJump()
    {
        var cpu = new Cpu();
        // JP 0x0200
        cpu.Memory.Write(0x0200, 0xC3); cpu.Memory.Write(0x0201, 0x00); cpu.Memory.Write(0x0202, 0x02); // JP 0x0200

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  JP x5M          :  {mips,8:F2} MIPS");
        return mips;
    }
}
