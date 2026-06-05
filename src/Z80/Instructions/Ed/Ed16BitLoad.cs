using Z80.Core;

namespace Z80.Instructions.Ed;

public static class EdLdNnRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        int reg = (opcode >> 4) & 3;
        ushort value = reg switch
        {
            0 => cpu.Regs.BC,
            1 => cpu.Regs.DE,
            2 => cpu.Regs.HL,
            3 => cpu.Regs.SP,
            _ => 0
        };
        cpu.Memory.Write(nn, (byte)(value & 0xFF));
        cpu.Memory.Write((ushort)(nn + 1), (byte)(value >> 8));
        cpu.Cycles += 20;
    }
}

public static class EdLdRrNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        int reg = (opcode >> 4) & 3;
        byte lo = cpu.Memory.Read(nn);
        byte hi = cpu.Memory.Read((ushort)(nn + 1));
        ushort value = (ushort)((hi << 8) | lo);
        switch (reg)
        {
            case 0: cpu.Regs.BC = value; break;
            case 1: cpu.Regs.DE = value; break;
            case 2: cpu.Regs.HL = value; break;
            case 3: cpu.Regs.SP = value; break;
        }
        cpu.Cycles += 20;
    }
}

public static class LdIA
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.I = cpu.Regs.A;
        cpu.Cycles += 9;
    }
}

public static class LdRA
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.R = cpu.Regs.A;
        cpu.Cycles += 9;
    }
}

public static class LdAI
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.A = cpu.Regs.I;
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Iff2);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 9;
    }
}

public static class LdAR
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.A = cpu.Regs.R;
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Iff2);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 9;
    }
}

public static class RRD
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte hl = cpu.Memory.Read(cpu.Regs.HL);
        cpu.Regs.A = (byte)((a & 0xF0) | (hl & 0x0F));
        byte newHl = (byte)((hl >> 4) | (a << 4));
        cpu.Memory.Write(cpu.Regs.HL, newHl);
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, Registers.CalculateParity(cpu.Regs.A));
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 18;
    }
}

public static class RLD
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte hl = cpu.Memory.Read(cpu.Regs.HL);
        cpu.Regs.A = (byte)((a & 0xF0) | (hl >> 4));
        byte newHl = (byte)((hl << 4) | (a & 0x0F));
        cpu.Memory.Write(cpu.Regs.HL, newHl);
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, Registers.CalculateParity(cpu.Regs.A));
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 18;
    }
}
