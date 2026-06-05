namespace Z80.Tests;

public class Alu16BitTests
{
    private readonly Cpu _cpu = new();

    [Theory]
    [InlineData(0x09, "BC")]
    [InlineData(0x19, "DE")]
    [InlineData(0x29, "HL")]
    [InlineData(0x39, "SP")]
    public void AddHlRr_AddsToHl(byte opcode, string regName)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0x1000;
        _cpu.Memory.Write(0x0100, opcode);

        switch (regName)
        {
            case "BC": _cpu.Regs.BC = 0x0234; break;
            case "DE": _cpu.Regs.DE = 0x0234; break;
            case "HL": _cpu.Regs.HL = 0x1000 + 0x0234; break;
            case "SP": _cpu.Regs.SP = 0x0234; break;
        }

        if (regName == "HL")
        {
            _cpu.Regs.HL = 0x1000;
            _cpu.Regs.HL += 0x0234;
        }

        _cpu.Step();

        if (regName != "HL")
            _cpu.Regs.HL.Should().Be(0x1234);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void AddHlBc_SetsCarryOnOverflow()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0xFFFF;
        _cpu.Regs.BC = 0x0001;
        _cpu.Memory.Write(0x0100, 0x09);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x0000);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void AddHlBc_SetsHalfCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0x0FFF;
        _cpu.Regs.BC = 0x0001;
        _cpu.Memory.Write(0x0100, 0x09);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x1000);
        _cpu.Regs.IsHalfCarry.Should().BeTrue();
    }

    [Fact]
    public void IncRr_IncrementsBC()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.BC = 0x1234;
        _cpu.Memory.Write(0x0100, 0x03);

        _cpu.Step();

        _cpu.Regs.BC.Should().Be(0x1235);
        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void IncRr_WrapsAround()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.BC = 0xFFFF;
        _cpu.Memory.Write(0x0100, 0x03);

        _cpu.Step();

        _cpu.Regs.BC.Should().Be(0x0000);
    }

    [Fact]
    public void DecRr_DecrementsDE()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.DE = 0x1234;
        _cpu.Memory.Write(0x0100, 0x1B);

        _cpu.Step();

        _cpu.Regs.DE.Should().Be(0x1233);
    }

    [Fact]
    public void DecRr_WrapsAround()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.DE = 0x0000;
        _cpu.Memory.Write(0x0100, 0x1B);

        _cpu.Step();

        _cpu.Regs.DE.Should().Be(0xFFFF);
    }

    [Fact]
    public void Rlca_RotatesLeftCircular()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x80;
        _cpu.Memory.Write(0x0100, 0x07);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsHalfCarry.Should().BeFalse();
        _cpu.Regs.IsSubtract.Should().BeFalse();
    }

    [Fact]
    public void Rlca_NoCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x40;
        _cpu.Memory.Write(0x0100, 0x07);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x80);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void Rrca_RotatesRightCircular()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x01;
        _cpu.Memory.Write(0x0100, 0x0F);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x80);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Rla_RotatesLeftThroughCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x80;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);
        _cpu.Memory.Write(0x0100, 0x17);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Rla_WithCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x80;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);
        _cpu.Memory.Write(0x0100, 0x17);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Rra_RotatesRightThroughCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x01;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);
        _cpu.Memory.Write(0x0100, 0x1F);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Cpl_ComplementsAccumulator()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x35;
        _cpu.Memory.Write(0x0100, 0x2F);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xCA);
        _cpu.Regs.IsHalfCarry.Should().BeTrue();
        _cpu.Regs.IsSubtract.Should().BeTrue();
    }

    [Fact]
    public void Scf_SetsCarryFlag()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);
        _cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
        _cpu.Memory.Write(0x0100, 0x37);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsHalfCarry.Should().BeFalse();
        _cpu.Regs.IsSubtract.Should().BeFalse();
    }

    [Fact]
    public void Ccf_ComplementsCarryFlag()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);
        _cpu.Memory.Write(0x0100, 0x3F);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeFalse();
        _cpu.Regs.IsHalfCarry.Should().BeTrue();
        _cpu.Regs.IsSubtract.Should().BeFalse();
    }

    [Fact]
    public void Ccf_FromNoCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);
        _cpu.Memory.Write(0x0100, 0x3F);

        _cpu.Step();

        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsHalfCarry.Should().BeFalse();
    }

    [Fact]
    public void Daa_AdjustsAfterAddition()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x0A;
        _cpu.Regs.SetFlag(CpuFlags.Subtract, false);
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);
        _cpu.Regs.SetFlag(CpuFlags.HalfCarry, false);
        _cpu.Memory.Write(0x0100, 0x27);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x10);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void Daa_AdjustsAfterSubtraction()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x10;
        _cpu.Regs.SetFlag(CpuFlags.Subtract, true);
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);
        _cpu.Regs.SetFlag(CpuFlags.HalfCarry, true);
        _cpu.Memory.Write(0x0100, 0x27);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x0A);
    }

    [Fact]
    public void AddHlHl_DoublesHl()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0x1234;
        _cpu.Memory.Write(0x0100, 0x29);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x2468);
    }
}
