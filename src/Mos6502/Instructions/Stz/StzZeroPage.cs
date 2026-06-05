namespace Mos6502.Instructions.Stz;

/// <summary>
/// STZ zp — Store Zero to Zero Page
/// Opcode: $64 | Size: 2 bytes | Cycles: 3
/// Memory[zp] = 0
/// Flags: none
/// </summary>
public static class StzZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Memory.Write(zp, 0);
        cpu.Cycles += 3;
    }
}

/// <summary>
/// STZ zp,X — Store Zero to Zero Page,X
/// Opcode: $74 | Size: 2 bytes | Cycles: 4
/// Memory[(zp + X) & 0xFF] = 0
/// Flags: none
/// </summary>
public static class StzZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Memory.Write((byte)(zp + cpu.Regs.X), 0);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// STZ abs — Store Zero to Absolute
/// Opcode: $9C | Size: 3 bytes | Cycles: 4
/// Memory[abs] = 0
/// Flags: none
/// </summary>
public static class StzAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        cpu.Memory.Write(addr, 0);
        cpu.Cycles += 4;
    }
}

/// <summary>
/// STZ abs,X — Store Zero to Absolute,X
/// Opcode: $9E | Size: 3 bytes | Cycles: 5
/// Memory[abs + X] = 0
/// Flags: none
/// </summary>
public static class StzAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort effective = (ushort)(addr + cpu.Regs.X);
        cpu.Memory.Write(effective, 0);
        cpu.Cycles += 5;
    }
}
