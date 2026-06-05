using FluentAssertions;

namespace Mos6502.Tests;

public class IncDecTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void IncZeroPage_IncrementsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xE6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0x06);
    }

    [Fact]
    public void IncZeroPage_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xE6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0xFF);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void IncZeroPageX_IncrementsWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xF6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x0015).Should().Be(0x06);
    }

    [Fact]
    public void IncAbsolute_IncrementsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xEE);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0x06);
    }

    [Fact]
    public void IncAbsoluteX_IncrementsWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xFE);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0x06);
    }

    [Fact]
    public void DecZeroPage_DecrementsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xC6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0x04);
    }

    [Fact]
    public void DecZeroPage_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xC6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x00);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0xFF);
        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void DecZeroPageX_DecrementsWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xD6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x0015).Should().Be(0x04);
    }

    [Fact]
    public void DecAbsolute_DecrementsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xCE);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0x04);
    }

    [Fact]
    public void DecAbsoluteX_DecrementsWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xDE);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x05);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0x04);
    }

    [Fact]
    public void InxImplied_IncrementsX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x06);
    }

    [Fact]
    public void InxImplied_WrapsFromFFTo00()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0xFF;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void InyImplied_IncrementsY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xC8);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x06);
    }

    [Fact]
    public void InyImplied_WrapsFromFFTo00()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0xFF;
        _cpu.Memory.Write(0x0200, 0xC8);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void DexImplied_DecrementsX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xCA);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x04);
    }

    [Fact]
    public void DexImplied_WrapsFrom00ToFF()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x00;
        _cpu.Memory.Write(0x0200, 0xCA);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0xFF);
        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void DeyImplied_DecrementsY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x88);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x04);
    }

    [Fact]
    public void DeyImplied_WrapsFrom00ToFF()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x00;
        _cpu.Memory.Write(0x0200, 0x88);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0xFF);
        _cpu.Regs.IsNegative.Should().BeTrue();
    }
}
