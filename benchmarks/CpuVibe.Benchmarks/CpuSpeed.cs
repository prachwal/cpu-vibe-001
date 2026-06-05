namespace CpuVibe.Benchmarks;

/// <summary>
/// Real-world 6502-based systems with their clock speeds in Hz.
/// </summary>
public enum CpuSpeed : long
{
    Mos6502_1MHz   = 1_000_000,
    Mos6502_2MHz   = 2_000_000,

    AppleII        = 1_023_000,

    C64_NTSC       = 1_022_727,
    C64_PAL        = 985_248,

    Nes_NTSC       = 1_789_773,
    Nes_PAL        = 1_662_607,

    Atari2600      = 1_193_182,
    Atari400_800   = 1_789_773,

    BBC_Micro      = 2_000_000,

    Vic20          = 1_022_727,

    Wdc65C02_8MHz  = 8_000_000,
    Wdc65C02_14MHz = 14_000_000,

    Wdc65C816_4MHz = 4_000_000,
    Wdc65C816_8MHz = 8_000_000,
    Wdc65C816_14MHz = 14_000_000,

    AppleIIGS      = 2_800_000,

    Snes_NTSC      = 3_579_545,
    Snes_PAL       = 3_072_000,
}

public static class CpuSpeedInfo
{
    private static readonly Dictionary<CpuSpeed, string> Names = new()
    {
        [CpuSpeed.Mos6502_1MHz]   = "MOS 6502 (1 MHz)",
        [CpuSpeed.Mos6502_2MHz]   = "MOS 6502 (2 MHz)",
        [CpuSpeed.AppleII]        = "Apple II",
        [CpuSpeed.C64_NTSC]       = "C64 NTSC",
        [CpuSpeed.C64_PAL]        = "C64 PAL",
        [CpuSpeed.Nes_NTSC]       = "NES NTSC",
        [CpuSpeed.Nes_PAL]        = "NES PAL",
        [CpuSpeed.Atari2600]      = "Atari 2600",
        [CpuSpeed.Atari400_800]   = "Atari 400/800",
        [CpuSpeed.BBC_Micro]      = "BBC Micro",
        [CpuSpeed.Wdc65C02_8MHz]  = "WDC 65C02 (8 MHz)",
        [CpuSpeed.Wdc65C02_14MHz] = "WDC 65C02 (14 MHz)",
        [CpuSpeed.Wdc65C816_4MHz] = "WDC 65C816 (4 MHz)",
        [CpuSpeed.Wdc65C816_8MHz] = "WDC 65C816 (8 MHz)",
        [CpuSpeed.Wdc65C816_14MHz]= "WDC 65C816 (14 MHz)",
        [CpuSpeed.AppleIIGS]      = "Apple IIGS",
        [CpuSpeed.Snes_NTSC]      = "SNES NTSC",
        [CpuSpeed.Snes_PAL]       = "SNES PAL",
    };

    public static double GetMhz(this CpuSpeed speed) => (double)(long)speed / 1_000_000.0;

    public static string GetName(this CpuSpeed speed) =>
        Names.TryGetValue(speed, out var name) ? name : speed.ToString();
}
