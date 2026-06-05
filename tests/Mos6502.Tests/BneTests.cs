using FluentAssertions;

namespace Mos6502.Tests;

public class BneTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Bne_BranchesWhenZIsClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0207);
    }

    [Fact]
    public void Bne_DoesNotBranchWhenZIsSet()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0202);
    }

    [Fact]
    public void Bne_BranchesBackward()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0xFB);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x01FD);
    }

    [Fact]
    public void Bne_CyclesAre2_WhenNotTaken()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void Bne_CyclesAre3_WhenTaken()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0x05);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void Bne_CyclesAre5_WhenTakenAndPageCrossing()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0x01);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }
}
