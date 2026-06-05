namespace Mos6502.Instructions.Dec;

/// <summary>
/// DEC abs — Decrement Memory (Absolute)
/// Opcode: $CE | Size: 3 bytes | Cycles: 6
/// [addr] = [addr] - 1
/// Flags: N, Z
/// </summary>
public static class DecAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte value = (byte)(cpu.Memory.Read(addr) - 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 6;
    }
}
