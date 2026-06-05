namespace CpuVibe.Instructions.Cpx;

/// <summary>
/// CPX #imm — Compare X Register (Immediate)
/// Opcode: $E0 | Size: 2 bytes | Cycles: 2
/// Compare: X - imm
/// Flags: N, Z, C
/// </summary>
public static class CpxImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);
        ushort result = (ushort)(cpu.Regs.X - operand);
        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)result == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, cpu.Regs.X >= operand);
        cpu.Cycles += 2;
    }
}
