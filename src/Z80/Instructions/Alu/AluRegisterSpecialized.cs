using Z80.Core;

namespace Z80.Instructions.Alu;

/// <summary>
/// Specialized ALU A,r handlers for registers B,C,D,E,H,L,A (src != 6).
/// Avoids RegisterHelper.GetRegister switch and inlines flag computation.
/// </summary>
public static class AluRegisterSpecialized
{
    public static void Execute(Cpu cpu, byte opcode)
    {
        int op = (opcode >> 3) & 7;
        int src = opcode & 7;

        if (src == 6)
        {
            AluRegister.Execute(cpu, opcode);
            return;
        }

        switch (src)
        {
            case 0: ExecuteWithB(cpu, op); break;
            case 1: ExecuteWithC(cpu, op); break;
            case 2: ExecuteWithD(cpu, op); break;
            case 3: ExecuteWithE(cpu, op); break;
            case 4: ExecuteWithH(cpu, op); break;
            case 5: ExecuteWithL(cpu, op); break;
            case 7: ExecuteWithA(cpu, op); break;
        }

        cpu.Cycles += 4;
    }

    private static void ExecuteWithB(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.B;
        switch (op)
        {
            case 0: AddA_B(cpu, a, b); break;
            case 1: AdcA_B(cpu, a, b); break;
            case 2: SubA_B(cpu, a, b); break;
            case 3: SbcA_B(cpu, a, b); break;
            case 4: AndA_B(cpu, a, b); break;
            case 5: XorA_B(cpu, a, b); break;
            case 6: OrA_B(cpu, a, b); break;
            case 7: CpA_B(cpu, a, b); break;
        }
    }

    private static void ExecuteWithC(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.C;
        switch (op)
        {
            case 0: AddA_C(cpu, a, b); break;
            case 1: AdcA_C(cpu, a, b); break;
            case 2: SubA_C(cpu, a, b); break;
            case 3: SbcA_C(cpu, a, b); break;
            case 4: AndA_C(cpu, a, b); break;
            case 5: XorA_C(cpu, a, b); break;
            case 6: OrA_C(cpu, a, b); break;
            case 7: CpA_C(cpu, a, b); break;
        }
    }

    private static void ExecuteWithD(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.D;
        switch (op)
        {
            case 0: AddA_D(cpu, a, b); break;
            case 1: AdcA_D(cpu, a, b); break;
            case 2: SubA_D(cpu, a, b); break;
            case 3: SbcA_D(cpu, a, b); break;
            case 4: AndA_D(cpu, a, b); break;
            case 5: XorA_D(cpu, a, b); break;
            case 6: OrA_D(cpu, a, b); break;
            case 7: CpA_D(cpu, a, b); break;
        }
    }

    private static void ExecuteWithE(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.E;
        switch (op)
        {
            case 0: AddA_E(cpu, a, b); break;
            case 1: AdcA_E(cpu, a, b); break;
            case 2: SubA_E(cpu, a, b); break;
            case 3: SbcA_E(cpu, a, b); break;
            case 4: AndA_E(cpu, a, b); break;
            case 5: XorA_E(cpu, a, b); break;
            case 6: OrA_E(cpu, a, b); break;
            case 7: CpA_E(cpu, a, b); break;
        }
    }

    private static void ExecuteWithH(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.H;
        switch (op)
        {
            case 0: AddA_H(cpu, a, b); break;
            case 1: AdcA_H(cpu, a, b); break;
            case 2: SubA_H(cpu, a, b); break;
            case 3: SbcA_H(cpu, a, b); break;
            case 4: AndA_H(cpu, a, b); break;
            case 5: XorA_H(cpu, a, b); break;
            case 6: OrA_H(cpu, a, b); break;
            case 7: CpA_H(cpu, a, b); break;
        }
    }

    private static void ExecuteWithL(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.L;
        switch (op)
        {
            case 0: AddA_L(cpu, a, b); break;
            case 1: AdcA_L(cpu, a, b); break;
            case 2: SubA_L(cpu, a, b); break;
            case 3: SbcA_L(cpu, a, b); break;
            case 4: AndA_L(cpu, a, b); break;
            case 5: XorA_L(cpu, a, b); break;
            case 6: OrA_L(cpu, a, b); break;
            case 7: CpA_L(cpu, a, b); break;
        }
    }

