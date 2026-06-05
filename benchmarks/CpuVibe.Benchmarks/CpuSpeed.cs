namespace CpuVibe.Benchmarks;

public enum CpuSpeed : long
{
    C64_PAL        = 985_248,
    Mos6502_1MHz   = 1_000_000,
    C64_NTSC       = 1_022_727,
    AppleII        = 1_023_000,
    Atari2600      = 1_193_182,
    Nes_PAL        = 1_662_607,
    Nes_NTSC       = 1_789_773,
    Mos6502_2MHz   = 2_000_000,
    AppleIIGS      = 2_800_000,
    Snes_PAL       = 3_072_000,
    Snes_NTSC      = 3_579_545,
    Wdc65C816_4MHz = 4_000_000,
    Wdc65C02_8MHz  = 8_000_000,
    Wdc65C02_14MHz = 14_000_000,
}

public static class CpuSpeedInfo
{
    private static readonly Dictionary<CpuSpeed, string> Names = new()
    {
        [CpuSpeed.C64_PAL]        = "C64 PAL",
        [CpuSpeed.Mos6502_1MHz]   = "MOS 6502 (1 MHz)",
        [CpuSpeed.C64_NTSC]       = "C64 NTSC",
        [CpuSpeed.AppleII]        = "Apple II",
        [CpuSpeed.Atari2600]      = "Atari 2600",
        [CpuSpeed.Nes_PAL]        = "NES PAL",
        [CpuSpeed.Nes_NTSC]       = "NES NTSC",
        [CpuSpeed.Mos6502_2MHz]   = "MOS 6502 (2 MHz)",
        [CpuSpeed.AppleIIGS]      = "Apple IIGS",
        [CpuSpeed.Snes_PAL]       = "SNES PAL",
        [CpuSpeed.Snes_NTSC]      = "SNES NTSC",
        [CpuSpeed.Wdc65C816_4MHz] = "WDC 65C816 (4 MHz)",
        [CpuSpeed.Wdc65C02_8MHz]  = "WDC 65C02 (8 MHz)",
        [CpuSpeed.Wdc65C02_14MHz] = "WDC 65C02 (14 MHz)",
    };

    public static double GetMhz(this CpuSpeed speed) => (double)(long)speed / 1_000_000.0;

    public static string GetName(this CpuSpeed speed) =>
        Names.TryGetValue(speed, out var name) ? name : speed.ToString();
}
