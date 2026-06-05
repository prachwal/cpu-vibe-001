namespace Mos6502.Instructions.Ldx;

/// <summary>
/// LDX abs,Y — Load X Register (Absolute,Y)
/// Opcode: $BE | Size: 3 bytes | Cycles: 4 (+1 page crossing)
/// X = [addr + Y]
/// Flags: N, Z
/// </summary>
public static class LdxAbsoluteY
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.Y);
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        cpu.Regs.X = cpu.Memory.Read(finalAddr);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.X == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (cpu.Regs.X & 0x80) != 0);
        cpu.Cycles += 4;
    }
}
