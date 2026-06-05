using System.Diagnostics;

namespace CpuVibe.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           CPU-VIBE-001  6502 Emulator Benchmark           ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  Runtime : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  OS      : {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        Console.WriteLine();

        double[] mipsResults = new double[6];
        string[] labels = { "NOP", "LDA #", "ADC/CLC", "Logic", "Shift", "Branch" };

        mipsResults[0] = BenchmarkNop();
        mipsResults[1] = BenchmarkLdaImmediate();
        mipsResults[2] = BenchmarkArithmetic();
        mipsResults[3] = BenchmarkLogical();
        mipsResults[4] = BenchmarkShift();
        mipsResults[5] = BenchmarkBranch();

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

        foreach (var sys in RealSystems.All)
        {
            double ratio = avgMips / sys.Mhz;
            Console.WriteLine($"│  {sys.Name,-21} │ {sys.Mhz,7:F2}  │  {ratio,7:F1}x            │");
        }

        Console.WriteLine("└────────────────────────┴──────────┴──────────────────────┘");

        Console.WriteLine();
        BenchmarkFibonacci();
        BenchmarkThrottle();
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
        cpu.Memory.Write(0x0200, 0xEA);
        cpu.Memory.Write(0x0201, 0x4C); cpu.Memory.Write(0x0202, 0x00); cpu.Memory.Write(0x0203, 0x02);

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  NOP x5M         :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkLdaImmediate()
    {
        var cpu = new Cpu();
        cpu.Memory.Write(0x0200, 0xA9); cpu.Memory.Write(0x0201, 0x42);
        cpu.Memory.Write(0x0202, 0x4C); cpu.Memory.Write(0x0203, 0x00); cpu.Memory.Write(0x0204, 0x02);

        double mips = RunBench(cpu, 5_000_000);
        Console.WriteLine($"  LDA #$xx x5M    :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkArithmetic()
    {
        var cpu = new Cpu();
        int a = 0x0200;
        cpu.Memory.Write((ushort)a++, 0x69); cpu.Memory.Write((ushort)a++, 0x01);
        cpu.Memory.Write((ushort)a++, 0x18);
        cpu.Memory.Write((ushort)a++, 0x69); cpu.Memory.Write((ushort)a++, 0x01);
        cpu.Memory.Write((ushort)a++, 0x18);
        cpu.Memory.Write((ushort)a++, 0x4C); cpu.Memory.Write((ushort)a++, 0x00); cpu.Memory.Write((ushort)a++, 0x02);

        double mips = RunBench(cpu, 3_000_000);
        Console.WriteLine($"  ADC/CLC x3M     :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkLogical()
    {
        var cpu = new Cpu();
        int a = 0x0200;
        cpu.Memory.Write((ushort)a++, 0x29); cpu.Memory.Write((ushort)a++, 0x0F);
        cpu.Memory.Write((ushort)a++, 0x09); cpu.Memory.Write((ushort)a++, 0xF0);
        cpu.Memory.Write((ushort)a++, 0x49); cpu.Memory.Write((ushort)a++, 0xFF);
        cpu.Memory.Write((ushort)a++, 0x4C); cpu.Memory.Write((ushort)a++, 0x00); cpu.Memory.Write((ushort)a++, 0x02);

        double mips = RunBench(cpu, 2_000_000);
        Console.WriteLine($"  AND/ORA/EOR x2M :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkShift()
    {
        var cpu = new Cpu();
        int a = 0x0200;
        cpu.Memory.Write((ushort)a++, 0x0A);
        cpu.Memory.Write((ushort)a++, 0x4A);
        cpu.Memory.Write((ushort)a++, 0x2A);
        cpu.Memory.Write((ushort)a++, 0x6A);
        cpu.Memory.Write((ushort)a++, 0x4C); cpu.Memory.Write((ushort)a++, 0x00); cpu.Memory.Write((ushort)a++, 0x02);
        cpu.Regs.A = 0xAA;

        double mips = RunBench(cpu, 2_000_000);
        Console.WriteLine($"  ASL/LSR/ROL/ROR :  {mips,8:F2} MIPS");
        return mips;
    }

    static double BenchmarkBranch()
    {
        var cpu = new Cpu();
        int a = 0x0200;
        cpu.Memory.Write((ushort)a++, 0xE8);
        cpu.Memory.Write((ushort)a++, 0xD0); cpu.Memory.Write((ushort)a++, 0xFC);
        cpu.Memory.Write((ushort)a++, 0x4C); cpu.Memory.Write((ushort)a++, 0x00); cpu.Memory.Write((ushort)a++, 0x02);

        double mips = RunBench(cpu, 3_000_000);
        Console.WriteLine($"  BNE taken x3M   :  {mips,8:F2} MIPS");
        return mips;
    }

    static void BenchmarkFibonacci()
    {
        var cpu = new Cpu();
        int a = 0x0200;
        cpu.Memory.Write((ushort)a++, 0xA9); cpu.Memory.Write((ushort)a++, 0x00);
        cpu.Memory.Write((ushort)a++, 0xA2); cpu.Memory.Write((ushort)a++, 0x0A);
        int loopAddr = a;
        cpu.Memory.Write((ushort)a++, 0xCA);
        cpu.Memory.Write((ushort)a++, 0x8A);
        cpu.Memory.Write((ushort)a++, 0x18);
        cpu.Memory.Write((ushort)a++, 0x69); cpu.Memory.Write((ushort)a++, 0x01);
        cpu.Memory.Write((ushort)a++, 0xD0); cpu.Memory.Write((ushort)a++, (byte)(loopAddr - a - 1));
        cpu.Memory.Write((ushort)a++, 0x4C); cpu.Memory.Write((ushort)a++, 0x00); cpu.Memory.Write((ushort)a++, 0x02);

        int runs = 100_000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < runs; i++)
        {
            cpu.Regs.PC = 0x0200;
            cpu.Regs.A = 0; cpu.Regs.X = 0;
            cpu.Regs.P = CpuFlags.Unused | CpuFlags.Interrupt;
            while (true)
            {
                if (cpu.Memory.Read(cpu.Regs.PC) == 0x4C) break;
                cpu.Step();
            }
        }
        sw.Stop();

        double runsPerSec = runs / (sw.ElapsedMilliseconds / 1000.0);
        Console.WriteLine($"  Fib(10) x100K   :  {sw.ElapsedMilliseconds,6} ms  |  {runsPerSec / 1000,8:F1}K runs/s");
    }

    static void BenchmarkThrottle()
    {
        Console.WriteLine();
        Console.WriteLine("  === Throttle Test (2s each) ===");

        var targets = new (string Name, long Hz)[]
        {
            ("NES NTSC ", 1_789_773),
            ("C64 PAL  ", 985_248),
            ("BBC Micro", 2_000_000),
            ("WDC 8MHz ", 8_000_000),
        };

        foreach (var (name, hz) in targets)
        {
            var cpu = new Cpu();
            cpu.Throttle = new Throttle(hz);

            // INC $10 loop
            cpu.Memory.Write(0x0200, 0xE6); cpu.Memory.Write(0x0201, 0x10);
            cpu.Memory.Write(0x0202, 0x4C); cpu.Memory.Write(0x0203, 0x00); cpu.Memory.Write(0x0204, 0x02);
            cpu.Regs.PC = 0x0200;

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 2000)
                cpu.Step();
            sw.Stop();

            double actualCps = cpu.Throttle.GetActualCps();
            double ratio = cpu.Throttle.GetSpeedRatio();
            double targetMhz = hz / 1_000_000.0;
            double actualMhz = actualCps / 1_000_000.0;

            Console.WriteLine($"  {name} ({targetMhz,5:F2} MHz) : {actualMhz,7:F2} MHz  |  {ratio,5:F2}x  |  {cpu.Cycles,14:N0} cyc");
        }
    }
}
