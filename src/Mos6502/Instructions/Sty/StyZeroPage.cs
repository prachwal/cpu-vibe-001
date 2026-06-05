namespace Mos6502.Instructions.Sty;

/// <summary>
/// STY zp — Store Y Register (Zero Page)
/// Opcode: $84 | Size: 2 bytes | Cycles: 3
/// [zp] = Y
/// </summary>
public static class StyZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Memory.Write(addr, cpu.Regs.Y);
        cpu.Cycles += 3;
    }
}
