namespace Z80.Instructions;

public static class RegisterHelper
{
    public static byte GetRegister(Cpu cpu, int index)
    {
        return index switch
        {
            0 => cpu.Regs.B,
            1 => cpu.Regs.C,
            2 => cpu.Regs.D,
            3 => cpu.Regs.E,
            4 => cpu.Regs.H,
            5 => cpu.Regs.L,
            6 => cpu.Memory.Read(cpu.Regs.HL),
            7 => cpu.Regs.A,
            _ => throw new ArgumentException($"Invalid register index: {index}")
        };
    }

    public static void SetRegister(Cpu cpu, int index, byte value)
    {
        switch (index)
        {
            case 0: cpu.Regs.B = value; break;
            case 1: cpu.Regs.C = value; break;
            case 2: cpu.Regs.D = value; break;
            case 3: cpu.Regs.E = value; break;
            case 4: cpu.Regs.H = value; break;
            case 5: cpu.Regs.L = value; break;
            case 6: cpu.Memory.Write(cpu.Regs.HL, value); break;
            case 7: cpu.Regs.A = value; break;
            default: throw new ArgumentException($"Invalid register index: {index}");
        }
    }
}
