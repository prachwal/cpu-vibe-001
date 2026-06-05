namespace CpuVibe.Instructions.Bne;

/// <summary>
/// BNE rel — Branch if Not Equal (Z=0)
/// Opcode: $D0 | Size: 2 bytes | Cycles: 2 (+1 taken, +2 page cross)
/// if (!Z) PC += offset
/// </summary>
public static class BneRelative
{
    public static void Execute(Cpu cpu)
    {
        sbyte offset = (sbyte)cpu.Memory.Read(cpu.Regs.PC++);

        cpu.Cycles += 2;

        if (!cpu.Regs.IsZero)
        {
            ushort oldPc = cpu.Regs.PC;
            cpu.Regs.PC = (ushort)(cpu.Regs.PC + offset);
            cpu.Cycles++;

            if ((oldPc & 0xFF00) != (cpu.Regs.PC & 0xFF00))
                cpu.Cycles++;
        }
    }
}
