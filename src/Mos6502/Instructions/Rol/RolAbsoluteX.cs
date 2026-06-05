namespace Mos6502.Instructions.Rol;

/// <summary>
/// ROL abs,X — Rotate Left (Absolute,X)
/// Opcode: $3E | Size: 3 bytes | Cycles: 7
/// Flags: N, Z, C
/// </summary>
public static class RolAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        byte value = cpu.Memory.Read(finalAddr);
        byte oldCarry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x80) != 0);
        value = (byte)((value << 1) | oldCarry);
        cpu.Memory.Write(finalAddr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 7;
    }
}
