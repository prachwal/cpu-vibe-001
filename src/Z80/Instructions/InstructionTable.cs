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
}
