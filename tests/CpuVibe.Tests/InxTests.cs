using FluentAssertions;

namespace CpuVibe.Tests;

public class InxTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Inx_IncrementsX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x06);
    }

    [Fact]
    public void Inx_WrapsFromFFTo00()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0xFF;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Inx_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x7F;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x80);
        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void Inx_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0xFF;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Inx_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0xE8);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }
}
