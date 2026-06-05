namespace CpuVibe.Benchmarks;

public class SystemInfo
{
    public string Name { get; }
    public double Mhz { get; }

    public SystemInfo(string name, double mhz)
    {
        Name = name;
        Mhz = mhz;
    }
}

public static class RealSystems
{
    public static readonly SystemInfo[] All = new[]
    {
        new SystemInfo("MOS 6502 (1 MHz)",   1.00),
        new SystemInfo("Apple II",           1.02),
        new SystemInfo("C64 NTSC",           1.02),
        new SystemInfo("C64 PAL",            0.99),
        new SystemInfo("Atari 2600",         1.19),
        new SystemInfo("Atari 400/800",      1.79),
        new SystemInfo("BBC Micro",          2.00),
        new SystemInfo("NES NTSC",           1.79),
        new SystemInfo("NES PAL",            1.66),
        new SystemInfo("WDC 65C02 (8 MHz)",  8.00),
        new SystemInfo("WDC 65C02 (14 MHz)", 14.00),
        new SystemInfo("WDC 65C816 (4 MHz)", 4.00),
        new SystemInfo("WDC 65C816 (8 MHz)", 8.00),
        new SystemInfo("WDC 65C816 (14 MHz)",14.00),
        new SystemInfo("Apple IIGS",         2.80),
        new SystemInfo("SNES NTSC",          3.58),
        new SystemInfo("SNES PAL",           3.07),
    };
}
