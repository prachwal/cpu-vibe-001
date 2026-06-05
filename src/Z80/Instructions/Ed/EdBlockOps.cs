using Z80.Core;

namespace Z80.Instructions.Ed;

public static class Ldi
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte value = cpu.Memory.Read(cpu.Regs.DE);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.DE++;
        cpu.Regs.HL++;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 16;
    }
}

public static class Ldir
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte value = cpu.Memory.Read(cpu.Regs.DE);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.DE++;
        cpu.Regs.HL++;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        if (cpu.Regs.BC != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Ldd
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte value = cpu.Memory.Read(cpu.Regs.DE);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.DE--;
        cpu.Regs.HL--;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        cpu.Cycles += 16;
    }
}

public static class Lddr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte value = cpu.Memory.Read(cpu.Regs.DE);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.DE--;
        cpu.Regs.HL--;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        if (cpu.Regs.BC != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Cpi
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte hl = cpu.Memory.Read(cpu.Regs.HL);
        byte result = (byte)(a - hl);
        cpu.Regs.HL++;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((a ^ hl ^ result) & 0x10) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 16;
    }
}

public static class Cpir
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte hl = cpu.Memory.Read(cpu.Regs.HL);
        byte result = (byte)(a - hl);
        cpu.Regs.HL++;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((a ^ hl ^ result) & 0x10) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        if (cpu.Regs.BC != 0 && result != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Cpd
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte hl = cpu.Memory.Read(cpu.Regs.HL);
        byte result = (byte)(a - hl);
        cpu.Regs.HL--;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((a ^ hl ^ result) & 0x10) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 16;
    }
}

public static class Cpdr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        byte a = cpu.Regs.A;
        byte hl = cpu.Memory.Read(cpu.Regs.HL);
        byte result = (byte)(a - hl);
        cpu.Regs.HL--;
        cpu.Regs.BC--;
        cpu.Regs.SetFlag(CpuFlags.Sign, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((a ^ hl ^ result) & 0x10) != 0);
        cpu.Regs.SetFlag(CpuFlags.ParityOverflow, cpu.Regs.BC != 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        if (cpu.Regs.BC != 0 && result != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Ini
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.C);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.HL++;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 16;
    }
}

public static class Inir
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.C);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.HL++;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        if (cpu.Regs.B != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Ind
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.C);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.HL--;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 16;
    }
}

public static class Indr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.C);
        cpu.Memory.Write(cpu.Regs.HL, value);
        cpu.Regs.HL--;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        if (cpu.Regs.B != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Outi
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.HL);
        cpu.Memory.Write(cpu.Regs.C, value);
        cpu.Regs.HL++;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 16;
    }
}

public static class Outir
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.HL);
        cpu.Memory.Write(cpu.Regs.C, value);
        cpu.Regs.HL++;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        if (cpu.Regs.B != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}

public static class Outd
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.HL);
        cpu.Memory.Write(cpu.Regs.C, value);
        cpu.Regs.HL--;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        cpu.Cycles += 16;
    }
}

public static class Outdr
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.B--;
        byte value = cpu.Memory.Read(cpu.Regs.HL);
        cpu.Memory.Write(cpu.Regs.C, value);
        cpu.Regs.HL--;
        cpu.Regs.SetFlag(CpuFlags.Zero, cpu.Regs.B == 0);
        cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        if (cpu.Regs.B != 0)
        {
            cpu.Regs.PC -= 2;
            cpu.Cycles += 21;
        }
        else
        {
            cpu.Cycles += 16;
        }
    }
}
