using FluentAssertions;

namespace Mos6502.Tests;

public class ShiftRotateTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void AslAccumulator_ShiftsLeft()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b01000000;
        _cpu.Memory.Write(0x0200, 0x0A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10000000);
    }

    [Fact]
    public void AslAccumulator_SetsCarryWhenBit7Set()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b10000000;
        _cpu.Memory.Write(0x0200, 0x0A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void AslZeroPage_ShiftsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x06);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b01000000);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0b10000000);
    }

    [Fact]
    public void AslZeroPageX_ShiftsWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x16);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0015, 0b01000000);

        _cpu.Step();

        _cpu.Memory.Read(0x0015).Should().Be(0b10000000);
    }

    [Fact]
    public void AslAbsolute_ShiftsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x0E);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1000, 0b01000000);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0b10000000);
    }

    [Fact]
    public void AslAbsoluteX_ShiftsWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x1E);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0b01000000);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0b10000000);
    }

    [Fact]
    public void LsrAccumulator_ShiftsRight()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b00000100;
        _cpu.Memory.Write(0x0200, 0x4A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00000010);
    }

    [Fact]
    public void LsrAccumulator_SetsCarryWhenBit0Set()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b00000001;
        _cpu.Memory.Write(0x0200, 0x4A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void LsrAccumulator_ClearsNegative()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0xFF;
        _cpu.Memory.Write(0x0200, 0x4A);

        _cpu.Step();

        _cpu.Regs.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void LsrZeroPage_ShiftsMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x46);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b00000100);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0b00000010);
    }

    [Fact]
    public void RolAccumulator_RotatesLeft()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b01000000;
        _cpu.Memory.Write(0x0200, 0x2A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10000000);
    }

    [Fact]
    public void RolAccumulator_WithCarrySetsBit0()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b01000000;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x2A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10000001);
    }

    [Fact]
    public void RolAccumulator_CarryFromBit7()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b10000000;
        _cpu.Memory.Write(0x0200, 0x2A);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.A.Should().Be(0x00);
    }

    [Fact]
    public void RolZeroPage_RotatesMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x26);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b01000000);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0b10000000);
    }

    [Fact]
    public void RorAccumulator_RotatesRight()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b00000100;
        _cpu.Memory.Write(0x0200, 0x6A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b00000010);
    }

    [Fact]
    public void RorAccumulator_WithCarrySetsBit7()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b00000100;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x6A);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0b10000010);
    }

    [Fact]
    public void RorAccumulator_CarryFromBit0()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0b00000001;
        _cpu.Memory.Write(0x0200, 0x6A);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.A.Should().Be(0x00);
    }

    [Fact]
    public void RorZeroPage_RotatesMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Memory.Write(0x0200, 0x66);
        _cpu.Memory.Write(0x0201, 0x10);
        _cpu.Memory.Write(0x0010, 0b00000100);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0b00000010);
    }

    [Fact]
    public void RolAbsoluteX_RotatesWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x3E);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0b01000000);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0b10000000);
    }

    [Fact]
    public void RorAbsoluteX_RotatesWithXOffset()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x7E);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);
        _cpu.Memory.Write(0x1005, 0b00000100);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0b00000010);
    }
}
