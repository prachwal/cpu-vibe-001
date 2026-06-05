using Z80.Core;

namespace Z80.Instructions.Cb;

public static class BitHelper
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

    private static int CyclesForBitReg(int reg) => reg == 6 ? 12 : 8;
    private static int CyclesForResSetReg(int reg) => reg == 6 ? 15 : 8;

    public static void Test(Cpu cpu, int bit, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        bool bitSet = (value & (1 << bit)) != 0;
        cpu.Regs.SetFlag(CpuFlags.Zero, !bitSet);
        cpu.Regs.SetFlag(CpuFlags.Sign, bit == 7 && bitSet);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += (byte)CyclesForBitReg(reg);
    }

    public static void Reset(Cpu cpu, int bit, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        value = (byte)(value & ~(1 << bit));
        WriteRegister(cpu, reg, value);
        cpu.Cycles += (byte)CyclesForResSetReg(reg);
    }

    public static void Set(Cpu cpu, int bit, int reg)
    {
        byte value = ReadRegister(cpu, reg);
        value = (byte)(value | (1 << bit));
        WriteRegister(cpu, reg, value);
        cpu.Cycles += (byte)CyclesForResSetReg(reg);
    }
}
