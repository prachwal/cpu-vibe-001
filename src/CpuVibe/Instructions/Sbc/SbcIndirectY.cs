namespace CpuVibe.Instructions.Sbc;

/// <summary>
/// SBC (zp),Y — Subtract with Carry (Indirect Indexed)
/// Opcode: $F1 | Size: 2 bytes | Cycles: 5 (+1 page crossing)
/// A = A - [ptr] + Y - !C
/// Flags: N, V, Z, C
/// </summary>
public static class SbcIndirectY
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte lo = cpu.Memory.Read(zp);
        byte hi = cpu.Memory.Read((byte)((zp + 1) & 0xFF));
        ushort baseAddr = (ushort)((hi << 8) | lo);
        ushort finalAddr = (ushort)(baseAddr + cpu.Regs.Y);
        cpu.Cycles += (byte)((baseAddr & 0xFF00) != (finalAddr & 0xFF00) ? 1 : 0);
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

        cpu.Cycles += 5;
    }
}
