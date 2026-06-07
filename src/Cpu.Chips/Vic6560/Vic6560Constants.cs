namespace Cpu.Chips.Vic6560;

public static class Vic6560Constants
{
    public const int RegisterCount = 16;
    public const int RegisterMirrorSize = 0x100;

    /// <summary>Maps 14-bit VIC address to CPU bus (A15 = NOT A13).</summary>
    public static ushort ToCpuAddress(int vicAddress)
    {
        int msb = (~((vicAddress & 0x2000) << 2)) & 0x8000;
        return (ushort)((vicAddress & 0x1FFF) | msb);
    }

    public const double Phi2Ntsc = 1_431_8181.0 / 14.0;
    public const double Phi2Pal = 4_433_618.0 / 4.0;

    public const int TotalScanlinesNtsc = 261;
    public const int TotalScanlinesPal = 312;
    public const int CyclesPerLineNtsc = 65;
    public const int CyclesPerLinePal = 71;
}
