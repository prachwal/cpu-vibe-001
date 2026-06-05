namespace Mos6502.Instructions.Sta;

/// <summary>
/// STA zp — Store Accumulator (Zero Page)
/// Opcode: $85 | Size: 2 bytes | Cycles: 3
/// [zp] = A
/// </summary>
public static class StaZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Memory.Write(addr, cpu.Regs.A);
        cpu.Cycles += 3;
    }
}
