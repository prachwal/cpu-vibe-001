namespace Mos6502.Instructions.Adc;

/// <summary>
/// ADC abs — Add with Carry (Absolute)
/// Opcode: $6D | Size: 3 bytes | Cycles: 4
/// A = A + [addr] + C
/// Flags: N, V, Z, C
/// </summary>
public static class AdcAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte operand = cpu.Memory.Read(addr);

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

        cpu.Cycles += 4;
    }
}
