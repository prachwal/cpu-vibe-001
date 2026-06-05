namespace CpuVibe.Instructions.Ldy;

/// <summary>
/// LDY abs — Load Y Register (Absolute)
/// Opcode: $AC | Size: 3 bytes | Cycles: 4
/// Y = [addr]
/// Flags: N, Z
/// </summary>
public static class LdyAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Regs.Y = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
