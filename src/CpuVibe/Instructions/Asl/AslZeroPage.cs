namespace CpuVibe.Instructions.Asl;

/// <summary>
/// ASL zp — Arithmetic Shift Left (Zero Page)
/// Opcode: $06 | Size: 2 bytes | Cycles: 5
/// C = [zp] bit 7, [zp] = [zp] << 1
/// Flags: N, Z, C
/// </summary>
public static class AslZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte value = cpu.Memory.Read(addr);
        cpu.Regs.SetFlag(CpuFlags.Carry, (value & 0x80) != 0);
        value = (byte)(value << 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 5;
    }
}
