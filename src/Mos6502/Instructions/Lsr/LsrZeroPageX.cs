namespace Mos6502.Instructions.Lsr;

/// <summary>
/// LSR zp,X — Logical Shift Right (Zero Page,X)
/// Opcode: $56 | Size: 2 bytes | Cycles: 6
/// Flags: N=0, Z, C
/// </summary>
public static class LsrZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x01) != 0);
        value = (byte)(value >> 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, false);
        cpu.Cycles += 6;
    }
}
