namespace Mos6502.Instructions.Rol;

/// <summary>
/// ROL abs — Rotate Left (Absolute)
/// Opcode: $2E | Size: 3 bytes | Cycles: 6
/// Flags: N, Z, C
/// </summary>
public static class RolAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
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
