namespace Mos6502.Instructions.Eor;

/// <summary>
/// EOR zp — Bitwise XOR (Zero Page)
/// Opcode: $45 | Size: 2 bytes | Cycles: 3
/// A = A ^ [zp]
/// Flags: N, Z
/// </summary>
public static class EorZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte operand = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A ^ operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 3;
    }
}
