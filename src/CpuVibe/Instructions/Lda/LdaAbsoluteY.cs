namespace CpuVibe.Instructions.Lda;

/// <summary>
/// LDA abs,Y — Load Accumulator from Absolute,Y
/// Opcode: $B9 | Size: 3 bytes | Cycles: 4 (+1 if page crossing)
/// A = [addr + Y]
/// </summary>
public static class LdaAbsoluteY
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.Y);
        cpu.Cycles += 4;
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte value = cpu.Memory.Read(finalAddr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
    }
}
