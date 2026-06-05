namespace CpuVibe.Instructions.Rol;

/// <summary>
/// ROL A — Rotate Left (Accumulator)
/// Opcode: $2A | Size: 1 byte | Cycles: 2
/// old C -> A bit 0, A bit 7 -> new C
/// Flags: N, Z, C
/// </summary>
public static class RolAccumulator
{
    public static void Execute(Cpu cpu)
    {
        byte oldCarry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
        cpu.Regs.SetFlag(CpuFlags.Carry, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.A = (byte)((cpu.Regs.A << 1) | oldCarry);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.A & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
