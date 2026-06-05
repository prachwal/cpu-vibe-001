namespace Mos6502.Instructions.Eor;

/// <summary>
/// EOR #imm — Bitwise XOR (Immediate)
/// Opcode: $49 | Size: 2 bytes | Cycles: 2
/// A = A ^ imm
/// Flags: N, Z
/// </summary>
public static class EorImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);
        byte result = (byte)(cpu.Regs.A ^ operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 2;
    }
}
