namespace Mos6502.Instructions.Eor;

/// <summary>
/// EOR zp,X — Bitwise XOR (Zero Page,X)
/// Opcode: $55 | Size: 2 bytes | Cycles: 4
/// A = A ^ [(zp + X) & 0xFF]
/// Flags: N, Z
/// </summary>
public static class EorZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte operand = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A ^ operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 4;
    }
}
