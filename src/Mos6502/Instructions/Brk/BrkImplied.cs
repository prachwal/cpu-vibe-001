namespace Mos6502.Instructions.Brk;

/// <summary>
/// BRK — Break (Software Interrupt)
/// Opcode: $00 | Size: 2 bytes (opcode + padding byte) | Cycles: 7
/// Push P+PC, jump to IRQ vector ($FFFE-$FFFF)
/// </summary>
public static class BrkImplied
{
    public static void Execute(Cpu cpu)
    {
        cpu.Regs.PC++; // skip padding byte

        // Push PC high byte
        cpu.StackPush((byte)(cpu.Regs.PC >> 8));
        // Push PC low byte
        cpu.StackPush((byte)(cpu.Regs.PC & 0xFF));
        // Push P with B flag set
        cpu.StackPush((byte)(cpu.Regs.P | CpuFlags.Break));

        cpu.Regs.SetFlag(CpuFlags.Interrupt, true);

        // Jump to IRQ vector
        byte lo = cpu.Memory.Read(0xFFFE);
        byte hi = cpu.Memory.Read(0xFFFF);
        cpu.Regs.PC = (ushort)((hi << 8) | lo);
        cpu.Cycles += 7;
    }
}
