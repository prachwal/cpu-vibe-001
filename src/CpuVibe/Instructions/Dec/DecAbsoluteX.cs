namespace CpuVibe.Instructions.Dec;

/// <summary>
/// DEC abs,X — Decrement Memory (Absolute,X)
/// Opcode: $DE | Size: 3 bytes | Cycles: 7
/// [addr + X] = [addr + X] - 1
/// Flags: N, Z
/// </summary>
public static class DecAbsoluteX
{
    public static void Execute(Cpu cpu)
    {
        ushort addr = cpu.ReadAddress();
        ushort finalAddr = (ushort)(addr + cpu.Regs.X);
        byte value = (byte)(cpu.Memory.Read(finalAddr) - 1);
        cpu.Memory.Write(finalAddr, value);
        cpu.Regs.SetFlag(CpuFlags.Zero, value == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
        cpu.Cycles += 7;
    }
}
