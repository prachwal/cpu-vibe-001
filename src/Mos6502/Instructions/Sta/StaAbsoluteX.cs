namespace Mos6502.Instructions.Sta;

/// <summary>
/// STA abs,X — Store Accumulator (Absolute,X)
/// Opcode: $9D | Size: 3 bytes | Cycles: 5
/// [addr + X] = A
/// </summary>
public static class StaAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Memory.Write((ushort)(addr + cpu.Regs.X), cpu.Regs.A);
        cpu.Cycles += 5;
    }
}
