namespace Mos6502.Instructions.Adc;

/// <summary>
/// ADC (zp,X) — Add with Carry (Indirect,X)
/// Opcode: $61 | Size: 2 bytes | Cycles: 6
/// A = A + [addr] + C
/// Flags: N, V, Z, C
/// </summary>
public static class AdcIndirectX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte ptr = (byte)(zp + cpu.Regs.X);
        byte lo = cpu.Memory.Read(ptr);
        byte hi = cpu.Memory.Read((byte)(ptr + 1));
        ushort addr = (ushort)((hi << 8) | lo);
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

        cpu.Cycles += 6;
    }
}
