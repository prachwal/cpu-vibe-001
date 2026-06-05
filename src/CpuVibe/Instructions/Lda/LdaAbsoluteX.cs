namespace CpuVibe.Instructions.Lda;

/// <summary>
/// LDA abs,X — Load Accumulator from Absolute,X
/// Opcode: $BD | Size: 3 bytes | Cycles: 4 (+1 if page crossing)
/// A = [addr + X]
/// </summary>
public static class LdaAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        cpu.Cycles += 4;
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte value = cpu.Memory.Read(finalAddr);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
    }
}
