namespace Mos6502.Instructions.Sta;

/// <summary>
/// STA zp,X — Store Accumulator (Zero Page,X)
/// Opcode: $95 | Size: 2 bytes | Cycles: 4
/// [(zp + X) & 0xFF] = A
/// </summary>
public static class StaZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        cpu.Memory.Write(addr, cpu.Regs.A);
        cpu.Cycles += 4;
    }
}
