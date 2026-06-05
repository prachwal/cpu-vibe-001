using FluentAssertions;

namespace CpuVibe.Tests;

public class LdaTests
{
    private readonly Cpu _cpu = new();

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x7F)]
    [InlineData(0x80)]
    [InlineData(0xFF)]
    public void LdaImmediate_LoadsValueToA(byte value)
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA9);
        _cpu.Memory.Write(0x0201, value);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(value);
    }

    [Fact]
    public void LdaImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA9);
        _cpu.Memory.Write(0x0201, 0x00);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void LdaImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA9);
        _cpu.Memory.Write(0x0201, 0x80);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeFalse();
    }

    [Fact]
    public void LdaImmediate_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA9);
        _cpu.Memory.Write(0x0201, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void LdaZeroPage_LoadsValueFromMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaZeroPage_CyclesAre3()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void LdaZeroPageX_LoadsValueFromZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xB5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaZeroPageX_WrapsAround()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x10;
        _cpu.Memory.Write(0x0200, 0xB5);
        _cpu.Memory.Write(0x0201, 0xF0);
        _cpu.Memory.Write(0x0000, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaZeroPageX_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xB5);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void LdaAbsolute_LoadsValueFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xAD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaAbsolute_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xAD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void LdaAbsoluteX_LoadsValueFromAddrPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xBD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaAbsoluteX_CyclesAre4_WhenNoPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xBD);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void LdaAbsoluteX_CyclesAre5_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x01;
        _cpu.Memory.Write(0x0200, 0xBD);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void LdaAbsoluteY_LoadsValueFromAddrPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xB9);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaAbsoluteY_CyclesAre5_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0xB9);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void LdaIndirectX_LoadsValueFromPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0xA1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaIndirectX_CyclesAre6()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0xA1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void LdaIndirectY_LoadsValueFromPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xB1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdaIndirectY_CyclesAre5_WhenNoPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xB1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void LdaIndirectY_CyclesAre6_WhenPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0xB1);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0xFF);
        _cpu.Memory.Write(0x0021, 0x0F);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void LdaImmediate_DoesNotAffectOtherFlags()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P = CpuFlags.Carry | CpuFlags.Overflow;
        _cpu.Memory.Write(0x0200, 0xA9);
        _cpu.Memory.Write(0x0201, 0x42);

        _cpu.Step();

        _cpu.Regs.P.HasFlag(CpuFlags.Carry).Should().BeTrue();
        _cpu.Regs.P.HasFlag(CpuFlags.Overflow).Should().BeTrue();
    }
}
