namespace CpuVibe.Instructions.Asl;

/// <summary>
/// ASL abs — Arithmetic Shift Left (Absolute)
/// Opcode: $0E | Size: 3 bytes | Cycles: 6
/// Flags: N, Z, C
/// </summary>
public static class AslAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x80) != 0);
        value = (byte)(value << 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 6;
    }
}
