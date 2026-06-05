namespace CpuVibe.Instructions.Jmp;

/// <summary>
/// JMP (abs) — Jump Indirect
/// Opcode: $6C | Size: 3 bytes | Cycles: 5
/// PC = [addr] | ([addr+1] << 8)
/// </summary>
public static class JmpIndirect
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte lo = cpu.Memory.Read(addr);
        byte hi = cpu.Memory.Read((ushort)((addr & 0xFF00) | ((addr + 1) & 0x00FF)));
        cpu.Regs.PC = (ushort)((hi << 8) | lo);
        cpu.Cycles += 5;
    }
}
