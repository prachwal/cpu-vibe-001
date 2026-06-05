namespace Mos6502.Instructions.Sty;

/// <summary>
/// STY abs — Store Y Register (Absolute)
/// Opcode: $8C | Size: 3 bytes | Cycles: 4
/// [addr] = Y
/// </summary>
public static class StyAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Memory.Write(addr, cpu.Regs.Y);
        cpu.Cycles += 4;
    }
}
