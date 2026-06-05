namespace CpuVibe.Instructions.Bvs;

/// <summary>
/// BVS rel — Branch if Overflow Set
/// Opcode: $70 | Size: 2 bytes | Cycles: 2 (3 if taken, 4 if page crossing)
/// Branch if V == 1
/// </summary>
public static class BvsRelative
{
    public static void Execute(Cpu cpu)
    {
        sbyte offset = (sbyte)cpu.Memory.Read(cpu.Regs.PC++);
        if (cpu.Regs.IsOverflow)
        {
            ushort oldPC = cpu.Regs.PC;
            cpu.Regs.PC = (ushort)(cpu.Regs.PC + offset);
            cpu.Cycles += 1;
            if ((oldPC & 0xFF00) != (cpu.Regs.PC & 0xFF00))
                cpu.Cycles += 1;
        }
        cpu.Cycles += 2;
    }
}
