namespace Mos6502.Instructions.Dec;

/// <summary>
/// DEC zp — Decrement Memory (Zero Page)
/// Opcode: $C6 | Size: 2 bytes | Cycles: 5
/// [zp] = [zp] - 1
/// Flags: N, Z
/// </summary>
public static class DecZeroPage
{
    public static void Execute(Cpu cpu)
    {
        byte addr = cpu.Memory.Read(cpu.Regs.PC++);
        byte value = (byte)(cpu.Memory.Read(addr) - 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 5;
    }
}
