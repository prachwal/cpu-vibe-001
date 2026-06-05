using Z80.Core;

namespace Z80.Instructions.DdFd;

public static class DdFdCB
{
    private static ushort GetIndexedAddr(Cpu cpu, bool isIX)
    {
        ushort index = isIX ? cpu.Regs.IX : cpu.Regs.IY;
        sbyte d = cpu.FetchSignedByte();
        return (ushort)(index + d);
    }

    public static void RlcIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (bit7 ? 1 : 0));
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void RrcIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (bit0 ? 0x80 : 0));
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void RlIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool oldCarry = cpu.Regs.IsCarry;
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (oldCarry ? 1 : 0));
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void RrIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool oldCarry = cpu.Regs.IsCarry;
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (oldCarry ? 0x80 : 0));
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void SlaIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)(value << 1);
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void SraIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((sbyte)value >> 1);
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void SllIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | 0x01);
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void SrlIndexed(Cpu cpu, bool isIX, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)(value >> 1);
        cpu.Memory.Write(addr, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        if (reg != 6) WriteReg(cpu, reg, result);
        cpu.Cycles += 23;
    }

    public static void BitIndexed(Cpu cpu, bool isIX, int bit)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        bool bitSet = (value & (1 << bit)) != 0;
        cpu.Regs.SetFlag(CpuFlags.Zero, !bitSet);
        cpu.Regs.SetFlag(CpuFlags.Sign, bit == 7 && bitSet);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 20;
    }

    public static void ResIndexed(Cpu cpu, bool isIX, int bit, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        value = (byte)(value & ~(1 << bit));
        cpu.Memory.Write(addr, value);
        if (reg != 6) WriteReg(cpu, reg, value);
        cpu.Cycles += 23;
    }

    public static void SetIndexed(Cpu cpu, bool isIX, int bit, int reg)
    {
        ushort addr = GetIndexedAddr(cpu, isIX);
        byte value = cpu.Memory.Read(addr);
        value = (byte)(value | (1 << bit));
        cpu.Memory.Write(addr, value);
        if (reg != 6) WriteReg(cpu, reg, value);
        cpu.Cycles += 23;
    }

    private static void WriteReg(Cpu cpu, int reg, byte value)
    {
        switch (reg)
        {
            case 0: cpu.Regs.B = value; break;
            case 1: cpu.Regs.C = value; break;
            case 2: cpu.Regs.D = value; break;
            case 3: cpu.Regs.E = value; break;
            case 4: cpu.Regs.H = value; break;
            case 5: cpu.Regs.L = value; break;
            case 7: cpu.Regs.A = value; break;
        }
    }
}
