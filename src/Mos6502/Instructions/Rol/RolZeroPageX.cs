namespace Mos6502.Instructions.Rol;

/// <summary>
/// ROL zp,X — Rotate Left (Zero Page,X)
/// Opcode: $36 | Size: 2 bytes | Cycles: 6
/// Flags: N, Z, C
/// </summary>
public static class RolZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte value = cpu.Memory.Read(addr);
        byte oldCarry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x80) != 0);
        value = (byte)((value << 1) | oldCarry);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 6;
    }
}
