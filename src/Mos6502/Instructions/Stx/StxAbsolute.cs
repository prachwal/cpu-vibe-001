namespace Mos6502.Instructions.Stx;

/// <summary>
/// STX abs — Store X Register (Absolute)
/// Opcode: $8E | Size: 3 bytes | Cycles: 4
/// [addr] = X
/// </summary>
public static class StxAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Memory.Write(addr, cpu.Regs.X);
        cpu.Cycles += 4;
    }
}
