namespace Z80.Tests;

public class EdTests
{
    private readonly Cpu _cpu = new();

    private void SetupED(byte edOpcode)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0xED);
        _cpu.Memory.Write(0x0101, edOpcode);
    }

    [Fact]
    public void Ldi_CopiesByte()
    {
        SetupED(0xA0);
        _cpu.Regs.DE = 0x8000;
        _cpu.Regs.HL = 0x9000;
        _cpu.Regs.BC = 0x0005;
        _cpu.Memory.Write(0x8000, 0xAB);

        _cpu.Step();

        _cpu.Memory.Read(0x9000).Should().Be(0xAB);
        _cpu.Regs.DE.Should().Be(0x8001);
        _cpu.Regs.HL.Should().Be(0x9001);
        _cpu.Regs.BC.Should().Be(0x0004);
        _cpu.Regs.IsCarry.Should().BeFalse();
        _cpu.Cycles.Should().Be(16);
    }

    [Fact]
    public void Ldir_RepeatsUntilBCZero()
    {
        SetupED(0xB0);
        _cpu.Regs.DE = 0x8000;
        _cpu.Regs.HL = 0x9000;
        _cpu.Regs.BC = 0x0003;
        _cpu.Memory.Write(0x8000, 0x11);

        _cpu.Step();

        _cpu.Memory.Read(0x9000).Should().Be(0x11);
        _cpu.Regs.BC.Should().Be(0x0002);
        _cpu.Regs.PC.Should().Be(0x0100);
        _cpu.Cycles.Should().Be(21);
    }

    [Fact]
    public void Ldir_StopsWhenBCZero()
    {
        SetupED(0xB0);
        _cpu.Regs.DE = 0x8000;
        _cpu.Regs.HL = 0x9000;
        _cpu.Regs.BC = 0x0001;
        _cpu.Memory.Write(0x8000, 0x22);

        _cpu.Step();

        _cpu.Memory.Read(0x9000).Should().Be(0x22);
        _cpu.Regs.BC.Should().Be(0x0000);
        _cpu.Cycles.Should().Be(16);
    }

    [Fact]
    public void Cpi_CompareMatch()
    {
        SetupED(0xA1);
        _cpu.Regs.A = 0xAB;
        _cpu.Regs.HL = 0x8000;
        _cpu.Regs.BC = 0x0005;
        _cpu.Memory.Write(0x8000, 0xAB);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.HL.Should().Be(0x8001);
        _cpu.Regs.BC.Should().Be(0x0004);
    }

    [Fact]
    public void Cpi_CompareNoMatch()
    {
        SetupED(0xA1);
        _cpu.Regs.A = 0xAB;
        _cpu.Regs.HL = 0x8000;
        _cpu.Regs.BC = 0x0005;
        _cpu.Memory.Write(0x8000, 0xCD);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void Neg_NegateAccumulator()
    {
        SetupED(0x44);
        _cpu.Regs.A = 0x05;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xFB);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void Neg_NegateZero()
    {
        SetupED(0x44);
        _cpu.Regs.A = 0x00;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsCarry.Should().BeFalse();
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Neg_Negate0x80()
    {
        SetupED(0x44);
        _cpu.Regs.A = 0x80;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x80);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void AdcHlBc()
    {
        SetupED(0x4A);
        _cpu.Regs.HL = 0x1000;
        _cpu.Regs.BC = 0x2000;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x3000);
        _cpu.Regs.IsCarry.Should().BeFalse();
        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Cycles.Should().Be(15);
    }

    [Fact]
    public void AdcHlBc_WithCarry()
    {
        SetupED(0x4A);
        _cpu.Regs.HL = 0x1000;
        _cpu.Regs.BC = 0x2000;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x3001);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void SbcHlDe()
    {
        SetupED(0x52);
        _cpu.Regs.HL = 0x3000;
        _cpu.Regs.DE = 0x1000;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x2000);
        _cpu.Regs.IsCarry.Should().BeFalse();
        _cpu.Cycles.Should().Be(15);
    }

    [Fact]
    public void SbcHlDe_WithBorrow()
    {
        SetupED(0x52);
        _cpu.Regs.HL = 0x3000;
        _cpu.Regs.DE = 0x1000;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x1FFF);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void LdNnBc()
    {
        SetupED(0x43);
        _cpu.Regs.BC = 0x1234;
        _cpu.Memory.Write(0x0102, 0x00);
        _cpu.Memory.Write(0x0103, 0x80);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x34);
        _cpu.Memory.Read(0x8001).Should().Be(0x12);
        _cpu.Cycles.Should().Be(20);
    }

    [Fact]
    public void LdBcNn()
    {
        SetupED(0x4B);
        _cpu.Memory.Write(0x0102, 0x00);
        _cpu.Memory.Write(0x0103, 0x80);
        _cpu.Memory.Write(0x8000, 0x34);
        _cpu.Memory.Write(0x8001, 0x12);

        _cpu.Step();

        _cpu.Regs.BC.Should().Be(0x1234);
        _cpu.Cycles.Should().Be(20);
    }

    [Fact]
    public void LdIA_SetsFlags()
    {
        SetupED(0x47);
        _cpu.Regs.A = 0xFF;
        _cpu.Regs.I = 0x42;

        _cpu.Step();

        _cpu.Regs.I.Should().Be(0xFF);
        _cpu.Cycles.Should().Be(9);
    }

    [Fact]
    public void LdAI_SetsFlags()
    {
        SetupED(0x57);
        _cpu.Regs.I = 0x80;
        _cpu.Iff2 = true;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x80);
        _cpu.Regs.IsSign.Should().BeTrue();
        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Cycles.Should().Be(9);
    }

    [Fact]
    public void LdAI_ZeroFlag()
    {
        SetupED(0x57);
        _cpu.Regs.I = 0x00;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsSign.Should().BeFalse();
    }

    [Fact]
    public void InAC()
    {
        SetupED(0x78);
        _cpu.Regs.A = 0x00;
        _cpu.Memory.Write(0x0102, 0x42);
        _cpu.Memory.Write(0x0042, 0x55);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x55);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void OutCA()
    {
        SetupED(0x79);
        _cpu.Regs.A = 0xAB;

        _cpu.Step();

        _cpu.Memory.Read(0x00).Should().Be(0xAB);
        _cpu.Cycles.Should().Be(12);
    }

    [Fact]
    public void Im1()
    {
        SetupED(0x56);

        _cpu.Step();

        _cpu.InterruptMode.Should().Be(1);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void Im2()
    {
        SetupED(0x5E);

        _cpu.Step();

        _cpu.InterruptMode.Should().Be(2);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void Reti()
    {
        SetupED(0x4D);
        _cpu.Regs.SP = 0xEFFE;
        _cpu.Memory.Write(0xEFFE, 0x00);
        _cpu.Memory.Write(0xEFFF, 0x80);
        _cpu.Iff1 = true;
        _cpu.Iff2 = true;

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Iff1.Should().BeTrue();
        _cpu.Iff2.Should().BeTrue();
        _cpu.Cycles.Should().Be(14);
    }

    [Fact]
    public void Retn_CopiesIff2ToIff1()
    {
        SetupED(0x45);
        _cpu.Regs.SP = 0xEFFE;
        _cpu.Memory.Write(0xEFFE, 0x00);
        _cpu.Memory.Write(0xEFFF, 0x80);
        _cpu.Iff1 = true;
        _cpu.Iff2 = false;

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Iff1.Should().BeFalse();
        _cpu.Iff2.Should().BeFalse();
    }

    [Fact]
    public void Ldd_CopiesBackward()
    {
        SetupED(0xA8);
        _cpu.Regs.DE = 0x8002;
        _cpu.Regs.HL = 0x9002;
        _cpu.Regs.BC = 0x0003;
        _cpu.Memory.Write(0x8002, 0xCD);

        _cpu.Step();

        _cpu.Memory.Read(0x9002).Should().Be(0xCD);
        _cpu.Regs.DE.Should().Be(0x8001);
        _cpu.Regs.HL.Should().Be(0x9001);
        _cpu.Regs.BC.Should().Be(0x0002);
    }

    [Fact]
    public void Outi_SendsByte()
    {
        SetupED(0xA3);
        _cpu.Regs.HL = 0x8000;
        _cpu.Regs.B = 0x05;
        _cpu.Regs.C = 0x05;
        _cpu.Memory.Write(0x8000, 0xBE);

        _cpu.Step();

        _cpu.Memory.Read(0x05).Should().Be(0xBE);
        _cpu.Regs.B.Should().Be(0x04);
        _cpu.Regs.HL.Should().Be(0x8001);
    }

    [Fact]
    public void Ini_SendsByte()
    {
        SetupED(0xA2);
        _cpu.Regs.HL = 0x8000;
        _cpu.Regs.B = 0x05;
        _cpu.Regs.C = 0x05;
        _cpu.Memory.Write(0x05, 0xDE);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0xDE);
        _cpu.Regs.B.Should().Be(0x04);
        _cpu.Regs.HL.Should().Be(0x8001);
    }
}
