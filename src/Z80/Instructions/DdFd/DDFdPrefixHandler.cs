using Z80.Core;
using Z80.Instructions.Alu;
using Z80.Instructions.Control;
using Z80.Instructions.Ld;
using Z80.Instructions.Nop;

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
            table[i] = ExecuteFallback;

        bool ix = isIX;

        table[0x09] = MakeAddIxIyRr(ix, 0);
        table[0x19] = MakeAddIxIyRr(ix, 1);
        table[0x29] = MakeAddIxIyRr(ix, 2);
        table[0x39] = MakeAddIxIyRr(ix, 3);

        table[0x23] = (cpu, op) => { SetIndex(ix, cpu, (ushort)(GetIndexVal(ix, cpu) + 1)); cpu.Cycles += 10; };
        table[0x2B] = (cpu, op) => { SetIndex(ix, cpu, (ushort)(GetIndexVal(ix, cpu) - 1)); cpu.Cycles += 10; };

        // INC/DEC IXh/IXl (undocumented)
        table[0x24] = (cpu, op) => { SetIndexHigh(ix, cpu, AluHelper.Inc(cpu, GetIndexHigh(ix, cpu))); cpu.Cycles += 8; };
        table[0x2C] = (cpu, op) => { SetIndexLow(ix, cpu, AluHelper.Inc(cpu, GetIndexLow(ix, cpu))); cpu.Cycles += 8; };
        table[0x25] = (cpu, op) => { SetIndexHigh(ix, cpu, AluHelper.Dec(cpu, GetIndexHigh(ix, cpu))); cpu.Cycles += 8; };
        table[0x2D] = (cpu, op) => { SetIndexLow(ix, cpu, AluHelper.Dec(cpu, GetIndexLow(ix, cpu))); cpu.Cycles += 8; };

        // LD IXh/IXl,n (undocumented)
        table[0x26] = (cpu, op) => { SetIndexHigh(ix, cpu, cpu.FetchByte()); cpu.Cycles += 11; };
        table[0x2E] = (cpu, op) => { SetIndexLow(ix, cpu, cpu.FetchByte()); cpu.Cycles += 11; };

        table[0x21] = (cpu, op) => { ushort nn = cpu.FetchWord(); SetIndex(ix, cpu, nn); cpu.Cycles += 14; };
        table[0x2A] = (cpu, op) => { ushort nn = cpu.FetchWord(); ushort v = (ushort)(cpu.Memory.Read(nn) | (cpu.Memory.Read((ushort)(nn + 1)) << 8)); SetIndex(ix, cpu, v); cpu.Cycles += 20; };
        table[0x22] = (cpu, op) => { ushort nn = cpu.FetchWord(); ushort v = GetIndexVal(ix, cpu); cpu.Memory.Write(nn, (byte)(v & 0xFF)); cpu.Memory.Write((ushort)(nn + 1), (byte)(v >> 8)); cpu.Cycles += 20; };
        table[0x36] = (cpu, op) => { ushort addr = IndexedAddr(ix, cpu); byte n = cpu.FetchByte(); cpu.Memory.Write(addr, n); cpu.Cycles += 19; };

        table[0x34] = MakeIncDecIndexed(ix, true);
        table[0x35] = MakeIncDecIndexed(ix, false);

        table[0xE5] = (cpu, op) => { cpu.StackPush(GetIndexVal(ix, cpu)); cpu.Cycles += 15; };
        table[0xE1] = (cpu, op) => { SetIndex(ix, cpu, cpu.StackPop()); cpu.Cycles += 14; };
        table[0xE3] = MakeExSpIndexed(ix);
        table[0xE9] = (cpu, op) => { cpu.Regs.PC = GetIndexVal(ix, cpu); cpu.Cycles += 8; };
        table[0xF9] = (cpu, op) => { cpu.Regs.SP = GetIndexVal(ix, cpu); cpu.Cycles += 10; };

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

        // LD r,IXh (undocumented)
        table[0x44] = (cpu, op) => { cpu.Regs.B = GetIndexHigh(ix, cpu); cpu.Cycles += 8; };
        table[0x4C] = (cpu, op) => { cpu.Regs.C = GetIndexHigh(ix, cpu); cpu.Cycles += 8; };
        table[0x54] = (cpu, op) => { cpu.Regs.D = GetIndexHigh(ix, cpu); cpu.Cycles += 8; };
        table[0x5C] = (cpu, op) => { cpu.Regs.E = GetIndexHigh(ix, cpu); cpu.Cycles += 8; };
        table[0x7C] = (cpu, op) => { cpu.Regs.A = GetIndexHigh(ix, cpu); cpu.Cycles += 8; };

        // LD r,IXl (undocumented)
        table[0x45] = (cpu, op) => { cpu.Regs.B = GetIndexLow(ix, cpu); cpu.Cycles += 8; };
        table[0x4D] = (cpu, op) => { cpu.Regs.C = GetIndexLow(ix, cpu); cpu.Cycles += 8; };
        table[0x55] = (cpu, op) => { cpu.Regs.D = GetIndexLow(ix, cpu); cpu.Cycles += 8; };
        table[0x5D] = (cpu, op) => { cpu.Regs.E = GetIndexLow(ix, cpu); cpu.Cycles += 8; };
        table[0x7D] = (cpu, op) => { cpu.Regs.A = GetIndexLow(ix, cpu); cpu.Cycles += 8; };

        // LD IXh,r (undocumented)
        table[0x60] = (cpu, op) => { SetIndexHigh(ix, cpu, cpu.Regs.B); cpu.Cycles += 8; };
        table[0x61] = (cpu, op) => { SetIndexHigh(ix, cpu, cpu.Regs.C); cpu.Cycles += 8; };
        table[0x62] = (cpu, op) => { SetIndexHigh(ix, cpu, cpu.Regs.D); cpu.Cycles += 8; };
        table[0x63] = (cpu, op) => { SetIndexHigh(ix, cpu, cpu.Regs.E); cpu.Cycles += 8; };
        table[0x67] = (cpu, op) => { SetIndexHigh(ix, cpu, cpu.Regs.A); cpu.Cycles += 8; };

        // LD IXl,r (undocumented)
        table[0x68] = (cpu, op) => { SetIndexLow(ix, cpu, cpu.Regs.B); cpu.Cycles += 8; };
        table[0x69] = (cpu, op) => { SetIndexLow(ix, cpu, cpu.Regs.C); cpu.Cycles += 8; };
        table[0x6A] = (cpu, op) => { SetIndexLow(ix, cpu, cpu.Regs.D); cpu.Cycles += 8; };
        table[0x6B] = (cpu, op) => { SetIndexLow(ix, cpu, cpu.Regs.E); cpu.Cycles += 8; };
        table[0x6F] = (cpu, op) => { SetIndexLow(ix, cpu, cpu.Regs.A); cpu.Cycles += 8; };

        // LD IXh,IXl / LD IXl,IXh (undocumented)
        table[0x64] = (cpu, op) => { cpu.Cycles += 8; };
        table[0x65] = (cpu, op) => { SetIndexHigh(ix, cpu, GetIndexLow(ix, cpu)); cpu.Cycles += 8; };
        table[0x6C] = (cpu, op) => { SetIndexLow(ix, cpu, GetIndexHigh(ix, cpu)); cpu.Cycles += 8; };
        table[0x6D] = (cpu, op) => { cpu.Cycles += 8; };

        // ALU A,IXh (undocumented)
        table[0x84] = (cpu, op) => { cpu.Regs.A = AluHelper.AddA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu), false); cpu.Cycles += 8; };
        table[0x8C] = (cpu, op) => { cpu.Regs.A = AluHelper.AddA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu), true); cpu.Cycles += 8; };
        table[0x94] = (cpu, op) => { cpu.Regs.A = AluHelper.SubA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu), false); cpu.Cycles += 8; };
        table[0x9C] = (cpu, op) => { cpu.Regs.A = AluHelper.SubA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu), true); cpu.Cycles += 8; };
        table[0xA4] = (cpu, op) => { cpu.Regs.A = AluHelper.AndA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu)); cpu.Cycles += 8; };
        table[0xAC] = (cpu, op) => { cpu.Regs.A = AluHelper.XorA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu)); cpu.Cycles += 8; };
        table[0xB4] = (cpu, op) => { cpu.Regs.A = AluHelper.OrA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu)); cpu.Cycles += 8; };
        table[0xBC] = (cpu, op) => { AluHelper.CpA(cpu, cpu.Regs.A, GetIndexHigh(ix, cpu)); cpu.Cycles += 8; };

        // ALU A,IXl (undocumented)
        table[0x85] = (cpu, op) => { cpu.Regs.A = AluHelper.AddA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu), false); cpu.Cycles += 8; };
        table[0x8D] = (cpu, op) => { cpu.Regs.A = AluHelper.AddA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu), true); cpu.Cycles += 8; };
        table[0x95] = (cpu, op) => { cpu.Regs.A = AluHelper.SubA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu), false); cpu.Cycles += 8; };
        table[0x9D] = (cpu, op) => { cpu.Regs.A = AluHelper.SubA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu), true); cpu.Cycles += 8; };
        table[0xA5] = (cpu, op) => { cpu.Regs.A = AluHelper.AndA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu)); cpu.Cycles += 8; };
        table[0xAD] = (cpu, op) => { cpu.Regs.A = AluHelper.XorA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu)); cpu.Cycles += 8; };
        table[0xB5] = (cpu, op) => { cpu.Regs.A = AluHelper.OrA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu)); cpu.Cycles += 8; };
        table[0xBD] = (cpu, op) => { AluHelper.CpA(cpu, cpu.Regs.A, GetIndexLow(ix, cpu)); cpu.Cycles += 8; };

        table[0xCB] = (cpu, op) => ExecuteDDCB(cpu, ix);
    }

    private static void ExecuteFallback(Cpu cpu, byte opcode)
    {
        InstructionHandler? handler = GetBaseFallbackHandler(opcode);
        if (handler == null)
        {
            NopDdFd.Execute(cpu, opcode);
            return;
        }

        handler(cpu, opcode);
        cpu.Cycles += 4;
    }

    private static InstructionHandler? GetBaseFallbackHandler(byte opcode)
    {
        if (IsSafeLdRegisterToRegister(opcode))
            return LdRegisterToRegister.Execute;

        if (IsSafeAluRegister(opcode))
            return AluRegister.Execute;

        return opcode switch
        {
            0x00 => NopImplied.Execute,

            0x01 or 0x11 or 0x31 => LdRrNn.Execute,
            0x02 => LdBcFromA.Execute,
            0x03 or 0x13 or 0x33 => IncRr.Execute,
            0x04 or 0x0C or 0x14 or 0x1C or 0x3C => IncRegister.Execute,
            0x05 or 0x0D or 0x15 or 0x1D or 0x3D => DecRegister.Execute,
            0x06 or 0x0E or 0x16 or 0x1E or 0x3E => LdRegisterFromImmediate.Execute,
            0x07 => Rlca.Execute,
            0x08 => ExAfAf.Execute,
            0x0A => LdAFromBc.Execute,
            0x0B or 0x1B or 0x3B => DecRr.Execute,
            0x0F => Rrca.Execute,
            0x12 => LdDeFromA.Execute,
            0x17 => Rla.Execute,
            0x18 => JrD.Execute,
            0x1A => LdAFromDe.Execute,
            0x1F => Rra.Execute,
            0x20 or 0x28 or 0x30 or 0x38 => JrCcD.Execute,
            0x27 => Daa.Execute,
            0x2F => Cpl.Execute,
            0x32 => LdNnA.Execute,
            0x37 => Scf.Execute,
            0x3A => LdANn.Execute,
            0x3F => Ccf.Execute,

            0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 => RetCc.Execute,
            0xC1 or 0xD1 or 0xF1 => PopRr.Execute,
            0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA => JpCcNn.Execute,
            0xC3 => JpNn.Execute,
            0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC => CallCcNn.Execute,
            0xC5 or 0xD5 or 0xF5 => PushRr.Execute,
            0xC6 or 0xCE or 0xD6 or 0xDE or 0xE6 or 0xEE or 0xF6 or 0xFE => AluImmediate.Execute,
            0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF => RstN.Execute,
            0xC9 => Ret.Execute,
            0xCD => CallNn.Execute,
            0xD9 => Exx.Execute,
            0xF3 => Di.Execute,
            0xFB => Ei.Execute,
            _ => null
        };
    }

    private static bool IsSafeLdRegisterToRegister(byte opcode)
    {
        if (opcode < 0x40 || opcode > 0x7F || opcode == 0x76)
            return false;

        int dst = (opcode >> 3) & 7;
        int src = opcode & 7;
        return dst is not 4 and not 5 and not 6 && src is not 4 and not 5 and not 6;
    }

    private static bool IsSafeAluRegister(byte opcode)
    {
        if (opcode < 0x80 || opcode > 0xBF)
            return false;

        int src = opcode & 7;
        return src is 0 or 1 or 2 or 3 or 7;
    }

    private static ushort GetIndexVal(bool isIX, Cpu cpu) => isIX ? cpu.Regs.IX : cpu.Regs.IY;
    private static void SetIndex(bool isIX, Cpu cpu, ushort value)
    {
        if (isIX) cpu.Regs.IX = value; else cpu.Regs.IY = value;
    }

    private static byte GetIndexHigh(bool isIX, Cpu cpu) => (byte)((isIX ? cpu.Regs.IX : cpu.Regs.IY) >> 8);
    private static byte GetIndexLow(bool isIX, Cpu cpu) => (byte)((isIX ? cpu.Regs.IX : cpu.Regs.IY) & 0xFF);
    private static void SetIndexHigh(bool isIX, Cpu cpu, byte value)
    {
        ushort idx = isIX ? cpu.Regs.IX : cpu.Regs.IY;
        idx = (ushort)((idx & 0x00FF) | (value << 8));
        if (isIX) cpu.Regs.IX = idx; else cpu.Regs.IY = idx;
    }
    private static void SetIndexLow(bool isIX, Cpu cpu, byte value)
    {
        ushort idx = isIX ? cpu.Regs.IX : cpu.Regs.IY;
        idx = (ushort)((idx & 0xFF00) | value);
        if (isIX) cpu.Regs.IX = idx; else cpu.Regs.IY = idx;
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
