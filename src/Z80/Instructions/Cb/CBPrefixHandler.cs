using Z80.Core;

namespace Z80.Instructions.Cb;

public static class CBPrefixHandler
{
    private static readonly InstructionHandler[] CBTable = new InstructionHandler[256];

    static CBPrefixHandler()
    {
        InitRotateShift();
        InitBit();
        InitRes();
        InitSet();
    }

    public static void Execute(Cpu cpu, byte opcode)
    {
        byte subOpcode = cpu.FetchByte();
        CBTable[subOpcode](cpu, subOpcode);
    }

    private static void InitRotateShift()
    {
        for (int reg = 0; reg < 8; reg++)
        {
            int r = reg;
            byte baseOp = (byte)(reg);

            CBTable[baseOp + 0x00] = (cpu, op) => RotateShiftHelper.Rlc(cpu, r);
            CBTable[baseOp + 0x08] = (cpu, op) => RotateShiftHelper.Rrc(cpu, r);
            CBTable[baseOp + 0x10] = (cpu, op) => RotateShiftHelper.Rl(cpu, r);
            CBTable[baseOp + 0x18] = (cpu, op) => RotateShiftHelper.Rr(cpu, r);
            CBTable[baseOp + 0x20] = (cpu, op) => RotateShiftHelper.Sla(cpu, r);
            CBTable[baseOp + 0x28] = (cpu, op) => RotateShiftHelper.Sra(cpu, r);
            CBTable[baseOp + 0x30] = (cpu, op) => RotateShiftHelper.Sll(cpu, r);
            CBTable[baseOp + 0x38] = (cpu, op) => RotateShiftHelper.Srl(cpu, r);
        }
    }

    private static void InitBit()
    {
        for (int bit = 0; bit < 8; bit++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int b = bit;
                int r = reg;
                byte opcode = (byte)(0x40 + bit * 8 + reg);
                CBTable[opcode] = (cpu, op) => BitHelper.Test(cpu, b, r);
            }
        }
    }

    private static void InitRes()
    {
        for (int bit = 0; bit < 8; bit++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int b = bit;
                int r = reg;
                byte opcode = (byte)(0x80 + bit * 8 + reg);
                CBTable[opcode] = (cpu, op) => BitHelper.Reset(cpu, b, r);
            }
        }
    }

    private static void InitSet()
    {
        for (int bit = 0; bit < 8; bit++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int b = bit;
                int r = reg;
                byte opcode = (byte)(0xC0 + bit * 8 + reg);
                CBTable[opcode] = (cpu, op) => BitHelper.Set(cpu, b, r);
            }
        }
    }
}
