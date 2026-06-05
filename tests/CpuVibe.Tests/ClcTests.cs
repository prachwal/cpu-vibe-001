using FluentAssertions;

namespace CpuVibe.Tests;

public class ClcTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Clc_ClearsCarryFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x18);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void Clc_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x18);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void Clc_DoesNotAffectOtherFlags()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P = CpuFlags.Zero | CpuFlags.Negative | CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x18);

        _cpu.Step();

        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Zero);
        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Negative);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }
}
