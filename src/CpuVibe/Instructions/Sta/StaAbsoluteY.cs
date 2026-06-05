namespace CpuVibe.Instructions.Sta;

/// <summary>
/// STA abs,Y — Store Accumulator (Absolute,Y)
/// Opcode: $99 | Size: 3 bytes | Cycles: 5
/// [addr + Y] = A
/// </summary>
public static class StaAbsoluteY
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Memory.Write((ushort)(addr + cpu.Regs.Y), cpu.Regs.A);
        cpu.Cycles += 5;
    }
}
