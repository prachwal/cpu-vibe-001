namespace CpuVibe.Instructions.Asl;

/// <summary>
/// ASL A — Arithmetic Shift Left (Accumulator)
/// Opcode: $0A | Size: 1 byte | Cycles: 2
/// C = A bit 7, A = A << 1
/// Flags: N, Z, C
/// </summary>
public static class AslAccumulator
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.SetFlag(CpuFlags.Carry, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.A = (byte)(cpu.Regs.A << 1);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.A & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
