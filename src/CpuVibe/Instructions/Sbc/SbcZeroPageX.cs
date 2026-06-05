namespace CpuVibe.Instructions.Sbc;

/// <summary>
/// SBC zp,X — Subtract with Carry (Zero Page,X)
/// Opcode: $F5 | Size: 2 bytes | Cycles: 4
/// A = A - [(zp + X) & 0xFF] - !C
/// Flags: N, V, Z, C
/// </summary>
public static class SbcZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte operand = cpu.Memory.Read(addr);

        if (cpu.Regs.P.HasFlag(CpuFlags.Decimal))
        {
            cpu.SbcBcd(operand);
        }
        else
        {
            byte a = cpu.Regs.A;
            byte carry = cpu.Regs.IsCarry ? (byte)0 : (byte)1;
            ushort result = (ushort)(a - operand - carry);

            cpu.Regs.SetFlag(CpuFlags.Zero, (byte)(result & 0xFF) == 0);
            cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
            cpu.Regs.SetFlag(CpuFlags.Carry, result <= 0xFF);
            cpu.Regs.SetFlag(CpuFlags.Overflow,
                ((a ^ operand) & (a ^ result) & 0x80) != 0);

            cpu.Regs.A = (byte)(result & 0xFF);
        }

        cpu.Cycles += 4;
    }
}
