namespace Mos6502.Instructions.Sbc;

/// <summary>
/// SBC zp — Subtract with Carry (Zero Page)
/// Opcode: $E5 | Size: 2 bytes | Cycles: 3
/// A = A - [zp] - !C
/// Flags: N, V, Z, C
/// </summary>
public static class SbcZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
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

        cpu.Cycles += 3;
    }
}
