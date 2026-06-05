using FluentAssertions;

namespace CpuVibe.Tests;

public class JmpTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void JmpAbsolute_JumpsToAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x4C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x1000);
    }

    [Fact]
    public void JmpAbsolute_CyclesAre3()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x4C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void JmpAbsolute_DoesNotAffectFlags()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P = CpuFlags.Zero | CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x4C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Zero);
        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Carry);
    }

    [Fact]
    public void JmpIndirect_JumpsToIndirectAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x6C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x00);
        _cpu.Memory.Write(0x1001, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
    }

    [Fact]
    public void JmpIndirect_CyclesAre5()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x6C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0x00);
        _cpu.Memory.Write(0x1001, 0x80);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void JmpIndirect_WrapsAtPageBoundary()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x6C);
        _cpu.Memory.Write(0x0201, 0xFF);
        _cpu.Memory.Write(0x0202, 0x0F);
        _cpu.Memory.Write(0x0FFF, 0x00);
        _cpu.Memory.Write(0x0F00, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
    }
}
