namespace CpuVibe.Instructions.Sta;

/// <summary>
/// STA (zp),Y — Store Accumulator (Indirect Indexed)
/// Opcode: $91 | Size: 2 bytes | Cycles: 6
/// [ptr] + Y = A
/// </summary>
public static class StaIndirectY
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte lo = cpu.Memory.Read(zp);
        byte hi = cpu.Memory.Read((byte)((zp + 1) & 0xFF));
        ushort addr = (ushort)(((hi << 8) | lo) + cpu.Regs.Y);
        cpu.Memory.Write(addr, cpu.Regs.A);
        cpu.Cycles += 6;
    }
}
