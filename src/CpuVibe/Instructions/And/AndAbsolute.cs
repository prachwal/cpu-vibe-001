namespace CpuVibe.Instructions.And;

/// <summary>
/// AND abs — Bitwise AND (Absolute)
/// Opcode: $2D | Size: 3 bytes | Cycles: 4
/// A = A & [addr]
/// Flags: N, Z
/// </summary>
public static class AndAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte operand = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A & operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 4;
    }
}
