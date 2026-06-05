namespace Z80.Tests;

public class AluTests
{
    private readonly Cpu _cpu = new();

    private void Setup(byte opcode, byte a, byte operand)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = a;
        _cpu.Memory.Write(0x0100, opcode);
        _cpu.Memory.Write(0x0101, operand);
        _cpu.Cycles = 0;
    }

    [Fact]
    public void AddA_B_AddsBToA()
    {
        Setup(0x80, 0x01, 0x00);
        _cpu.Regs.B = 0x02;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x03);
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void AddA_B_SetsCarryOnOverflow()
    {
        Setup(0x80, 0xFF, 0x00);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void AddA_B_SetsHalfCarry()
    {
        Setup(0x80, 0x0F, 0x00);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.IsHalfCarry.Should().BeTrue();
        _cpu.Regs.A.Should().Be(0x10);
    }

    [Fact]
    public void AddA_B_SetsZeroFlag()
    {
        Setup(0x80, 0x00, 0x00);
        _cpu.Regs.B = 0x00;

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void AddA_B_SetsSignFlag()
    {
        Setup(0x80, 0x80, 0x00);
        _cpu.Regs.B = 0x00;

        _cpu.Step();

        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void AddA_B_SetsOverflowFlag()
    {
        Setup(0x80, 0x7F, 0x00);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.IsParityOverflow.Should().BeTrue();
        _cpu.Regs.A.Should().Be(0x80);
    }

    [Fact]
    public void AddA_N_UsesImmediate()
    {
        Setup(0xC6, 0x10, 0x20);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x30);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void AdcA_B_AddsWithCarry()
    {
        Setup(0x88, 0x01, 0x00);
        _cpu.Regs.B = 0x02;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x04);
    }

    [Fact]
    public void SubA_B_SubtractsBFromA()
    {
        Setup(0x90, 0x05, 0x00);
        _cpu.Regs.B = 0x03;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x02);
        _cpu.Regs.IsSubtract.Should().BeTrue();
    }

    [Fact]
    public void SubA_B_SetsCarryOnBorrow()
    {
        Setup(0x90, 0x00, 0x00);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xFF);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void SbcA_B_SubtractsWithBorrow()
    {
        Setup(0x98, 0x05, 0x00);
        _cpu.Regs.B = 0x02;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x02);
    }

    [Fact]
    public void AndA_B_PerformsBitwiseAnd()
    {
        Setup(0xA0, 0xAB, 0x00);
        _cpu.Regs.B = 0x0F;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x0B);
        _cpu.Regs.IsHalfCarry.Should().BeTrue();
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void XorA_B_PerformsBitwiseXor()
    {
        Setup(0xA8, 0xFF, 0x00);
        _cpu.Regs.B = 0xFF;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void OrA_B_PerformsBitwiseOr()
    {
        Setup(0xB0, 0xF0, 0x00);
        _cpu.Regs.B = 0x0F;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xFF);
        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void CpA_B_CompareEqual()
    {
        Setup(0xB8, 0x42, 0x00);
        _cpu.Regs.B = 0x42;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CpA_B_CompareLess()
    {
        Setup(0xB8, 0x10, 0x00);
        _cpu.Regs.B = 0x20;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x10);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void IncB_IncrementsB()
    {
        Setup(0x04, 0x00, 0x00);
        _cpu.Regs.B = 0x0F;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x10);
        _cpu.Regs.IsHalfCarry.Should().BeTrue();
    }

    [Fact]
    public void IncB_SetsZeroOnOverflow()
    {
        Setup(0x04, 0x00, 0x00);
        _cpu.Regs.B = 0xFF;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void IncB_SetsOverflowOn0x7F()
    {
        Setup(0x04, 0x00, 0x00);
        _cpu.Regs.B = 0x7F;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x80);
        _cpu.Regs.IsParityOverflow.Should().BeTrue();
        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void DecB_DecrementsB()
    {
        Setup(0x05, 0x00, 0x00);
        _cpu.Regs.B = 0x10;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x0F);
        _cpu.Regs.IsHalfCarry.Should().BeTrue();
    }

    [Fact]
    public void DecB_SetsZeroOnUnderflow()
    {
        Setup(0x05, 0x00, 0x00);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void DecB_SetsOverflowOn0x80()
    {
        Setup(0x05, 0x00, 0x00);
        _cpu.Regs.B = 0x80;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x7F);
        _cpu.Regs.IsParityOverflow.Should().BeTrue();
    }

    [Fact]
    public void IncA_IncrementsAccumulator()
    {
        Setup(0x3C, 0x00, 0x00);
        _cpu.Regs.A = 0x41;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void DecA_DecrementsAccumulator()
    {
        Setup(0x3D, 0x00, 0x00);
        _cpu.Regs.A = 0x42;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x41);
    }

    [Fact]
    public void IncHl_IncrementsMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0x80;
        _cpu.Regs.L = 0x00;
        _cpu.Memory.Write(0x0100, 0x34);
        _cpu.Memory.Write(0x8000, 0x0F);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x10);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void DecHl_DecrementsMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0x80;
        _cpu.Regs.L = 0x00;
        _cpu.Memory.Write(0x0100, 0x35);
        _cpu.Memory.Write(0x8000, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x0F);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void AddA_HL_AddsFromMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x10;
        _cpu.Regs.H = 0x80;
        _cpu.Regs.L = 0x00;
        _cpu.Memory.Write(0x0100, 0x86);
        _cpu.Memory.Write(0x8000, 0x20);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x30);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void AllAluRegisterOpcodes_TakeCorrectCycles()
    {
        for (int op = 0x80; op <= 0xBF; op++)
        {
            _cpu.Regs.PC = 0x0100;
            _cpu.Regs.A = 0;
            _cpu.Regs.B = 0; _cpu.Regs.C = 0; _cpu.Regs.D = 0;
            _cpu.Regs.E = 0; _cpu.Regs.H = 0; _cpu.Regs.L = 0;
            _cpu.Memory.Write(0x0100, (byte)op);
            _cpu.Memory.Write(0x8000, 0);
            _cpu.Cycles = 0;

            _cpu.Step();

            int src = op & 7;
            long expected = (src == 6) ? 7 : 4;
            _cpu.Cycles.Should().Be(expected, $"opcode 0x{op:X2} should take {expected} cycles");
        }
    }
}
