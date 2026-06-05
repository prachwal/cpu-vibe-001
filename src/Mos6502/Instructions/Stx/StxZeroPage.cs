namespace Mos6502.Instructions.Stx;

/// <summary>
/// STX zp — Store X Register (Zero Page)
/// Opcode: $86 | Size: 2 bytes | Cycles: 3
/// [zp] = X
/// </summary>
public static class StxZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Memory.Write(addr, cpu.Regs.X);
        cpu.Cycles += 3;
    }
}
