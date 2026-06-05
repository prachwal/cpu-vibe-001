using FluentAssertions;

namespace Mos6502.Tests;

public class SbcTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void SbcImmediate_SubtractsWithoutCarry()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcImmediate_SubtractsWithBorrow()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x04);
    }

    [Fact]
    public void SbcImmediate_SetsCarryWhenNoBorrow()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x02);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void SbcImmediate_ClearsCarryWhenBorrow()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x02;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xFD);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void SbcImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void SbcImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xFF);
        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void SbcImmediate_SetsOverflowFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x80;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x7F);
        _cpu.Regs.IsOverflow.Should().BeTrue();
    }

    [Fact]
    public void SbcImmediate_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Memory.Write(0x0200, 0xE9);
        _cpu.Memory.Write(0x0201, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void SbcZeroPage_SubtractsValueFromMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcZeroPage_CyclesAre3()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Memory.Write(0x0200, 0xE5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void SbcZeroPageX_SubtractsFromZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.X = 0x05;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xF5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcZeroPageX_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xF5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void SbcAbsolute_SubtractsFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xED);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcAbsolute_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Memory.Write(0x0200, 0xED);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void SbcAbsoluteX_SubtractsFromAddrPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.X = 0x05;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xFD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcAbsoluteX_CyclesAre5_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.X = 0x01;
        _cpu.Memory.Write(0x0200, 0xFD);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void SbcAbsoluteY_SubtractsFromAddrPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.Y = 0x05;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xF9);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcAbsoluteY_CyclesAre5_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0xF9);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void SbcIndirectX_SubtractsFromPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.X = 0x04;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xE1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcIndirectX_CyclesAre6()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0xE1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void SbcIndirectY_SubtractsFromPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x08;
        _cpu.Regs.Y = 0x05;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xF1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0x03);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x05);
    }

    [Fact]
    public void SbcIndirectY_CyclesAre6_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x05;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0xF1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0xFF);
        _cpu.Memory.Write(0x0021, 0x0F);
        _cpu.Memory.Write(0x1000, 0x03);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }
}
