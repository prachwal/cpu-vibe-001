namespace Cpu.Chips.Vic20;

public enum Vic20ExpansionPreset
{
    None,
    K3,
    K8,
    K16,
    K24,
    All
}

public static class Vic20MemoryMap
{
    public const ushort BuiltinRamStart = 0x1000;
    public const int BuiltinRamSize = 0x1000;

    public const ushort CharRomStart = 0x8000;
    public const int CharRomSize = 0x1000;

    public const ushort VicBaseAddress = 0x9000;

    public const ushort ColorRamStart = 0x9400;
    public const int ColorRamSize = 0x0400;

    public const ushort BasicRomStart = 0xC000;
    public const int BasicRomSize = 0x2000;

    public const ushort KernalRomStart = 0xE000;
    public const int KernalRomSize = 0x2000;

    public const ushort Block1Start = 0x2000;
    public const ushort Block2Start = 0x4000;
    public const ushort Block3Start = 0x6000;
    public const ushort Block5Start = 0xA000;
    public const int BlockSize = 0x2000;

    public const ushort ViaBaseAddress = 0x9110;
}
