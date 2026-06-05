namespace Z80.Tests.Core;

public class FlagsTests
{
    [Fact]
    public void SetFlag_TurnsOn()
    {
        var regs = new Registers();
        regs.F = 0;

        regs.SetFlag(CpuFlags.Zero, true);

        regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void SetFlag_TurnsOff()
    {
        var regs = new Registers();
        regs.F = 0x40;

        regs.SetFlag(CpuFlags.Zero, false);

        regs.IsZero.Should().BeFalse();
    }

    [Fact]
    public void SetNZ_SetsSign()
    {
        var regs = new Registers();
        regs.SetFlag(CpuFlags.Zero, true);

        regs.SetSZ(0x80);

        regs.IsSign.Should().BeTrue();
        regs.IsZero.Should().BeFalse();
    }

    [Fact]
    public void SetNZ_SetsZero()
    {
        var regs = new Registers();
        regs.SetSZ(0x00);

        regs.IsZero.Should().BeTrue();
        regs.IsSign.Should().BeFalse();
    }

    [Fact]
    public void SetSZPV_SetsParityEven()
    {
        var regs = new Registers();
        regs.SetSZPV(0x03);

        regs.IsParityOverflow.Should().BeTrue();
    }

    [Fact]
    public void SetSZPV_SetsParityOdd()
    {
        var regs = new Registers();
        regs.SetSZPV(0x07);

        regs.IsParityOverflow.Should().BeFalse();
    }

    [Fact]
    public void AllFlags_CanBeSet()
    {
        var regs = new Registers();
        regs.SetFlag(CpuFlags.Sign, true);
        regs.SetFlag(CpuFlags.Zero, true);
        regs.SetFlag(CpuFlags.HalfCarry, true);
        regs.SetFlag(CpuFlags.ParityOverflow, true);
        regs.SetFlag(CpuFlags.Subtract, true);
        regs.SetFlag(CpuFlags.Carry, true);

        regs.IsSign.Should().BeTrue();
        regs.IsZero.Should().BeTrue();
        regs.IsHalfCarry.Should().BeTrue();
        regs.IsParityOverflow.Should().BeTrue();
        regs.IsSubtract.Should().BeTrue();
        regs.IsCarry.Should().BeTrue();
    }
}
