namespace CpuVibe.Instructions.Ldy;

/// <summary>
/// LDY zp,X — Load Y Register (Zero Page,X)
/// Opcode: $B4 | Size: 2 bytes | Cycles: 4
/// Y = [(zp + X) & 0xFF]
/// Flags: N, Z
/// </summary>
public static class LdyZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        cpu.Regs.Y = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
