using Z80.Core;

namespace Z80.Instructions;

public static class AluHelper
{
    public static byte AddA(Cpu cpu, byte a, byte b, bool withCarry)
    {
        int carry = withCarry && cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        int halfResult = (a & 0x0F) + (b & 0x0F) + carry;

        cpu.Regs.F = (byte)(
            (value & (byte)CpuFlags.Sign) |
            (value == 0 ? (byte)CpuFlags.Zero : 0) |
            (result > 0xFF ? (byte)CpuFlags.Carry : 0) |
            (halfResult > 0x0F ? (byte)CpuFlags.HalfCarry : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? (byte)CpuFlags.ParityOverflow : 0) |
            (value & 0x28));

        return value;
    }

    public static byte SubA(Cpu cpu, byte a, byte b, bool withBorrow)
    {
        int borrow = withBorrow && cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        int halfResult = (a & 0x0F) - (b & 0x0F) - borrow;

        cpu.Regs.F = (byte)(
            (value & (byte)CpuFlags.Sign) |
            (value == 0 ? (byte)CpuFlags.Zero : 0) |
            (result < 0 ? (byte)CpuFlags.Carry : 0) |
            (halfResult < 0 ? (byte)CpuFlags.HalfCarry : 0) |
            (byte)CpuFlags.Subtract |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? (byte)CpuFlags.ParityOverflow : 0) |
            (value & 0x28));

        return value;
    }

    public static byte AndA(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);

        cpu.Regs.F = (byte)(
            Registers.GetSzpFlags(result) |
            (byte)CpuFlags.HalfCarry |
            (result & 0x28));

        return result;
    }

    public static byte OrA(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);

        cpu.Regs.F = (byte)(
            Registers.GetSzpFlags(result) |
            (result & 0x28));

        return result;
    }

    public static byte XorA(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);

        cpu.Regs.F = (byte)(
            Registers.GetSzpFlags(result) |
            (result & 0x28));

        return result;
    }

    public static void CpA(Cpu cpu, byte a, byte b)
    {
        SubA(cpu, a, b, false);
        cpu.Regs.SetF3F5(b);
    }

    public static byte Inc(Cpu cpu, byte value)
    {
        byte result = (byte)(value + 1);

        cpu.Regs.F = (byte)(
            (result & (byte)CpuFlags.Sign) |
            (result == 0 ? (byte)CpuFlags.Zero : 0) |
            ((value & 0x0F) == 0x0F ? (byte)CpuFlags.HalfCarry : 0) |
            (value == 0x7F ? (byte)CpuFlags.ParityOverflow : 0) |
            (cpu.Regs.F & (byte)CpuFlags.Carry) |
            (result & 0x28));

        return result;
    }

    public static byte Dec(Cpu cpu, byte value)
    {
        byte result = (byte)(value - 1);

        cpu.Regs.F = (byte)(
            (result & (byte)CpuFlags.Sign) |
            (result == 0 ? (byte)CpuFlags.Zero : 0) |
            ((value & 0x0F) == 0x00 ? (byte)CpuFlags.HalfCarry : 0) |
            (value == 0x80 ? (byte)CpuFlags.ParityOverflow : 0) |
            (byte)CpuFlags.Subtract |
            (cpu.Regs.F & (byte)CpuFlags.Carry) |
            (result & 0x28));

        return result;
    }
}
