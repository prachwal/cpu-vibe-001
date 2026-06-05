using Z80.Core;

namespace Z80.Instructions.Ed;

public static class NopEd
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Cycles += 8;
    }
}
