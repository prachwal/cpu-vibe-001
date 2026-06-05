using FluentAssertions;

namespace CpuVibe.Tests;

public class BrkTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Brk_PushesPCandPToStack()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.Regs.P = CpuFlags.Unused | CpuFlags.Interrupt;
        _cpu.Memory.Write(0x0200, 0x00);
        _cpu.Memory.Write(0xFFFE, 0x00);
        _cpu.Memory.Write(0xFFFF, 0x80);

        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0xFC);
        _cpu.Memory.Read(0x01FF).Should().Be(0x02);
        _cpu.Memory.Read(0x01FE).Should().Be(0x02);
        _cpu.Memory.Read(0x01FD).Should().Be((byte)(CpuFlags.Unused | CpuFlags.Interrupt | CpuFlags.Break));
    }

    [Fact]
    public void Brk_JumpsToIRQVector()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x00);
        _cpu.Memory.Write(0xFFFE, 0x00);
        _cpu.Memory.Write(0xFFFF, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
    }

    [Fact]
    public void Brk_SetsInterruptFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x00);
        _cpu.Memory.Write(0xFFFE, 0x00);
        _cpu.Memory.Write(0xFFFF, 0x80);

        _cpu.Step();

        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Interrupt);
    }

    [Fact]
    public void Brk_CyclesAre7()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x00);
        _cpu.Memory.Write(0xFFFE, 0x00);
        _cpu.Memory.Write(0xFFFF, 0x80);

        _cpu.Step();

        _cpu.Cycles.Should().Be(7);
    }
}
