using FluentAssertions;

namespace Mos6502.Tests;

public class LdxLdyStxStyTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void LdxImmediate_LoadsValue()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA2);
        _cpu.Memory.Write(0x0201, 0x42);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x42);
    }

    [Fact]
    public void LdxImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA2);
        _cpu.Memory.Write(0x0201, 0x00);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void LdxImmediate_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA2);
        _cpu.Memory.Write(0x0201, 0x80);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void LdxZeroPage_LoadsFromMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x42);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x42);
    }

    [Fact]
    public void LdxZeroPageY_LoadsFromZpPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xB6);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x42);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x42);
    }

    [Fact]
    public void LdxAbsolute_LoadsFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xAE);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x42);
    }

    [Fact]
    public void LdxAbsoluteY_LoadsFromAddrPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0xBE);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x42);
    }

    [Fact]
    public void LdyImmediate_LoadsValue()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA0);
        _cpu.Memory.Write(0x0201, 0x42);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x42);
    }

    [Fact]
    public void LdyImmediate_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA0);
        _cpu.Memory.Write(0x0201, 0x00);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void LdyZeroPage_LoadsFromMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xA4);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0x42);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x42);
    }

    [Fact]
    public void LdyZeroPageX_LoadsFromZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xB4);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0x42);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x42);
    }

    [Fact]
    public void LdyAbsolute_LoadsFromAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xAC);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x42);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x42);
    }

    [Fact]
    public void LdyAbsoluteX_LoadsFromAddrPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xBC);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0x42);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x42);
    }

    [Fact]
    public void StxZeroPage_StoresX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x42;
        _cpu.Memory.Write(0x0200, 0x86);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0x42);
    }

    [Fact]
    public void StxZeroPageY_StoresXWithYOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x42;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x96);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x0015).Should().Be(0x42);
    }

    [Fact]
    public void StxAbsolute_StoresX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x42;
        _cpu.Memory.Write(0x0200, 0x8E);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0x42);
    }

    [Fact]
    public void StyZeroPage_StoresY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x42;
        _cpu.Memory.Write(0x0200, 0x84);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0x42);
    }

    [Fact]
    public void StyZeroPageX_StoresYWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x42;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x94);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x0015).Should().Be(0x42);
    }

    [Fact]
    public void StyAbsolute_StoresY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x42;
        _cpu.Memory.Write(0x0200, 0x8C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0x42);
    }
}
