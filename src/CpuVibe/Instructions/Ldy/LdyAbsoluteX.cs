namespace CpuVibe.Instructions.Ldy;

/// <summary>
/// LDY abs,X — Load Y Register (Absolute,X)
/// Opcode: $BC | Size: 3 bytes | Cycles: 4 (+1 page crossing)
/// Y = [addr + X]
/// Flags: N, Z
/// </summary>
public static class LdyAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        cpu.Regs.Y = cpu.Memory.Read(finalAddr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.Y == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.Y & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
