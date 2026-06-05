using Z80.Core;

namespace Z80.Instructions.DdFd;

public static class DdFdPrefixHandler
{
    private static readonly InstructionHandler[] DdTable = new InstructionHandler[256];
    private static readonly InstructionHandler[] FdTable = new InstructionHandler[256];

    static DdFdPrefixHandler()
    {
        InitTable(DdTable, true);
        InitTable(FdTable, false);
    }

    public static void ExecuteDD(Cpu cpu, byte opcode)
    {
        byte subOpcode = cpu.FetchByte();
        DdTable[subOpcode](cpu, subOpcode);
    }

    public static void ExecuteFD(Cpu cpu, byte opcode)
    {
        byte subOpcode = cpu.FetchByte();
        FdTable[subOpcode](cpu, subOpcode);
    }

    public static void ExecuteDDCB(Cpu cpu, bool isIX)
    {
        sbyte d = cpu.FetchSignedByte();
        byte subOpcode = cpu.FetchByte();
        ushort addr = (ushort)((isIX ? cpu.Regs.IX : cpu.Regs.IY) + d);
        int reg = subOpcode & 7;
        int operation = (subOpcode >> 3) & 7;
        int bit = (subOpcode >> 6) & 3;

        if (subOpcode < 0x40)
        {
            byte value = cpu.Memory.Read(addr);
            switch (operation)
            {
                case 0: value = Rlc(cpu, value); break;
                case 1: value = Rrc(cpu, value); break;
                case 2: value = Rl(cpu, value); break;
                case 3: value = Rr(cpu, value); break;
                case 4: value = Sla(cpu, value); break;
                case 5: value = Sra(cpu, value); break;
                case 6: value = Sll(cpu, value); break;
                case 7: value = Srl(cpu, value); break;
            }
            cpu.Memory.Write(addr, value);
            if (reg != 6) WriteReg(cpu, reg, value);
            cpu.Cycles += 23;
        }
        else if (subOpcode < 0x80)
        {
            int bitNum = (subOpcode >> 3) & 7;
            byte value = cpu.Memory.Read(addr);
            bool bitSet = (value & (1 << bitNum)) != 0;
            cpu.Regs.SetFlag(CpuFlags.Zero, !bitSet);
            cpu.Regs.SetFlag(CpuFlags.Sign, bitNum == 7 && bitSet);
            cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
            cpu.Regs.SetFlag(CpuFlags.Subtract, false);
            cpu.Cycles += 20;
        }
        else if (subOpcode < 0xC0)
        {
            int bitNum = (subOpcode >> 3) & 7;
            byte value = cpu.Memory.Read(addr);
            value = (byte)(value & ~(1 << bitNum));
            cpu.Memory.Write(addr, value);
            if (reg != 6) WriteReg(cpu, reg, value);
            cpu.Cycles += 23;
        }
        else
        {
            int bitNum = (subOpcode >> 3) & 7;
            byte value = cpu.Memory.Read(addr);
            value = (byte)(value | (1 << bitNum));
            cpu.Memory.Write(addr, value);
            if (reg != 6) WriteReg(cpu, reg, value);
            cpu.Cycles += 23;
        }
    }

