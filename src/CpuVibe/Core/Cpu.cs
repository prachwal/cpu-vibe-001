using CpuVibe.Instructions;

public class Cpu
{
    public Registers Regs { get; } = new();
    public Memory Memory { get; } = new();
    public long Cycles { get; set; }
    public Throttle? Throttle { get; set; }

    public void Reset()
    {
        Regs.Reset();
        Memory.Reset();
        Cycles = 0;

        byte lo = Memory.Read(0xFFFC);
        byte hi = Memory.Read(0xFFFD);
        Regs.PC = (ushort)((hi << 8) | lo);
    }

    public ushort ReadAddress()
    {
        byte lo = Memory.Read(Regs.PC++);
        byte hi = Memory.Read(Regs.PC++);
        return (ushort)((hi << 8) | lo);
    }

    public void StackPush(byte value)
    {
        Memory.Write((ushort)(0x0100 + Regs.SP), value);
        Regs.SP--;
    }

    public byte StackPop()
    {
        Regs.SP++;
        return Memory.Read((ushort)(0x0100 + Regs.SP));
    }

    public void Step()
    {
        long before = Cycles;
        byte opcode = Memory.Read(Regs.PC++);
        InstructionTable.Handlers[opcode](this);
        Throttle?.AddCycles((byte)(Cycles - before));
    }
}
