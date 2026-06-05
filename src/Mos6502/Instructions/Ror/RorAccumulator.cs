namespace Mos6502.Instructions.Ror;

/// <summary>
/// ROR A — Rotate Right (Accumulator)
/// Opcode: $6A | Size: 1 byte | Cycles: 2
/// old C -> A bit 7, A bit 0 -> new C
/// Flags: N, Z, C
/// </summary>
public static class RorAccumulator
{
    public static void Execute(Cpu cpu)
    {
        byte oldCarry = cpu.Regs.IsCarry ? (byte)0x80 : (byte)0x00;
        cpu.Regs.SetFlag(CpuFlags.Carry, (cpu.Regs.A & 0x01) != 0);
        cpu.Regs.A = (byte)((cpu.Regs.A >> 1) | oldCarry);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.A & 0x80) != 0);
        cpu.Cycles += 2;
    }
}
