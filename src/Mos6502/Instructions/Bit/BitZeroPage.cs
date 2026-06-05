namespace Mos6502.Instructions.Bit;

/// <summary>
/// BIT zp — Test Memory Bits
/// Opcode: $24 | Size: 2 bytes | Cycles: 3
/// Z = A & [zp], N = [zp] bit 7, V = [zp] bit 6
/// </summary>
public static class BitZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte value = cpu.Memory.Read(addr);
        byte result = (byte)(cpu.Regs.A & value);
        cpu.Regs.SetFlag(CpuFlags.Zero, result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Overflow, (value & 0x40) != 0);
        cpu.Cycles += 3;
    }
}
