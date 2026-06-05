namespace CpuVibe.Instructions.Ror;

/// <summary>
/// ROR abs,X — Rotate Right (Absolute,X)
/// Opcode: $7E | Size: 3 bytes | Cycles: 7
/// Flags: N, Z, C
/// </summary>
public static class RorAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        byte value = cpu.Memory.Read(finalAddr);
        byte oldCarry = cpu.Regs.IsCarry ? (byte)0x80 : (byte)0x00;
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x01) != 0);
        value = (byte)((value >> 1) | oldCarry);
        cpu.Memory.Write(finalAddr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 7;
    }
}
