using FluentAssertions;

namespace Mos6502.Tests;

public class TransferTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Sec_SetsCarryFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x38);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Sec_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x38);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void Tax_TransfersAtoX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0xAA);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x42);
    }

    [Fact]
    public void Tax_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Memory.Write(0x0200, 0xAA);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Tax_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x80;
        _cpu.Memory.Write(0x0200, 0xAA);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void Tay_TransfersAtoY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0xA8);

        _cpu.Step();

        _cpu.Regs.Y.Should().Be(0x42);
    }

    [Fact]
    public void Tay_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Memory.Write(0x0200, 0xA8);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Tsx_TransfersSPtoX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0xFD;
        _cpu.Memory.Write(0x0200, 0xBA);

        _cpu.Step();

        _cpu.Regs.X.Should().Be(0xFD);
    }

    [Fact]
    public void Tsx_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.SP = 0x80;
        _cpu.Memory.Write(0x0200, 0xBA);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }

    [Fact]
    public void Txa_TransfersXtoA()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x42;
        _cpu.Memory.Write(0x0200, 0x8A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void Txa_SetsZeroFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x00;
        _cpu.Memory.Write(0x0200, 0x8A);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Txs_TransfersXtoSP()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0xFD;
        _cpu.Memory.Write(0x0200, 0x9A);

        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0xFD);
    }

    [Fact]
    public void Tya_TransfersYtoA()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x42;
        _cpu.Memory.Write(0x0200, 0x98);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void Tya_SetsNegativeFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.Y = 0x80;
        _cpu.Memory.Write(0x0200, 0x98);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
    }
}
