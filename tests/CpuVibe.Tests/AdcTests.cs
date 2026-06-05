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
}
