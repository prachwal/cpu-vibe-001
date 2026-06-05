namespace Mos6502.Instructions.Sbc;

/// <summary>
/// SBC abs,X — Subtract with Carry (Absolute,X)
/// Opcode: $FD | Size: 3 bytes | Cycles: 4 (+1 page crossing)
/// A = A - [addr + X] - !C
/// Flags: N, V, Z, C
/// </summary>
public static class SbcAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        cpu.Cycles += (byte)((addr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
        byte operand = cpu.Memory.Read(finalAddr);

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
