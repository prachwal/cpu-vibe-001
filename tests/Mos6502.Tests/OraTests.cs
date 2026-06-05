using FluentAssertions;

namespace Mos6502.Tests;

public class OraTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void OraImmediate_BitwiseOr()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11000000;
        _cpu.Memory.Write(0x0200, 0x09);
        _cpu.Memory.Write(0x0201, 0b00110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b11110000);
    }

    [Fact]
    public void OraImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Memory.Write(0x0200, 0x09);
        _cpu.Memory.Write(0x0201, 0x00);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void OraImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Memory.Write(0x0200, 0x09);
        _cpu.Memory.Write(0x0201, 0b10000000);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void OraZeroPage_OrsWithMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11000000;
        _cpu.Memory.Write(0x0200, 0x05);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b00110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b11110000);
    }

    [Fact]
    public void OraZeroPageX_OrsFromZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11000000;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x15);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0b00110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b11110000);
    }

    [Fact]
    public void OraAbsolute_OrsFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11000000;
        _cpu.Memory.Write(0x0200, 0x0D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0b00110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b11110000);
    }

    [Fact]
    public void OraAbsoluteX_CrossesPage()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.X = 0x01;
        _cpu.Memory.Write(0x0200, 0x1D);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void OraAbsoluteY_CrossesPage()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0x19);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void OraIndirectX_OrsFromPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11000000;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0x01);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0b00110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b11110000);
    }

    [Fact]
    public void OraIndirectY_OrsFromPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11000000;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x11);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0b00110000);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b11110000);
    }
}
