namespace Mos6502.Instructions.Lsr;

/// <summary>
/// LSR abs,X — Logical Shift Right (Absolute,X)
/// Opcode: $5E | Size: 3 bytes | Cycles: 7
/// Flags: N=0, Z, C
/// </summary>
public static class LsrAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        byte value = cpu.Memory.Read(finalAddr);
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x01) != 0);
        value = (byte)(value >> 1);
        cpu.Memory.Write(finalAddr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, false);
        cpu.Cycles += 7;
    }
}
