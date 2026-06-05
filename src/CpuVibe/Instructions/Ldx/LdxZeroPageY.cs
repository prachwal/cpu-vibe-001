namespace CpuVibe.Instructions.Ldx;

/// <summary>
/// LDX zp,Y — Load X Register (Zero Page,Y)
/// Opcode: $B6 | Size: 2 bytes | Cycles: 4
/// X = [(zp + Y) & 0xFF]
/// Flags: N, Z
/// </summary>
public static class LdxZeroPageY
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.Y) & 0xFF);
        cpu.Regs.X = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
