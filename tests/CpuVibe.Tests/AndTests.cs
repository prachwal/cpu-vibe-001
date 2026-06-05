using FluentAssertions;

namespace CpuVibe.Tests;

public class AndTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void AndImmediate_BitwiseAnd()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x29);
        _cpu.Memory.Write(0x0201, 0b10101010);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10001000);
    }

    [Fact]
    public void AndImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x29);
        _cpu.Memory.Write(0x0201, 0b00110011);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void AndImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x29);
        _cpu.Memory.Write(0x0201, 0b11110000);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void AndImmediate_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Memory.Write(0x0200, 0x29);
        _cpu.Memory.Write(0x0201, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void AndZeroPage_AndsWithMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x25);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b10101010);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10001000);
    }

    [Fact]
    public void AndZeroPageX_AndsWithZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x35);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0b10101010);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10001000);
    }

    [Fact]
    public void AndAbsolute_AndsWithMemoryAtAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x2D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0b10101010);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10001000);
    }

    [Fact]
    public void AndAbsoluteX_CrossesPage()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.X = 0x01;
        _cpu.Memory.Write(0x0200, 0x3D);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void AndAbsoluteY_CrossesPage()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.Y = 0x01;
        _cpu.Memory.Write(0x0200, 0x39);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x1000, 0xFF);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void AndIndirectX_AndsFromPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0x21);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);
        _cpu.Memory.Write(0x1000, 0b10101010);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10001000);
    }

    [Fact]
    public void AndIndirectY_AndsFromPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x31);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);
        _cpu.Memory.Write(0x1005, 0b10101010);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10001000);
    }
}
