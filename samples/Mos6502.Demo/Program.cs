using System.Diagnostics;

namespace Mos6502.Demo;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════╗");
        Console.WriteLine("║   CPU-VIBE-001 — 6502 Throttle Demo          ║");
        Console.WriteLine("╚═══════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.WriteLine("Wybierz tryb:");
        Console.WriteLine("  1. Bez throttle (max speed)");
        Console.WriteLine("  2. NES NTSC  (1.79 MHz)");
        Console.WriteLine("  3. C64 PAL   (0.99 MHz)");
        Console.WriteLine("  4. BBC Micro (2.00 MHz)");
        Console.WriteLine("  5. WDC 65C02 (8.00 MHz)");
        Console.WriteLine();
        Console.Write("Wybor (1-5): ");

        string? choice = Console.ReadLine()?.Trim();

        long targetHz = choice switch
        {
            "2" => 1_789_773,
            "3" => 985_248,
            "4" => 2_000_000,
            "5" => 8_000_000,
            _   => 0
        };

        string systemName = choice switch
        {
            "2" => "NES NTSC @ 1.79 MHz",
            "3" => "C64 PAL @ 0.99 MHz",
            "4" => "BBC Micro @ 2.00 MHz",
            "5" => "WDC 65C02 @ 8.00 MHz",
            _   => "MAX SPEED (no throttle)"
        };

        Console.WriteLine();
        Console.WriteLine($"  System: {systemName}");
        Console.WriteLine();

        var cpu = new Cpu();

        if (targetHz > 0)
            cpu.Throttle = new Throttle(targetHz);

        // Program: counter $00 -> $FF, blink
        // Loop: INC $10, LDA $10, CMP #$FF, BNE loop
        int addr = 0x0200;
        int loopAddr = addr;
        cpu.Memory.Write((ushort)addr++, 0xE6); cpu.Memory.Write((ushort)addr++, 0x10); // INC $10
        cpu.Memory.Write((ushort)addr++, 0xA5); cpu.Memory.Write((ushort)addr++, 0x10); // LDA $10
        cpu.Memory.Write((ushort)addr++, 0xC9); cpu.Memory.Write((ushort)addr++, 0xFF); // CMP #$FF
        cpu.Memory.Write((ushort)addr++, 0xD0); cpu.Memory.Write((ushort)addr++, (byte)(loopAddr - addr - 1)); // BNE loop

        cpu.Regs.PC = (ushort)loopAddr;
        cpu.Memory.Write(0x0010, 0x00);

        Console.WriteLine("  Nacisnij ENTER aby zatrzymac...");
        Console.WriteLine();

        var sw = Stopwatch.StartNew();
        long lastCycles = 0;
        int lastPercent = -1;

        var runTask = Task.Run(() =>
        {
            while (true)
            {
                cpu.Step();

                // Print progress every ~0.5s
                if (sw.ElapsedMilliseconds % 500 < 10)
                {
                    int percent = (cpu.Memory.Read(0x0010) * 100) / 255;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        long currentCycles = cpu.Cycles;
                        double cps = cpu.Throttle != null
                            ? cpu.Throttle.GetActualCps()
                            : currentCycles / (sw.Elapsed.TotalSeconds > 0 ? sw.Elapsed.TotalSeconds : 1);

                        double ratio = cpu.Throttle != null
                            ? cpu.Throttle.GetSpeedRatio()
                            : 0;

                        string bar = new string('█', percent / 5) + new string('░', 20 - percent / 5);

                        lock (Console.Out)
                        {
                            Console.SetCursorPosition(2, Console.CursorTop > 0 ? Console.CursorTop : 0);
                            Console.Write($"\r  [{bar}] {percent,3}%  |  Cycles: {currentCycles,14:N0}  |  CPS: {cps / 1_000_000,8:F2} M");
                            if (cpu.Throttle != null)
                                Console.Write($"  |  Speed: {ratio,5:F2}x");
                            Console.WriteLine();
                        }
                    }
                }
            }
        });

        Console.ReadLine();
        sw.Stop();

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("  === Wyniki ===");
        Console.WriteLine($"  Calkowite cykle : {cpu.Cycles:N0}");
        Console.WriteLine($"  Czas            : {sw.ElapsedMilliseconds} ms");
        if (sw.Elapsed.TotalSeconds > 0)
        {
            double cps = cpu.Cycles / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  Srednie CPS     : {cps / 1_000_000:F2} M");
            if (cpu.Throttle != null)
            {
                Console.WriteLine($"  Przewaga speed  : {cpu.Throttle.GetSpeedRatio():F2}x vs {systemName}");
            }
        }
        Console.WriteLine();
    }
}
