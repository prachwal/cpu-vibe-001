namespace CpuVibe.Instructions.Phx;

/// <summary>
/// PHX — Push X (Implied)
/// Opcode: $DA | Size: 1 byte | Cycles: 2
/// StackPush(X)
/// Flags: none
/// </summary>
public static class PhxImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.StackPush(cpu.Regs.X);
        cpu.Cycles += 2;
    }
}
