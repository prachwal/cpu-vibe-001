namespace CpuVibe.Instructions.Rol;

/// <summary>
/// ROL zp — Rotate Left (Zero Page)
/// Opcode: $26 | Size: 2 bytes | Cycles: 5
/// Flags: N, Z, C
/// </summary>
public static class RolZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte value = cpu.Memory.Read(addr);
        byte oldCarry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x80) != 0);
        value = (byte)((value << 1) | oldCarry);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 5;
    }
}