    private static void ExecuteWithA(Cpu cpu, int op)
    {
        byte a = cpu.Regs.A;
        byte b = cpu.Regs.A;
        switch (op)
        {
            case 0: AddA_A(cpu, a, b); break;
            case 1: AdcA_A(cpu, a, b); break;
            case 2: SubA_A(cpu, a, b); break;
            case 3: SbcA_A(cpu, a, b); break;
            case 4: AndA_A(cpu, a, b); break;
            case 5: XorA_A(cpu, a, b); break;
            case 6: OrA_A(cpu, a, b); break;
            case 7: CpA_A(cpu, a, b); break;
        }
    }

    // ADD A,B
    private static void AddA_B(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,B
    private static void AdcA_B(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,B
    private static void SubA_B(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,B
    private static void SbcA_B(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,B
    private static void AndA_B(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,B
    private static void XorA_B(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,B
    private static void OrA_B(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,B
    private static void CpA_B(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }

    // ADD A,C
    private static void AddA_C(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,C
    private static void AdcA_C(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,C
    private static void SubA_C(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,C
    private static void SbcA_C(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,C
    private static void AndA_C(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,C
    private static void XorA_C(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,C
    private static void OrA_C(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,C
    private static void CpA_C(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }

    // ADD A,D
    private static void AddA_D(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,D
    private static void AdcA_D(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,D
    private static void SubA_D(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,D
    private static void SbcA_D(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,D
    private static void AndA_D(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,D
    private static void XorA_D(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,D
    private static void OrA_D(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,D
    private static void CpA_D(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }

    // ADD A,E
    private static void AddA_E(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,E
    private static void AdcA_E(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,E
    private static void SubA_E(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,E
    private static void SbcA_E(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,E
    private static void AndA_E(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,E
    private static void XorA_E(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,E
    private static void OrA_E(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,E
    private static void CpA_E(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }

    // ADD A,H
    private static void AddA_H(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,H
    private static void AdcA_H(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,H
    private static void SubA_H(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,H
    private static void SbcA_H(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,H
    private static void AndA_H(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,H
    private static void XorA_H(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,H
    private static void OrA_H(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,H
    private static void CpA_H(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }

    // ADD A,L
    private static void AddA_L(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,L
    private static void AdcA_L(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,L
    private static void SubA_L(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,L
    private static void SbcA_L(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,L
    private static void AndA_L(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,L
    private static void XorA_L(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,L
    private static void OrA_L(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,L
    private static void CpA_L(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }

    // ADD A,A
    private static void AddA_A(Cpu cpu, byte a, byte b)
    {
        int result = a + b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F)) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // ADC A,A
    private static void AdcA_A(Cpu cpu, byte a, byte b)
    {
        int carry = cpu.Regs.IsCarry ? 1 : 0;
        int result = a + b + carry;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result > 0xFF ? 0x01 : 0) |
            (((a & 0x0F) + (b & 0x0F) + carry) > 0x0F ? 0x10 : 0) |
            ((~(a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SUB A,A
    private static void SubA_A(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // SBC A,A
    private static void SbcA_A(Cpu cpu, byte a, byte b)
    {
        int borrow = cpu.Regs.IsCarry ? 1 : 0;
        int result = a - b - borrow;
        byte value = (byte)result;
        cpu.Regs.A = value;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F) - borrow) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (value & 0x28));
    }

    // AND A,A
    private static void AndA_A(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a & b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | 0x10 | (result & 0x28));
    }

    // XOR A,A
    private static void XorA_A(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a ^ b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // OR A,A
    private static void OrA_A(Cpu cpu, byte a, byte b)
    {
        byte result = (byte)(a | b);
        cpu.Regs.A = result;
        cpu.Regs.F = (byte)(Registers.GetSzpFlags(result) | (result & 0x28));
    }

    // CP A,A
    private static void CpA_A(Cpu cpu, byte a, byte b)
    {
        int result = a - b;
        byte value = (byte)result;
        cpu.Regs.F = (byte)(
            (value & 0x80) |
            (value == 0 ? 0x40 : 0) |
            (result < 0 ? 0x01 : 0) |
            (((a & 0x0F) - (b & 0x0F)) < 0 ? 0x10 : 0) |
            0x02 |
            (((a ^ b) & (a ^ result) & 0x80) != 0 ? 0x04 : 0) |
            (b & 0x28));
    }
}
