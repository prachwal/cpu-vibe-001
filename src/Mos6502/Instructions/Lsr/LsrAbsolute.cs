namespace Mos6502.Instructions.Lsr;

/// <summary>
/// LSR abs — Logical Shift Right (Absolute)
/// Opcode: $4E | Size: 3 bytes | Cycles: 6
/// Flags: N=0, Z, C
/// </summary>
public static class LsrAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x01) != 0);
        value = (byte)(value >> 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, false);
        cpu.Cycles += 6;
    }
}
