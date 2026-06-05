namespace Mos6502.Instructions.Tsb;

/// <summary>
/// TSB zp — Test and Set Bits
/// Opcode: $04 | Size: 2 bytes | Cycles: 5
/// Z = (A & Memory[zp]) == 0
/// Memory[zp] = Memory[zp] | A
/// Flags: Z
/// </summary>
public static class TsbZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte val = cpu.Memory.Read(zp);
        cpu.Regs.SetFlag(CpuFlags.Zero, (cpu.Regs.A & val) == 0);
        cpu.Memory.Write(zp, (byte)(val | cpu.Regs.A));
        cpu.Cycles += 5;
    }
}

/// <summary>
/// TSB abs — Test and Set Bits
/// Opcode: $0C | Size: 3 bytes | Cycles: 6
/// Z = (A & Memory[abs]) == 0
/// Memory[abs] = Memory[abs] | A
/// Flags: Z
/// </summary>
public static class TsbAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte val = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Zero, (cpu.Regs.A & val) == 0);
        cpu.Memory.Write(addr, (byte)(val | cpu.Regs.A));
        cpu.Cycles += 6;
    }
}
