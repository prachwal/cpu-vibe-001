namespace CpuVibe.Instructions.Sty;

/// <summary>
/// STY zp,X — Store Y Register (Zero Page,X)
/// Opcode: $94 | Size: 2 bytes | Cycles: 4
/// [(zp + X) & 0xFF] = Y
/// </summary>
public static class StyZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        cpu.Memory.Write(addr, cpu.Regs.Y);
        cpu.Cycles += 4;
    }
}
