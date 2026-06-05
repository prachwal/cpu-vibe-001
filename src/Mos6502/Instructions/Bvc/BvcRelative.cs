namespace Mos6502.Instructions.Bvc;

/// <summary>
/// BVC rel — Branch if Overflow Clear
/// Opcode: $50 | Size: 2 bytes | Cycles: 2 (3 if taken, 4 if page crossing)
/// Branch if V == 0
/// </summary>
public static class BvcRelative
{
    public static void Execute(Cpu cpu)
    {
        sbyte offset = (sbyte)cpu.Memory.Read(cpu.Regs.PC++);
        if (!cpu.Regs.IsOverflow)
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
