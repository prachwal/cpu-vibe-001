namespace CpuVibe.Instructions.Sbc;

/// <summary>
/// SBC abs — Subtract with Carry (Absolute)
/// Opcode: $ED | Size: 3 bytes | Cycles: 4
/// A = A - [addr] - !C
/// Flags: N, V, Z, C
/// </summary>
public static class SbcAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte operand = cpu.Memory.Read(addr);
        byte a = cpu.Regs.A;
        byte carry = cpu.Regs.IsCarry ? (byte)0 : (byte)1;

        ushort result = (ushort)(a - operand - carry);

        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)(result & 0xFF) == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, result <= 0xFF);
        cpu.Regs.SetFlag(CpuFlags.Overflow,
            ((a ^ operand) & (a ^ result) & 0x80) != 0);

        cpu.Regs.A = (byte)(result & 0xFF);
        cpu.Cycles += 4;
    }
}
