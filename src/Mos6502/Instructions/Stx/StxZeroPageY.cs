namespace Mos6502.Instructions.Stx;

/// <summary>
/// STX zp,Y — Store X Register (Zero Page,Y)
/// Opcode: $96 | Size: 2 bytes | Cycles: 4
/// [(zp + Y) & 0xFF] = X
/// </summary>
public static class StxZeroPageY
{
    public static void Execute(Cpu cpu)
    {
        byte zp = cpu.Memory.Read(cpu.Regs.PC++);
        byte addr = (byte)((zp + cpu.Regs.Y) & 0xFF);
        cpu.Memory.Write(addr, cpu.Regs.X);
        cpu.Cycles += 4;
    }
}
