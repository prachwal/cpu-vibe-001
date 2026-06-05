namespace CpuVibe.Instructions.Bit;

/// <summary>
/// BIT abs — Test Memory Bits
/// Opcode: $2C | Size: 3 bytes | Cycles: 4
/// Z = A & [addr], N = [addr] bit 7, V = [addr] bit 6
/// </summary>
public static class BitAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte value = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A & value);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Overflow, (value & 0x40) != 0);
        cpu.Cycles += 4;
    }
}
