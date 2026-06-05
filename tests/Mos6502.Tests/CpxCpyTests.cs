using FluentAssertions;

namespace Mos6502.Tests;

public class CpxCpyTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void CpxImmediate_EqualSetsZeroAndCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xE0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void CpxImmediate_GreaterSetsCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x08;
        _cpu.Memory.Write(0x0200, 0xE0);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeFalse();
    }

    [Fact]
    public void CpxImmediate_LessClearsCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x02;
        _cpu.Memory.Write(0x0200, 0xE0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void CpxZeroPage_CompareWithMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xE4);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CpxAbsolute_CompareWithAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xEC);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CpyImmediate_EqualSetsZeroAndCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xC0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void CpyImmediate_GreaterSetsCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x08;
        _cpu.Memory.Write(0x0200, 0xC0);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void CpyImmediate_LessClearsCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x02;
        _cpu.Memory.Write(0x0200, 0xC0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void CpyZeroPage_CompareWithMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xC4);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void CpyAbsolute_CompareWithAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xCC);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }
}
