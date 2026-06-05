namespace CpuVibe.Instructions.Bra;

/// <summary>
/// BRA rel — Branch Always (Relative)
/// Opcode: $80 | Size: 2 bytes | Cycles: 2
/// PC = PC + offset
/// Flags: none
/// </summary>
public static class BraRelative
{
    public static void Execute(Cpu cpu)
    {
        sbyte offset = (sbyte)cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.PC = (ushort)(cpu.Regs.PC + offset);
        cpu.Cycles += 2;
    }
}
