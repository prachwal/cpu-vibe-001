namespace Mos6502.Instructions.Ora;

/// <summary>
/// ORA abs,X — Bitwise OR (Absolute,X)
/// Opcode: $1D | Size: 3 bytes | Cycles: 4 (+1 page crossing)
/// A = A | [addr + X]
/// Flags: N, Z
/// </summary>
public static class OraAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte operand = cpu.Memory.Read(finalAddr);
        byte result = (byte)(cpu.Regs.A | operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.A = result;
        cpu.Cycles += 4;
    }
}
