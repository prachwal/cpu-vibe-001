namespace Mos6502.Instructions.Ldy;

/// <summary>
/// LDY zp — Load Y Register (Zero Page)
/// Opcode: $A4 | Size: 2 bytes | Cycles: 3
/// Y = [zp]
/// Flags: N, Z
/// </summary>
public static class LdyZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.Y = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 3;
    }
}
