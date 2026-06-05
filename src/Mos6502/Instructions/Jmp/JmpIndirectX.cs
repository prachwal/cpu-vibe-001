namespace Mos6502.Instructions.Jmp;

/// <summary>
/// JMP (abs,X) — Jump Indirect,X (65C02)
/// Opcode: $7C | Size: 3 bytes | Cycles: 6
/// PC = Memory[abs + X] | (Memory[abs + X + 1] << 8)
/// No page boundary bug (unlike 6502 JMP (abs))
/// </summary>
public static class JmpIndirectX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort effective = (ushort)(addr + cpu.Regs.X);
        byte lo = cpu.Memory.Read(effective);
        byte hi = cpu.Memory.Read((ushort)(effective + 1));
        cpu.Regs.PC = (ushort)((hi << 8) | lo);
        cpu.Cycles += 6;
    }
}
