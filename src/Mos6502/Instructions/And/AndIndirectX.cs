namespace Mos6502.Instructions.And;

/// <summary>
/// AND (zp,X) — Bitwise AND (Indexed Indirect)
/// Opcode: $21 | Size: 2 bytes | Cycles: 6
/// A = A & [ptr + X]
/// Flags: N, Z
/// </summary>
public static class AndIndirectX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte ptr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte lo = cpu.Memory.Read(ptr);
        byte hi = cpu.Memory.Read((byte)((ptr + 1) & 0xFF));
        ushort addr = (ushort)((hi << 8) | lo);
        byte operand = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A & operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 6;
    }
}
