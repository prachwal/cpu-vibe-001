namespace Z80.Instructions;

public static class AluHelper
{
    public static byte AddA(Cpu cpu, byte a, byte b, bool withCarry)
    {
        int carry = withCarry && cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        int halfResult = (a & 0x0F) + (b & 0x0F) + carry;

        cpu.Regs.SetFlag(CpuFlags.Zero, (result & 0xFF) == 0);
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFF);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, halfResult > 0x0F);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow,
            (~(a ^ b) & (a ^ result) & 0x80) != 0);
        cpu.Regs.SetF3F5((byte)result);

        return (byte)result;
    }

    public static byte SubA(Cpu cpu, byte a, byte b, bool withBorrow)
    {
        int borrow = withBorrow && cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        int halfResult = (a & 0x0F) - (b & 0x0F) - borrow;

        cpu.Regs.SetFlag(CpuFlags.Zero, (result & 0xFF) == 0);
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, result < 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, halfResult < 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow,
            ((a ^ b) & (a ^ result) & 0x80) != 0);
        cpu.Regs.SetF3F5((byte)result);

        return (byte)result;
    }

    public static byte AndA(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Regs.SetFlag(CpuFlags.Carry, false);
        cpu.Regs.SetF3F5(result);
        return result;
    }

    public static byte OrA(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Regs.SetFlag(CpuFlags.Carry, false);
        cpu.Regs.SetF3F5(result);
        return result;
    }

    public static byte XorA(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Regs.SetFlag(CpuFlags.Carry, false);
        cpu.Regs.SetF3F5(result);
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
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, (value & 0x0F) == 0x0F);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, value == 0x7F);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Regs.SetF3F5(result);
        return result;
    }

    public static byte Dec(Cpu cpu, byte value)
    {
        byte result = (byte)(value - 1);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, (value & 0x0F) == 0x00);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, value == 0x80);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Regs.SetF3F5(result);
        return result;
    }
}
