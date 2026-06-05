namespace Mos6502.Instructions.Dec;

/// <summary>
/// DEC zp,X — Decrement Memory (Zero Page,X)
/// Opcode: $D6 | Size: 2 bytes | Cycles: 6
/// [(zp + X) & 0xFF] = [(zp + X) & 0xFF] - 1
/// Flags: N, Z
/// </summary>
public static class DecZeroPageX
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.X) & 0xFF);
        byte value = (byte)(cpu.Memory.Read(addr) - 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 6;
    }
}
