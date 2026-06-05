using FluentAssertions;

namespace CpuVibe.Tests;

public class AdcTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void AdcImmediate_AddsWithoutCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcImmediate_AddsWithCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x09);
    }

    [Fact]
    public void AdcImmediate_SetsCarryOnOverflow()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void AdcImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x00);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void AdcImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x80;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x00);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void AdcImmediate_SetsOverflowFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x7F;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x80);
        _cpu.Regs.IsOverflow.Should().BeTrue();
        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void AdcImmediate_WithCarryAddsThreeValues()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x03);
    }

    [Fact]
    public void AdcImmediate_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void AdcImmediate_NegativePlusNegativeProducesPositive()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x80;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x69);
        _cpu.Memory.Write(0x0201, 0x80);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsOverflow.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void AdcZeroPage_AddsValueFromMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x65);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcZeroPage_CyclesAre3()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Memory.Write(0x0200, 0x65);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void AdcZeroPageX_AddsValueFromZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.X = 0x05;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x75);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcZeroPageX_WrapsAround()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.X = 0x10;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x75);
        _cpu.Memory.Write(0x0201, 0xF0);
        _cpu.Memory.Write(0x0000, 0x02);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x03);
    }

    [Fact]
    public void AdcZeroPageX_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x75);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void AdcAbsolute_AddsValueFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x6D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcAbsolute_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Memory.Write(0x0200, 0x6D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void AdcAbsoluteX_AddsValueFromAddrPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.X = 0x05;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x7D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcAbsoluteX_CyclesAre4_WhenNoPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x7D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void AdcAbsoluteX_CyclesAre5_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.X = 0x01;
        _cpu.Memory.Write(0x0200, 0x7D);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void AdcAbsoluteY_AddsValueFromAddrPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.Y = 0x05;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x79);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcAbsoluteY_CyclesAre5_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0x79);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void AdcIndirectX_AddsValueFromPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.X = 0x04;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x61);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcIndirectX_CyclesAre6()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0x61);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void AdcIndirectY_AddsValueFromPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x03;
        _cpu.Regs.Y = 0x05;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x71);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x08);
    }

    [Fact]
    public void AdcIndirectY_CyclesAre5_WhenNoPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x71);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void AdcIndirectY_CyclesAre6_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0x71);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0xFF);
        _cpu.Memory.Write(0x0021, 0x0F);
        _cpu.Memory.Write(0x1000, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }
}
