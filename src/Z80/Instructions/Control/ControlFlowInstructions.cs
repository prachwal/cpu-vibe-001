namespace Z80.Instructions.Control;

public static class ConditionHelper
{
    public static bool Check(Cpu cpu, int cc)
    {
        return cc switch
        {
            0 => !cpu.Regs.IsZero,
            1 => cpu.Regs.IsZero,
            2 => !cpu.Regs.IsCarry,
            3 => cpu.Regs.IsCarry,
            4 => !cpu.Regs.IsParityOverflow,
            5 => cpu.Regs.IsParityOverflow,
            6 => !cpu.Regs.IsSign,
            7 => cpu.Regs.IsSign,
            _ => false
        };
    }
}

/// <summary>
/// JP nn — Jump unconditional
/// Opcode: $C3 | Size: 3 bytes | Cycles: 10
/// </summary>
public static class JpNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        cpu.Regs.PC = nn;
        cpu.Cycles += 10;
    }
}

/// <summary>
/// JP cc, nn — Jump conditional
/// Opcode: $C2,$CA,$D2,$DA,$E2,$EA | Size: 3 bytes | Cycles: 10 (taken), 10 (not taken)
/// </summary>
public static class JpCcNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        int cc = (opcode >> 3) & 7;
        if (ConditionHelper.Check(cpu, cc))
            cpu.Regs.PC = nn;
        cpu.Cycles += 10;
    }
}

/// <summary>
/// JP (HL) — Jump to address in HL
/// Opcode: $E9 | Size: 1 byte | Cycles: 4
/// </summary>
public static class JpHl
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.PC = cpu.Regs.HL;
        cpu.Cycles += 4;
    }
}

/// <summary>
/// JR d — Jump relative
/// Opcode: $18 | Size: 2 bytes | Cycles: 12
/// </summary>
public static class JrD
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        sbyte d = cpu.FetchSignedByte();
        cpu.Regs.PC = (ushort)(cpu.Regs.PC + d);
        cpu.Cycles += 12;
    }
}

/// <summary>
/// JR cc, d — Jump relative conditional
/// Opcode: $20,$28,$30,$38 | Size: 2 bytes | Cycles: 12 (taken), 7 (not taken)
/// </summary>
public static class JrCcD
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        sbyte d = cpu.FetchSignedByte();
        int cc = (opcode >> 3) & 3;
        if (ConditionHelper.Check(cpu, cc))
        {
            cpu.Regs.PC = (ushort)(cpu.Regs.PC + d);
            cpu.Cycles += 12;
        }
        else
        {
            cpu.Cycles += 7;
        }
    }
}

/// <summary>
/// DJNZ d — Decrement B and jump relative
/// Opcode: $10 | Size: 2 bytes | Cycles: 13 (taken), 8 (not taken)
/// </summary>
public static class DjnzD
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        sbyte d = cpu.FetchSignedByte();
        cpu.Regs.B--;
        if (cpu.Regs.B != 0)
        {
            cpu.Regs.PC = (ushort)(cpu.Regs.PC + d);
            cpu.Cycles += 13;
        }
        else
        {
            cpu.Cycles += 8;
        }
    }
}

/// <summary>
/// CALL nn — Call subroutine
/// Opcode: $CD | Size: 3 bytes | Cycles: 17
/// </summary>
public static class CallNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        cpu.StackPush(cpu.Regs.PC);
        cpu.Regs.PC = nn;
        cpu.Cycles += 17;
    }
}

/// <summary>
/// CALL cc, nn — Call conditional
/// Opcode: $C4,$CC,$D4,$DC,$E4,$EC | Size: 3 bytes | Cycles: 17 (taken), 10 (not taken)
/// </summary>
public static class CallCcNn
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        ushort nn = cpu.FetchWord();
        int cc = (opcode >> 3) & 7;
        if (ConditionHelper.Check(cpu, cc))
        {
            cpu.StackPush(cpu.Regs.PC);
            cpu.Regs.PC = nn;
            cpu.Cycles += 17;
        }
        else
        {
            cpu.Cycles += 10;
        }
    }
}

/// <summary>
/// RET — Return from subroutine
/// Opcode: $C9 | Size: 1 byte | Cycles: 10
/// </summary>
public static class Ret
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Regs.PC = cpu.StackPop();
        cpu.Cycles += 10;
    }
}

/// <summary>
/// RET cc — Return conditional
/// Opcode: $C0,$C8,$D0,$D8,$E0,$E8 | Size: 1 byte | Cycles: 11 (taken), 5 (not taken)
/// </summary>
public static class RetCc
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int cc = (opcode >> 3) & 7;
        if (ConditionHelper.Check(cpu, cc))
        {
            cpu.Regs.PC = cpu.StackPop();
            cpu.Cycles += 11;
        }
        else
        {
            cpu.Cycles += 5;
        }
    }
}

/// <summary>
/// RST n — Restart (CALL to page zero)
/// Opcode: $C7,$CF,$D7,$DF,$E7,$EF,$F7,$FF | Size: 1 byte | Cycles: 11
/// </summary>
public static class RstN
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.StackPush(cpu.Regs.PC);
        cpu.Regs.PC = (ushort)(opcode & 0x38);
        cpu.Cycles += 11;
    }
}

/// <summary>
/// DI — Disable interrupts
/// Opcode: $F3 | Size: 1 byte | Cycles: 4
/// </summary>
public static class Di
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Iff1 = false;
        cpu.Iff2 = false;
        cpu.Cycles += 4;
    }
}

/// <summary>
/// EI — Enable interrupts
/// Opcode: $FB | Size: 1 byte | Cycles: 4
/// </summary>
public static class Ei
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        cpu.Iff1 = true;
        cpu.Iff2 = true;
        cpu.Cycles += 4;
    }
}
