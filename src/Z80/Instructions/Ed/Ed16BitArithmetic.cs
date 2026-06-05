using Z80.Core;

namespace Z80.Instructions.Ed;

public static class AdcHlRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int reg = (opcode >> 4) & 3;
        ushort rr = reg switch
        {
            0 => cpu.Regs.BC,
            1 => cpu.Regs.DE,
            2 => cpu.Regs.HL,
            3 => cpu.Regs.SP,
            _ => 0
        };
        uint hl = cpu.Regs.HL;
        uint result = hl + rr + (cpu.Regs.IsCarry ? 1u : 0u);
        cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFFFF);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((hl ^ rr ^ result) & 0x1000) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, (~(hl ^ rr) & (hl ^ result) & 0x8000) != 0);
        cpu.Regs.HL = (ushort)result;
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.HL & 0x8000) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.HL == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 15;
    }
}

public static class SbcHlRr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int reg = (opcode >> 4) & 3;
        ushort rr = reg switch
        {
            0 => cpu.Regs.BC,
            1 => cpu.Regs.DE,
            2 => cpu.Regs.HL,
            3 => cpu.Regs.SP,
            _ => 0
        };
        uint hl = cpu.Regs.HL;
        uint result = hl - rr - (cpu.Regs.IsCarry ? 1u : 0u);
        cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFFFF);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((hl ^ rr ^ result) & 0x1000) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, ((hl ^ rr) & (hl ^ result) & 0x8000) != 0);
        cpu.Regs.HL = (ushort)result;
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.HL & 0x8000) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.HL == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 15;
    }
}

public static class Neg
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte old = cpu.Regs.A;
        cpu.Regs.A = (byte)(0 - cpu.Regs.A);
        cpu.Regs.SetFlag(CpuFlags.Carry, old != 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, (old & 0x0F) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, old == 0x80);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Regs.SetFlag(CpuFlags.Sign, (cpu.Regs.A & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.A == 0);
        cpu.Cycles += 8;
    }
}

public static class Retn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.PC = cpu.StackPop();
        cpu.Iff1 = cpu.Iff2;
        cpu.Cycles += 14;
    }
}

public static class Reti
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.PC = cpu.StackPop();
        cpu.Iff1 = cpu.Iff2;
        cpu.Cycles += 14;
    }
}

public static class Im0
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.InterruptMode = 0;
        cpu.Cycles += 8;
    }
}

public static class Im1
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.InterruptMode = 1;
        cpu.Cycles += 8;
    }
}

public static class Im2
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.InterruptMode = 2;
        cpu.Cycles += 8;
    }
}
