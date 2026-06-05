namespace Mos6502.Instructions.Cmp;

/// <summary>
/// CMP #imm — Compare Accumulator (Immediate)
/// Opcode: $C9 | Size: 2 bytes | Cycles: 2
/// Compare: A - imm
/// Flags: N, Z, C
/// </summary>
public static class CmpImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 2;
    }
}
