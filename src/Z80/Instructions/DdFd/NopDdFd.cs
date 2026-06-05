using Z80.Core;

namespace Z80.Instructions.DdFd;

public static class NopDdFd
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Cycles += 4;
    }
}
