namespace CpuVibe.Instructions.Bcc;

/// <summary>
/// BCC rel — Branch if Carry Clear
/// Opcode: $90 | Size: 2 bytes | Cycles: 2 (3 if taken, 4 if page crossing)
/// Branch if C == 0
/// </summary>
public static class BccRelative
{
    public static void Execute(Cpu cpu)
    {
        sbyte offset = (sbyte)cpu.Memory.Read(cpu.Regs.PC++);
        if (!cpu.Regs.IsCarry)
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
