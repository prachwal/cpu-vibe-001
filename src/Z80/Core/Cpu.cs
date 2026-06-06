using CpuBase;
using Z80.Instructions;

namespace Z80.Core;

/// <summary>
/// Zilog Z80 CPU emulator.
/// </summary>
public class Cpu : ICpu
{
    public Registers Regs { get; } = new();
    public Memory Memory { get; } = new();
    IMemory ICpu.Memory => Memory;
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

    public void Step()
    {
        if (Halted)
        {
            Cycles += 4;
            return;
        }

        byte opcode = FetchByte();
        Regs.R++;
        Table.Handlers[opcode](this, opcode);
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

    public void StackPush(byte value)
    {
        Regs.SP--;
        Memory.Write(Regs.SP, value);
    }

    public byte StackPopByte()
    {
        byte value = Memory.Read(Regs.SP);
        Regs.SP++;
        return value;
    }

    public void StackPush16(ushort value)
    {
        Regs.SP--;
        Memory.Write(Regs.SP, (byte)(value >> 8));
        Regs.SP--;
        Memory.Write(Regs.SP, (byte)(value & 0xFF));
    }

    public ushort StackPop16()
    {
        byte lo = Memory.Read(Regs.SP);
        Regs.SP++;
        byte hi = Memory.Read(Regs.SP);
        Regs.SP++;
        return (ushort)((hi << 8) | lo);
    }

    public ushort StackPop() => StackPop16();

    public void StackPush(ushort value)
    {
        StackPush16(value);
    }
}