    private static byte Rlc(Cpu cpu, byte value)
    {
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (bit7 ? 1 : 0));
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        return result;
    }

    private static byte Rrc(Cpu cpu, byte value)
    {
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (bit0 ? 0x80 : 0));
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        return result;
    }

    private static byte Rl(Cpu cpu, byte value)
    {
        bool oldCarry = cpu.Regs.IsCarry;
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (oldCarry ? 1 : 0));
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        return result;
    }

    private static byte Rr(Cpu cpu, byte value)
    {
        bool oldCarry = cpu.Regs.IsCarry;
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (oldCarry ? 0x80 : 0));
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        return result;
    }

    private static byte Sla(Cpu cpu, byte value)
    {
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)(value << 1);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        return result;
    }

    private static byte Sra(Cpu cpu, byte value)
    {
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)((sbyte)value >> 1);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        return result;
    }

    private static byte Sll(Cpu cpu, byte value)
    {
        bool bit7 = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | 0x01);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit7);
        return result;
    }

    private static byte Srl(Cpu cpu, byte value)
    {
        bool bit0 = (value & 0x01) != 0;
        byte result = (byte)(value >> 1);
        cpu.Regs.SetSZPV(result);
        cpu.Regs.SetFlag(CpuFlags.Carry, bit0);
        return result;
    }

    private static void WriteReg(Cpu cpu, int reg, byte value)
    {
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
    }

    private static byte GetReg(Cpu cpu, int reg) => reg switch
    {
        0 => cpu.Regs.B, 1 => cpu.Regs.C, 2 => cpu.Regs.D, 3 => cpu.Regs.E,
        4 => cpu.Regs.H, 5 => cpu.Regs.L, 7 => cpu.Regs.A, _ => 0
    };

    private static ushort IndexedAddr(bool isIX, Cpu cpu)
    {
        ushort index = isIX ? cpu.Regs.IX : cpu.Regs.IY;
        sbyte d = cpu.FetchSignedByte();
        return (ushort)(index + d);
    }

    private static void InitTable(InstructionHandler[] table, bool isIX)
    {
        for (int i = 0; i < 256; i++)
            table[i] = NopDdFd.Execute;

        bool ix = isIX;

        table[0x09] = MakeAddIxIyRr(ix, 0);
        table[0x19] = MakeAddIxIyRr(ix, 1);
        table[0x29] = MakeAddIxIyRr(ix, 2);
        table[0x39] = MakeAddIxIyRr(ix, 3);

        table[0x21] = (cpu, op) => { ushort nn = cpu.FetchWord(); SetIndex(ix, cpu, nn); cpu.Cycles += 14; };
        table[0x2A] = (cpu, op) => { ushort nn = cpu.FetchWord(); ushort v = (ushort)(cpu.Memory.Read(nn) | (cpu.Memory.Read((ushort)(nn + 1)) << 8)); SetIndex(ix, cpu, v); cpu.Cycles += 20; };
        table[0x22] = (cpu, op) => { ushort nn = cpu.FetchWord(); ushort v = GetIndexVal(ix, cpu); cpu.Memory.Write(nn, (byte)(v & 0xFF)); cpu.Memory.Write((ushort)(nn + 1), (byte)(v >> 8)); cpu.Cycles += 20; };
        table[0x36] = (cpu, op) => { ushort addr = IndexedAddr(ix, cpu); byte n = cpu.FetchByte(); cpu.Memory.Write(addr, n); cpu.Cycles += 19; };

        table[0x34] = MakeIncDecIndexed(ix, true);
        table[0x35] = MakeIncDecIndexed(ix, false);

        table[0xE5] = (cpu, op) => { cpu.StackPush(GetIndexVal(ix, cpu)); cpu.Cycles += 15; };
        table[0xE1] = (cpu, op) => { SetIndex(ix, cpu, cpu.StackPop()); cpu.Cycles += 14; };
        table[0xE3] = MakeExSpIndexed(ix);

        table[0x46] = MakeLdRegIndexed(ix, 0);
        table[0x4E] = MakeLdRegIndexed(ix, 1);
        table[0x56] = MakeLdRegIndexed(ix, 2);
        table[0x5E] = MakeLdRegIndexed(ix, 3);
        table[0x66] = MakeLdRegIndexed(ix, 4);
        table[0x6E] = MakeLdRegIndexed(ix, 5);
        table[0x7E] = MakeLdRegIndexed(ix, 7);

        table[0x70] = MakeLdIndexedReg(ix, 0);
        table[0x71] = MakeLdIndexedReg(ix, 1);
        table[0x72] = MakeLdIndexedReg(ix, 2);
        table[0x73] = MakeLdIndexedReg(ix, 3);
        table[0x74] = MakeLdIndexedReg(ix, 4);
        table[0x75] = MakeLdIndexedReg(ix, 5);
        table[0x77] = MakeLdIndexedReg(ix, 7);

        table[0x86] = MakeAluIndexed(ix, 0);
        table[0x8E] = MakeAluIndexed(ix, 1);
        table[0x96] = MakeAluIndexed(ix, 2);
        table[0x9E] = MakeAluIndexed(ix, 3);
        table[0xA6] = MakeAluIndexed(ix, 4);
        table[0xAE] = MakeAluIndexed(ix, 5);
        table[0xB6] = MakeAluIndexed(ix, 6);
        table[0xBE] = MakeAluIndexed(ix, 7);

        table[0xCB] = (cpu, op) => ExecuteDDCB(cpu, ix);
    }

    private static ushort GetIndexVal(bool isIX, Cpu cpu) => isIX ? cpu.Regs.IX : cpu.Regs.IY;
    private static void SetIndex(bool isIX, Cpu cpu, ushort value)
    {
        if (isIX) cpu.Regs.IX = value; else cpu.Regs.IY = value;
    }

    private static InstructionHandler MakeAddIxIyRr(bool isIX, int reg16)
    {
        return (cpu, op) =>
        {
            ushort index = isIX ? cpu.Regs.IX : cpu.Regs.IY;
            ushort rr = reg16 switch { 0 => cpu.Regs.BC, 1 => cpu.Regs.DE, 2 => index, 3 => cpu.Regs.SP, _ => 0 };
            uint result = (uint)(index + rr);
            cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFFFF);
            cpu.Regs.SetFlag(CpuFlags.HalfCarry, ((index ^ rr ^ result) & 0x1000) != 0);
            cpu.Regs.SetFlag(CpuFlags.Subtract, false);
            SetIndex(isIX, cpu, (ushort)result);
            cpu.Cycles += 15;
        };
    }

    private static InstructionHandler MakeIncDecIndexed(bool isIX, bool inc)
    {
        return (cpu, op) =>
        {
            ushort addr = IndexedAddr(isIX, cpu);
            byte val = cpu.Memory.Read(addr);
            if (inc) val++; else val--;
            cpu.Memory.Write(addr, val);
            cpu.Regs.SetFlag(CpuFlags.Sign, (val & 0x80) != 0);
            cpu.Regs.SetFlag(CpuFlags.Zero, val == 0);
            cpu.Regs.SetFlag(CpuFlags.HalfCarry, inc ? (val & 0x0F) == 0 : (val & 0x0F) == 0x0F);
            cpu.Regs.SetFlag(CpuFlags.ParityOverflow, inc ? val == 0x80 : val == 0x7F);
            cpu.Regs.SetFlag(CpuFlags.Subtract, !inc);
            cpu.Cycles += 23;
        };
    }

    private static InstructionHandler MakeExSpIndexed(bool isIX)
    {
        return (cpu, op) =>
        {
            ushort spAddr = cpu.Regs.SP;
            byte lo = cpu.Memory.Read(spAddr);
            byte hi = cpu.Memory.Read((ushort)(spAddr + 1));
            ushort memVal = (ushort)((hi << 8) | lo);
            ushort idx = GetIndexVal(isIX, cpu);
            cpu.Memory.Write(spAddr, (byte)(idx & 0xFF));
            cpu.Memory.Write((ushort)(spAddr + 1), (byte)(idx >> 8));
            SetIndex(isIX, cpu, memVal);
            cpu.Cycles += 23;
        };
    }

    private static InstructionHandler MakeLdRegIndexed(bool isIX, int reg)
    {
        return (cpu, op) =>
        {
            ushort addr = IndexedAddr(isIX, cpu);
            SetReg(cpu, reg, cpu.Memory.Read(addr));
            cpu.Cycles += 19;
        };
    }

    private static InstructionHandler MakeLdIndexedReg(bool isIX, int reg)
    {
        return (cpu, op) =>
        {
            ushort addr = IndexedAddr(isIX, cpu);
            cpu.Memory.Write(addr, GetReg(cpu, reg));
            cpu.Cycles += 19;
        };
    }

    private static InstructionHandler MakeAluIndexed(bool isIX, int aluOp)
    {
        return (cpu, op) =>
        {
            ushort addr = IndexedAddr(isIX, cpu);
            byte value = cpu.Memory.Read(addr);
            switch (aluOp)
            {
                case 0: cpu.Regs.A = AluHelper.AddA(cpu, cpu.Regs.A, value, false); break;
                case 1: cpu.Regs.A = AluHelper.AddA(cpu, cpu.Regs.A, value, true); break;
                case 2: cpu.Regs.A = AluHelper.SubA(cpu, cpu.Regs.A, value, false); break;
                case 3: cpu.Regs.A = AluHelper.SubA(cpu, cpu.Regs.A, value, true); break;
                case 4: cpu.Regs.A = AluHelper.AndA(cpu, cpu.Regs.A, value); break;
                case 5: cpu.Regs.A = AluHelper.XorA(cpu, cpu.Regs.A, value); break;
                case 6: cpu.Regs.A = AluHelper.OrA(cpu, cpu.Regs.A, value); break;
                case 7: AluHelper.CpA(cpu, cpu.Regs.A, value); break;
            }
            cpu.Cycles += 19;
        };
    }

    private static void SetReg(Cpu cpu, int reg, byte value)
    {
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
    }
}
