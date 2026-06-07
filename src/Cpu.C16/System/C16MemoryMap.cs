namespace Cpu.C16.System;

public static class C16MemoryMap
{
    public const ushort TedBaseAddress = 0xFF00;
    public const ushort TedEndAddress = 0xFF3F;
    public const int TedRegisterCount = 64;
    public const ushort SwitchRomAddress = 0xFF3E;
    public const ushort SwitchRamAddress = 0xFF3F;
    public const ushort Pio2BaseAddress = 0xFD30;
    public const ushort Pio2EndAddress = 0xFD3F;

    public const ushort BasicRomStart = 0x8000;
    public const int BasicRomSize = 0x4000;
    public const ushort KernalRomStart = 0xC000;
    public const int KernalRomSize = 0x4000;

    public const ushort ScreenMemoryDefault = 0x0C00;
    public const int ScreenRows = 25;
    public const int ScreenCols = 40;
    public const int ScreenSize = ScreenRows * ScreenCols;
}
