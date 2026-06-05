namespace Z80.Core;

public class Cpu
{
    public Registers Regs { get; } = new();
    public Memory Memory { get; } = new();
    public long Cycles { get; set; }
    public bool Iff1 { get; set; }
    public bool Iff2 { get; set; }
    public byte InterruptMode { get; set; }
    public bool Halted { get; set; }

    public InstructionTable Table { get; }

    public Cpu()
    {
        Table = new InstructionTable();
    }

    public void Reset()
    {
        Regs.Reset();
        Memory.Reset();
        Cycles = 0;
        Iff1 = false;
        Iff2 = false;
        InterruptMode = 0;
        Halted = false;
    }

    public byte FetchByte()
    {
        byte value = Memory.Read(Regs.PC);
        Regs.PC++;
        return value;
    }

    public ushort FetchWord()
    {
        byte lo = FetchByte();
        byte hi = FetchByte();
        return (ushort)((hi << 8) | lo);
    }

    public sbyte FetchSignedByte()
    {
        return (sbyte)FetchByte();
    }

    public void StackPush(ushort value)
    {
        Regs.SP--;
        Memory.Write(Regs.SP, (byte)(value >> 8));
        Regs.SP--;
        Memory.Write(Regs.SP, (byte)(value & 0xFF));
    }

    public ushort StackPop()
    {
        byte lo = Memory.Read(Regs.SP);
        Regs.SP++;
        byte hi = Memory.Read(Regs.SP);
        Regs.SP++;
        return (ushort)((hi << 8) | lo);
    }

    public void Step()
    {
        if (Halted)
        {
            Cycles += 4;
            return;
        }

        byte opcode = FetchByte();
        Regs.R++;
        Table.Handlers[opcode](this);
    }
}
