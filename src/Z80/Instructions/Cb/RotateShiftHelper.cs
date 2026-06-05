using Z80.Core;

namespace Z80.Instructions.Cb;

public static class RotateShiftHelper
{
    private static byte ReadRegister(Cpu cpu, int reg) => reg switch
    {
        0 => cpu.Regs.B,
        1 => cpu.Regs.C,
        2 => cpu.Regs.D,
        3 => cpu.Regs.E,
        4 => cpu.Regs.H,
        5 => cpu.Regs.L,
        6 => cpu.Memory.Read(cpu.Regs.HL),
        7 => cpu.Regs.A,
        _ => 0
    };

    private static void WriteRegister(Cpu cpu, int reg, byte value)
    {
        switch (reg)
        {
            case 0: cpu.Regs.B = value; break;
            case 1: cpu.Regs.C = value; break;
            case 2: cpu.Regs.D = value; break;
            case 3: cpu.Regs.E = value; break;
            case 4: cpu.Regs.H = value; break;
            case 5: cpu.Regs.L = value; break;
            case 6: cpu.Memory.Write(cpu.Regs.HL, value); break;
            case 7: cpu.Regs.A = value; break;
        }
    }

    private static int CyclesForReg(int reg) => reg == 6 ? 15 : 8;

    public static void Rlc(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (bit7 ? 1 : 0));
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Rrc(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (bit0 ? 0x80 : 0));
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Rl(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool oldCarry = cpu.Regs.IsCarry;
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (oldCarry ? 1 : 0));
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Rr(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool oldCarry = cpu.Regs.IsCarry;
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (oldCarry ? 0x80 : 0));
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Sla(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)(value << 1);
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Sra(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((sbyte)value >> 1);
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Sll(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | 0x01);
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }

    public static void Srl(Cpu cpu, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)(value >> 1);
        WriteRegister(cpu, reg, result);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        cpu.Cycles += (byte)CyclesForReg(reg);
    }
}
