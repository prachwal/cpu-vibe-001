namespace Mos6502.Instructions.Jmp;

/// <summary>
/// JMP abs — Jump to Absolute Address
/// Opcode: $4C | Size: 3 bytes | Cycles: 3
/// PC = addr
/// </summary>
public static class JmpAbsolute
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.PC = cpu.ReadAddress();
        cpu.Cycles += 3;
    }
}
