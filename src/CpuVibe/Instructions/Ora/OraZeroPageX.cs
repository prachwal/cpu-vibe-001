namespace CpuVibe.Instructions.Ora;

/// <summary>
/// ORA zp,X — Bitwise OR (Zero Page,X)
/// Opcode: $15 | Size: 2 bytes | Cycles: 4
/// A = A | [(zp + X) & 0xFF]
/// Flags: N, Z
/// </summary>
public static class OraZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte operand = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A | operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 4;
    }
}
