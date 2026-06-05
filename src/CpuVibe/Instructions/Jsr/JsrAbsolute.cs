namespace CpuVibe.Instructions.Jsr;

/// <summary>
/// JSR abs — Jump to Subroutine
/// Opcode: $20 | Size: 3 bytes | Cycles: 6
/// Push PC+2, PC = addr
/// </summary>
public static class JsrAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort returnAddr = (ushort)(cpu.Regs.PC - 1);
        cpu.StackPush((byte)(returnAddr >> 8));
        cpu.StackPush((byte)(returnAddr & 0xFF));
        cpu.Regs.PC = addr;
        cpu.Cycles += 6;
    }
}
