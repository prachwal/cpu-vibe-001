using FluentAssertions;

namespace Mos6502.Tests;

public class EorTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void EorImmediate_BitwiseXor()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x49);
        _cpu.Memory.Write(0x0201, 0b11110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00111100);
    }

    [Fact]
    public void EorImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x49);
        _cpu.Memory.Write(0x0201, 0b11001100);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void EorImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b01000000;
        _cpu.Memory.Write(0x0200, 0x49);
        _cpu.Memory.Write(0x0201, 0b11000000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10000000);
        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void EorZeroPage_XorsWithMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x45);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b11110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00111100);
    }

    [Fact]
    public void EorZeroPageX_XorsFromZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x55);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0b11110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00111100);
    }

    [Fact]
    public void EorAbsolute_XorsFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x4D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0b11110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00111100);
    }

    [Fact]
    public void EorAbsoluteX_CrossesPage()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.X = 0x01;
        _cpu.Memory.Write(0x0200, 0x5D);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void EorAbsoluteY_CrossesPage()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0x59);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void EorIndirectX_XorsFromPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0x41);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0b11110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00111100);
    }

    [Fact]
    public void EorIndirectY_XorsFromPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x51);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0b11110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00111100);
    }
}
