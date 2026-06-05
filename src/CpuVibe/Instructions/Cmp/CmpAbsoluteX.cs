namespace CpuVibe.Instructions.Cmp;

/// <summary>
/// CMP abs,X — Compare Accumulator (Absolute,X)
/// Opcode: $DD | Size: 3 bytes | Cycles: 4 (+1 page crossing)
/// Flags: N, Z, C
/// </summary>
public static class CmpAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte operand = cpu.Memory.Read(finalAddr);
        ushort result = (ushort)(cpu.Regs.A - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.A >= operand);
        cpu.Cycles += 4;
    }
}
