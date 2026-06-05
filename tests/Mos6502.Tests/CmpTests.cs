using FluentAssertions;

namespace Mos6502.Tests;

public class CmpTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void CmpImmediate_SetsCarryWhenEqual()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Memory.Write(0x0200, 0xC9);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpImmediate_SetsCarryWhenGreater()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Memory.Write(0x0200, 0xC9);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeFalse();
    }

    [Fact]
    public void CmpImmediate_ClearsCarryWhenLess()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x02;
        _cpu.Memory.Write(0x0200, 0xC9);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void CmpImmediate_SetsNegativeWhenLess()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x02;
        _cpu.Memory.Write(0x0200, 0xC9);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void CmpImmediate_DoesNotAlterA()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0xC9);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void CmpZeroPage_CompareWithMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Memory.Write(0x0200, 0xC5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpZeroPageX_CompareWithZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xD5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpAbsolute_CompareWithAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Memory.Write(0x0200, 0xCD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpAbsoluteX_CompareWithAddrPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xDD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpAbsoluteY_CompareWithAddrPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xD9);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpIndirectX_CompareWithPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0xC1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CmpIndirectY_CompareWithPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xD1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }
}
