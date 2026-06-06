using Z80.Core;

namespace Z80.Instructions.DdFd;

public static class NopDdFd
{
    public static int CallCount { get; private set; }
    public static byte LastSubOpcode { get; private set; }
    public static ushort LastPC { get; private set; }

    public static void Execute(Cpu cpu, byte opcode)
    {
        CallCount++;
        LastSubOpcode = opcode;
        LastPC = cpu.Regs.PC;
        cpu.Cycles += 4;
    }

    public static void ResetCount() => CallCount = 0;
}
