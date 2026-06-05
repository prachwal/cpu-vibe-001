namespace CpuVibe.Instructions.Lsr;

/// <summary>
/// LSR zp — Logical Shift Right (Zero Page)
/// Opcode: $46 | Size: 2 bytes | Cycles: 5
/// Flags: N=0, Z, C
/// </summary>
public static class LsrZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x01) != 0);
        value = (byte)(value >> 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, false);
        cpu.Cycles += 5;
    }
}
