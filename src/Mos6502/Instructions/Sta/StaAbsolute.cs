namespace Mos6502.Instructions.Sta;

/// <summary>
/// STA abs — Store Accumulator (Absolute)
/// Opcode: $8D | Size: 3 bytes | Cycles: 4
/// [addr] = A
/// </summary>
public static class StaAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Memory.Write(addr, cpu.Regs.A);
        cpu.Cycles += 4;
    }
}
