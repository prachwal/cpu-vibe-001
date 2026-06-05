namespace CpuVibe.Instructions.Inc;

/// <summary>
/// INC abs — Increment Memory (Absolute)
/// Opcode: $EE | Size: 3 bytes | Cycles: 6
/// [addr] = [addr] + 1
/// Flags: N, Z
/// </summary>
public static class IncAbsolute
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        byte value = (byte)(cpu.Memory.Read(addr) + 1);
        cpu.Memory.Write(addr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 6;
    }
}
