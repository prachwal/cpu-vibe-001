using CpuBase;
using Mos6502.Instructions;

namespace Mos6502.Core;

/// <summary>
/// MOS 6502 CPU emulator.
/// </summary>
public class Cpu : ICpu
{
    public Registers Regs { get; } = new();
    public Memory Memory { get; private set; }
    IMemory ICpu.Memory => Memory;
    public long Cycles { get; set; }
    public bool IrqAsserted { get; set; }
    public Throttle? Throttle { get; set; }
    public CpuVariant Variant { get; }
    public InstructionTable Table { get; }

    public Cpu(CpuVariant variant = CpuVariant.Mos6502)
    {
        Memory = new Memory();
        Variant = variant;
        Table = InstructionTable.Create(variant);
    }

    public Cpu(Memory memory, CpuVariant variant = CpuVariant.Mos6502)
    {
        Memory = memory;
        Variant = variant;
        Table = InstructionTable.Create(variant);
    }

    public void Reset()
    {
        Regs.Reset();
        Memory.Reset();
        Cycles = 0;

        byte lo = Memory.Read(0xFFFC);
        byte hi = Memory.Read(0xFFFD);
        Regs.PC = (ushort)((hi << 8) | lo);
    }

    public void Step()
    {
        if (IrqAsserted && !Regs.IsInterruptDisabled)
        {
            ServiceIrq();
            return;
        }

        long before = Cycles;
        byte opcode = Memory.Read(Regs.PC++);
        Table.Handlers[opcode](this);
        Throttle?.AddCycles((byte)(Cycles - before));
    }

    public void ServiceIrq()
    {
        StackPush((byte)(Regs.PC >> 8));
        StackPush((byte)(Regs.PC & 0xFF));
        StackPush((byte)(Regs.P & ~CpuFlags.Break));
        Regs.SetFlag(CpuFlags.Interrupt, true);

        byte lo = Memory.Read(0xFFFE);
        byte hi = Memory.Read(0xFFFF);
        Regs.PC = (ushort)((hi << 8) | lo);
        Cycles += 7;
    }

    public byte FetchByte()
    {
        return Memory.Read(Regs.PC++);
    }

    public ushort FetchWord()
    {
        byte lo = Memory.Read(Regs.PC++);
        byte hi = Memory.Read(Regs.PC++);
        return (ushort)((hi << 8) | lo);
    }

    public ushort ReadAddress()
    {
        return FetchWord();
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

    public void AdcBcd(byte operand)
    {
        byte a = Regs.A;
        byte carry = Regs.IsCarry ? (byte)1 : (byte)0;

        ushort binaryResult = (ushort)(a + operand + carry);

        int lo = (a & 0x0F) + (operand & 0x0F) + carry;
        if (lo > 9) lo += 6;

        int hi = (a >> 4) + (operand >> 4) + (lo > 0x0F ? 1 : 0);
        if (hi > 9) hi += 6;

        byte bcdResult = (byte)(((hi & 0x0F) << 4) | (lo & 0x0F));

        Regs.SetFlag(CpuFlags.Zero, bcdResult == 0);
        Regs.SetFlag(CpuFlags.Negative, (binaryResult & 0x80) != 0);
        Regs.SetFlag(CpuFlags.Overflow,
            (~(a ^ operand) & (a ^ binaryResult) & 0x80) != 0);
        Regs.SetFlag(CpuFlags.Carry, hi > 0x0F);

        Regs.A = bcdResult;
    }

    public void SbcBcd(byte operand)
    {
        byte a = Regs.A;
        byte carry = Regs.IsCarry ? (byte)0 : (byte)1;

        ushort binaryResult = (ushort)(a - operand - carry);

        int lo = (a & 0x0F) - (operand & 0x0F) - carry;
        if (lo < 0) lo -= 6;

        int hi = (a >> 4) - (operand >> 4) - (lo < 0 ? 1 : 0);
        if (hi < 0) hi -= 6;

        byte bcdResult = (byte)(((hi & 0x0F) << 4) | (lo & 0x0F));

        Regs.SetFlag(CpuFlags.Zero, bcdResult == 0);
        Regs.SetFlag(CpuFlags.Negative, (binaryResult & 0x80) != 0);
        Regs.SetFlag(CpuFlags.Overflow,
            ((a ^ operand) & (a ^ binaryResult) & 0x80) != 0);
        Regs.SetFlag(CpuFlags.Carry, binaryResult <= 0xFF);

        Regs.A = bcdResult;
    }
}
