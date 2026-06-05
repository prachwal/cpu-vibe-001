using FluentAssertions;

namespace CpuVibe.Tests;

public class JsrRtsStackTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Jsr_PushesReturnAddressAndJumps()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.Memory.Write(0x0200, 0x20);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x1000);
        _cpu.Regs.SP.Should().Be(0xFD);
    }

    [Fact]
    public void Rts_ReturnsFromSubroutine()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.StackPush(0x02);
        _cpu.StackPush(0x01);
        _cpu.Memory.Write(0x0200, 0x60);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0202);
    }

    [Fact]
    public void Jsr_Rts_RoundTrip()
    {
        _cpu.Regs.PC = 0x0300;
        _cpu.Memory.Write(0x0300, 0x20);
        _cpu.Memory.Write(0x0301, 0x00);
        _cpu.Memory.Write(0x0302, 0x04);
        _cpu.Memory.Write(0x0400, 0x60);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x0400);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x0303);
    }

    [Fact]
    public void Rti_RestoresPcAndP()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.StackPush(0x03);
        _cpu.StackPush(0x00);
        _cpu.StackPush(0x24);
        _cpu.Memory.Write(0x0200, 0x40);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0300);
        _cpu.Regs.P.Should().Be((CpuFlags)0x24);
    }

    [Fact]
    public void Pha_Pla_RoundTrip()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0x48);
        _cpu.Memory.Write(0x0201, 0xA9);
        _cpu.Memory.Write(0x0202, 0x00);
        _cpu.Memory.Write(0x0203, 0x68);

        _cpu.Step();
        _cpu.Regs.A.Should().Be(0x42);

        _cpu.Step();
        _cpu.Regs.A.Should().Be(0x00);

        _cpu.Step();
        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void Php_Plp_RoundTrip()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P = (CpuFlags)0x2D;
        _cpu.Memory.Write(0x0200, 0x08);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x28);

        _cpu.Step();
        _cpu.Step();

        _cpu.Regs.P.Should().Be((CpuFlags)0x2D);
    }

    [Fact]
    public void Pha_CyclesAre3()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x48);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void Pla_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.StackPush(0x42);
        _cpu.Memory.Write(0x0200, 0x68);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void Pla_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.StackPush(0x00);
        _cpu.Memory.Write(0x0200, 0x68);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Pla_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFF;
        _cpu.StackPush(0x80);
        _cpu.Memory.Write(0x0200, 0x68);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }
}
