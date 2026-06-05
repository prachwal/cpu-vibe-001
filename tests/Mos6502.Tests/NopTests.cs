using FluentAssertions;

namespace Mos6502.Tests;

public class NopTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Nop_DoesNotChangeRegisters()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x10;
        _cpu.Regs.Y = 0x20;
        _cpu.Memory.Write(0x0200, 0xEA);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
        _cpu.Regs.X.Should().Be(0x10);
        _cpu.Regs.Y.Should().Be(0x20);
    }

    [Fact]
    public void Nop_DoesNotChangeFlags()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P = CpuFlags.Zero | CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xEA);

        _cpu.Step();

        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Zero);
        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Carry);
    }

    [Fact]
    public void Nop_AdvancesPC()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xEA);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0201);
    }

    [Fact]
    public void Nop_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xEA);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }
}
