using Z80.Core;

namespace Z80.Instructions.Ed;

public static class EDPrefixHandler
{
    private static readonly InstructionHandler[] EDTable = new InstructionHandler[256];

    static EDPrefixHandler()
    {
        for (int i = 0; i < 256; i++)
            EDTable[i] = NopEd.Execute;

        Init16BitLoad();
        Init16BitArithmetic();
        InitBlockOps();
        InitIo();
        InitBitGroup();
        InitInterruptMode();
    }

    public static void Execute(Cpu cpu, byte opcode)
    {
        byte subOpcode = cpu.FetchByte();
        EDTable[subOpcode](cpu, subOpcode);
    }

    private static int Reg16Index(int idx) => idx switch
    {
        0 => 0,
        1 => 1,
        2 => 2,
        3 => 3,
        _ => 0
    };

    private static void Init16BitLoad()
    {
        EDTable[0x43] = EdLdNnRr.Execute;
        EDTable[0x53] = EdLdNnRr.Execute;
        EDTable[0x63] = EdLdNnRr.Execute;
        EDTable[0x73] = EdLdNnRr.Execute;

        EDTable[0x4B] = EdLdRrNn.Execute;
        EDTable[0x5B] = EdLdRrNn.Execute;
        EDTable[0x6B] = EdLdRrNn.Execute;
        EDTable[0x7B] = EdLdRrNn.Execute;

        EDTable[0x47] = LdIA.Execute;
        EDTable[0x4F] = LdRA.Execute;
        EDTable[0x57] = LdAI.Execute;
        EDTable[0x5F] = LdAR.Execute;

        EDTable[0x67] = RRD.Execute;
        EDTable[0x6F] = RLD.Execute;

        EDTable[0xA0] = Ldi.Execute;
        EDTable[0xB0] = Ldir.Execute;
        EDTable[0xA8] = Ldd.Execute;
        EDTable[0xB8] = Lddr.Execute;
        EDTable[0xA1] = Cpi.Execute;
        EDTable[0xB1] = Cpir.Execute;
        EDTable[0xA9] = Cpd.Execute;
        EDTable[0xB9] = Cpdr.Execute;

        EDTable[0xA2] = Ini.Execute;
        EDTable[0xB2] = Inir.Execute;
        EDTable[0xAA] = Ind.Execute;
        EDTable[0xBA] = Indr.Execute;
        EDTable[0xA3] = Outi.Execute;
        EDTable[0xB3] = Outir.Execute;
        EDTable[0xAB] = Outd.Execute;
        EDTable[0xBB] = Outdr.Execute;
    }

    private static void Init16BitArithmetic()
    {
        EDTable[0x4A] = AdcHlRr.Execute;
        EDTable[0x5A] = AdcHlRr.Execute;
        EDTable[0x6A] = AdcHlRr.Execute;
        EDTable[0x7A] = AdcHlRr.Execute;

        EDTable[0x42] = SbcHlRr.Execute;
        EDTable[0x52] = SbcHlRr.Execute;
        EDTable[0x62] = SbcHlRr.Execute;
        EDTable[0x72] = SbcHlRr.Execute;

        EDTable[0x44] = Neg.Execute;
        EDTable[0x4C] = Neg.Execute;
        EDTable[0x54] = Neg.Execute;
        EDTable[0x5C] = Neg.Execute;
        EDTable[0x64] = Neg.Execute;
        EDTable[0x6C] = Neg.Execute;
        EDTable[0x74] = Neg.Execute;
        EDTable[0x7C] = Neg.Execute;

        EDTable[0x45] = Retn.Execute;
        EDTable[0x55] = Retn.Execute;
        EDTable[0x65] = Retn.Execute;
        EDTable[0x75] = Retn.Execute;

        EDTable[0x4D] = Reti.Execute;
        EDTable[0x5D] = Reti.Execute;
        EDTable[0x6D] = Reti.Execute;
        EDTable[0x7D] = Reti.Execute;

        EDTable[0x66] = Im0.Execute;
        EDTable[0x76] = Im0.Execute;

        EDTable[0x56] = Im1.Execute;
        EDTable[0x7E] = Im2.Execute;

        EDTable[0x46] = Im0.Execute;
        EDTable[0x5E] = Im2.Execute;
        EDTable[0x7E] = Im2.Execute;
    }

    private static void InitBlockOps()
    {
    }

    private static void InitIo()
    {
        EDTable[0x78] = InAC.Execute;
        EDTable[0x79] = OutCA.Execute;

        EDTable[0x40] = InRC.Execute;
        EDTable[0x48] = InRC.Execute;
        EDTable[0x50] = InRC.Execute;
        EDTable[0x58] = InRC.Execute;
        EDTable[0x60] = InRC.Execute;
        EDTable[0x68] = InRC.Execute;
        EDTable[0x70] = InRC.Execute;

        EDTable[0x41] = OutCR.Execute;
        EDTable[0x49] = OutCR.Execute;
        EDTable[0x51] = OutCR.Execute;
        EDTable[0x59] = OutCR.Execute;
        EDTable[0x61] = OutCR.Execute;
        EDTable[0x69] = OutCR.Execute;
        EDTable[0x71] = OutCR.Execute;
    }

    private static void InitBitGroup()
    {
        for (int bit = 0; bit < 8; bit++)
        {
            int b = bit;
            if (b != 7)
            {
                EDTable[0x40 + b * 8] = InRC.Execute;
                EDTable[0x41 + b * 8] = OutCR.Execute;
            }
        }
    }

    private static void InitInterruptMode()
    {
    }
}
