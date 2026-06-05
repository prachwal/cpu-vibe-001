using Z80.Instructions.Alu;
using Z80.Instructions.Ld;
using Z80.Instructions.Nop;

namespace Z80.Instructions;

public class InstructionTable
{
    public InstructionHandler[] Handlers = new InstructionHandler[256];

    public InstructionTable()
    {
        for (int i = 0; i < 256; i++)
            Handlers[i] = NopImplied.Execute;

        InitNop();
        InitLd();
        InitLd16Bit();
        InitPushPop();
        InitExchange();
        InitAlu();
        InitAlu16Bit();
        InitRotate();
    }

    private void InitNop()
    {
        Handlers[0x00] = NopImplied.Execute;
    }

    private void InitLd()
    {
        for (int op = 0x40; op <= 0x7F; op++)
        {
            if (op == 0x76) continue;
            Handlers[op] = LdRegisterToRegister.Execute;
        }

        Handlers[0x06] = LdRegisterFromImmediate.Execute;
        Handlers[0x0E] = LdRegisterFromImmediate.Execute;
        Handlers[0x16] = LdRegisterFromImmediate.Execute;
        Handlers[0x1E] = LdRegisterFromImmediate.Execute;
        Handlers[0x26] = LdRegisterFromImmediate.Execute;
        Handlers[0x2E] = LdRegisterFromImmediate.Execute;
        Handlers[0x3E] = LdRegisterFromImmediate.Execute;

        Handlers[0x36] = LdHlFromImmediate.Execute;

        Handlers[0x02] = LdAFromBc.Execute;
        Handlers[0x12] = LdAFromDe.Execute;
        Handlers[0x0A] = LdBcFromA.Execute;
        Handlers[0x1A] = LdDeFromA.Execute;

        Handlers[0x76] = Halt.Execute;
    }

    private void InitLd16Bit()
    {
        Handlers[0x01] = LdRrNn.Execute;
        Handlers[0x11] = LdRrNn.Execute;
        Handlers[0x21] = LdRrNn.Execute;
        Handlers[0x31] = LdRrNn.Execute;

        Handlers[0x22] = LdNnHl.Execute;
        Handlers[0x2A] = LdHlNn.Execute;
        Handlers[0x32] = LdNnA.Execute;
        Handlers[0x3A] = LdANn.Execute;

        Handlers[0xF9] = LdSpHl.Execute;
    }

    private void InitPushPop()
    {
        Handlers[0xC5] = PushRr.Execute;
        Handlers[0xD5] = PushRr.Execute;
        Handlers[0xE5] = PushRr.Execute;
        Handlers[0xF5] = PushRr.Execute;

        Handlers[0xC1] = PopRr.Execute;
        Handlers[0xD1] = PopRr.Execute;
        Handlers[0xE1] = PopRr.Execute;
        Handlers[0xF1] = PopRr.Execute;
    }

    private void InitExchange()
    {
        Handlers[0xEB] = ExDeHl.Execute;
        Handlers[0x08] = ExAfAf.Execute;
        Handlers[0xD9] = Exx.Execute;
        Handlers[0xE3] = ExSpHl.Execute;
    }

    private void InitAlu()
    {
        for (int op = 0x80; op <= 0xBF; op++)
            Handlers[op] = AluRegister.Execute;

        Handlers[0xC6] = AluImmediate.Execute;
        Handlers[0xCE] = AluImmediate.Execute;
        Handlers[0xD6] = AluImmediate.Execute;
        Handlers[0xDE] = AluImmediate.Execute;
        Handlers[0xE6] = AluImmediate.Execute;
        Handlers[0xEE] = AluImmediate.Execute;
        Handlers[0xF6] = AluImmediate.Execute;
        Handlers[0xFE] = AluImmediate.Execute;

        Handlers[0x04] = IncRegister.Execute;
        Handlers[0x0C] = IncRegister.Execute;
        Handlers[0x14] = IncRegister.Execute;
        Handlers[0x1C] = IncRegister.Execute;
        Handlers[0x24] = IncRegister.Execute;
        Handlers[0x2C] = IncRegister.Execute;
        Handlers[0x34] = IncRegister.Execute;
        Handlers[0x3C] = IncRegister.Execute;

        Handlers[0x05] = DecRegister.Execute;
        Handlers[0x0D] = DecRegister.Execute;
        Handlers[0x15] = DecRegister.Execute;
        Handlers[0x1D] = DecRegister.Execute;
        Handlers[0x25] = DecRegister.Execute;
        Handlers[0x2D] = DecRegister.Execute;
        Handlers[0x35] = DecRegister.Execute;
        Handlers[0x3D] = DecRegister.Execute;
    }

    private void InitAlu16Bit()
    {
        Handlers[0x09] = AddHlRr.Execute;
        Handlers[0x19] = AddHlRr.Execute;
        Handlers[0x29] = AddHlRr.Execute;
        Handlers[0x39] = AddHlRr.Execute;

        Handlers[0x03] = IncRr.Execute;
        Handlers[0x13] = IncRr.Execute;
        Handlers[0x23] = IncRr.Execute;
        Handlers[0x33] = IncRr.Execute;

        Handlers[0x0B] = DecRr.Execute;
        Handlers[0x1B] = DecRr.Execute;
        Handlers[0x2B] = DecRr.Execute;
        Handlers[0x3B] = DecRr.Execute;
    }

    private void InitRotate()
    {
        Handlers[0x07] = Rlca.Execute;
        Handlers[0x0F] = Rrca.Execute;
        Handlers[0x17] = Rla.Execute;
        Handlers[0x1F] = Rra.Execute;
        Handlers[0x27] = Daa.Execute;
        Handlers[0x2F] = Cpl.Execute;
        Handlers[0x37] = Scf.Execute;
        Handlers[0x3F] = Ccf.Execute;
    }
}
