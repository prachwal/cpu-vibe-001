using Z80.Core;

namespace Z80.Instructions.Ed;

public static class InAC
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte port = cpu.FetchByte();
        cpu.Regs.A = cpu.Memory.Read(port);
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, Registers.CalculateParity(cpu.Regs.A));
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 11;
    }
}

public static class OutCA
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte port = cpu.Regs.C;
        cpu.Memory.Write(port, cpu.Regs.A);
        cpu.Cycles += 12;
    }
}

public static class InRC
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int reg = (opcode >> 3) & 7;
        byte value = cpu.Memory.Read(cpu.Regs.C);
        cpu.Regs.SetFlag(CpuFlags.Sign, (value & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, Registers.CalculateParity(value));
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
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
        cpu.Cycles += 12;
    }
}

public static class OutCR
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int reg = (opcode >> 3) & 7;
        byte value = reg switch
        {
            0 => cpu.Regs.B,
            1 => cpu.Regs.C,
            2 => cpu.Regs.D,
            3 => cpu.Regs.E,
            4 => cpu.Regs.H,
            5 => cpu.Regs.L,
            7 => cpu.Regs.A,
            _ => 0
        };
        cpu.Memory.Write(cpu.Regs.C, value);
        cpu.Cycles += 12;
    }
}
