namespace CpuVibe.Instructions.Ldx;

/// <summary>
/// LDX abs — Load X Register (Absolute)
/// Opcode: $AE | Size: 3 bytes | Cycles: 4
/// X = [addr]
/// Flags: N, Z
/// </summary>
public static class LdxAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Regs.X = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
