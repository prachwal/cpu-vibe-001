namespace CpuVibe.Instructions.Adc;

/// <summary>
/// ADC zp — Add with Carry (Zero Page)
/// Opcode: $65 | Size: 2 bytes | Cycles: 3
/// A = A + [zp] + C
/// </summary>
public static class AdcZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte operand = cpu.Memory.Read(addr);
        byte a = cpu.Regs.A;
        byte carry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;

        ushort result = (ushort)(a + operand + carry);

        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)(result & 0xFF) == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFF);
        cpu.Regs.SetFlag(CpuFlags.Overflow,
            (~(a ^ operand) & (a ^ result) & 0x80) != 0);

        cpu.Regs.A = (byte)(result & 0xFF);
        cpu.Cycles += 3;
    }
}
