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
    }

    private void InitNop()
    {
        Handlers[0x00] = NopImplied.Execute;
    }
}
