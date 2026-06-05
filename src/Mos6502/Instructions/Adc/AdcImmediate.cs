namespace Mos6502.Instructions.Adc;

/// <summary>
/// ADC #imm — Add with Carry (Immediate)
/// Opcode: $69 | Size: 2 bytes | Cycles: 2
/// A = A + imm + C
/// Flags: N, V, Z, C
/// </summary>
public static class AdcImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);

        if (cpu.Regs.P.HasFlag(CpuFlags.Decimal))
        {
            cpu.AdcBcd(operand);
        }
        else
        {
            byte a = cpu.Regs.A;
            byte carry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;
            ushort result = (ushort)(a + operand + carry);

            cpu.Regs.SetFlag(CpuFlags.Zero, (byte)(result & 0xFF) == 0);
            cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
            cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFF);
            cpu.Regs.SetFlag(CpuFlags.Overflow,
                (~(a ^ operand) & (a ^ result) & 0x80) != 0);

            cpu.Regs.A = (byte)(result & 0xFF);
        }

        cpu.Cycles += 2;
    }
}
