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
}
