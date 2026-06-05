namespace Z80.Instructions.Nop;

/// <summary>
/// NOP — No Operation
/// Opcode: $00 | Size: 1 byte | Cycles: 4
/// Flags: none affected
/// </summary>
public static class NopImplied
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Cycles += 4;
    }
}
