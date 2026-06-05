namespace CpuVibe.Instructions.Ply;

/// <summary>
/// PLY — Pull Y (Implied)
/// Opcode: $7A | Size: 1 byte | Cycles: 2
/// Y = StackPop()
/// Flags: N, Z
/// </summary>
public static class PlyImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.Y = cpu.StackPop();
        cpu.Regs.SetNZ(cpu.Regs.Y);
        cpu.Cycles += 2;
    }
}
