namespace Mos6502.Instructions.Sta;

/// <summary>
/// STA (zp,X) — Store Accumulator (Indexed Indirect)
/// Opcode: $81 | Size: 2 bytes | Cycles: 6
/// [ptr + X] = A
/// </summary>
public static class StaIndirectX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte ptr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte lo = cpu.Memory.Read(ptr);
        byte hi = cpu.Memory.Read((byte)((ptr + 1) & 0xFF));
        ushort addr = (ushort)((hi << 8) | lo);
        cpu.Memory.Write(addr, cpu.Regs.A);
        cpu.Cycles += 6;
    }
}
