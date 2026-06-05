using FluentAssertions;

namespace CpuVibe.Tests;

public class FlagTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Cld_ClearsDecimalFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Decimal;
        _cpu.Memory.Write(0x0200, 0xD8);

        _cpu.Step();

        _cpu.Regs.P.HasFlag(CpuFlags.Decimal).Should().BeFalse();
    }

    [Fact]
    public void Sed_SetsDecimalFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0xF8);

        _cpu.Step();

        _cpu.Regs.P.HasFlag(CpuFlags.Decimal).Should().BeTrue();
    }

    [Fact]
    public void Cli_ClearsInterruptDisable()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Interrupt;
        _cpu.Memory.Write(0x0200, 0x58);

        _cpu.Step();

        _cpu.Regs.P.HasFlag(CpuFlags.Interrupt).Should().BeFalse();
    }

    [Fact]
    public void Sei_SetsInterruptDisable()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x78);

        _cpu.Step();

        _cpu.Regs.P.HasFlag(CpuFlags.Interrupt).Should().BeTrue();
    }

    [Fact]
    public void Clv_ClearsOverflowFlag()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Overflow;
        _cpu.Memory.Write(0x0200, 0xB8);

        _cpu.Step();

        _cpu.Regs.IsOverflow.Should().BeFalse();
    }

    [Fact]
    public void BitZeroPage_TestsWithAnd()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b11001100;
        _cpu.Memory.Write(0x0200, 0x24);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b10101010);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeFalse();
    }

    [Fact]
    public void BitZeroPage_SetsZeroWhenNoOverlap()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b00110011;
        _cpu.Memory.Write(0x0200, 0x24);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b11001100);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void BitZeroPage_SetsNVFromMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Memory.Write(0x0200, 0x24);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b11000000);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeTrue();
        _cpu.Regs.IsOverflow.Should().BeTrue();
    }

    [Fact]
    public void BitAbsolute_TestsWithAnd()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Memory.Write(0x0200, 0x2C);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0b01000000);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Regs.IsOverflow.Should().BeTrue();
    }

    [Fact]
    public void Cli_CyclesAre2()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Interrupt;
        _cpu.Memory.Write(0x0200, 0x58);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }
}
