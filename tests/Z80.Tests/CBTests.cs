namespace Z80.Tests;

public class CBTests
{
    private readonly Cpu _cpu = new();

    private void SetupCB(byte cbOpcode)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0xCB);
        _cpu.Memory.Write(0x0101, cbOpcode);
    }

    [Fact]
    public void RlcB_RotatesLeftCircular()
    {
        SetupCB(0x00);
        _cpu.Regs.B = 0x85;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x0B);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void RlcB_ClearsCarry()
    {
        SetupCB(0x00);
        _cpu.Regs.B = 0x30;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x60);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void RlcA()
    {
        SetupCB(0x07);
        _cpu.Regs.A = 0x80;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RrcB_RotatesRightCircular()
    {
        SetupCB(0x08);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x80);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RlB_RotateLeft()
    {
        SetupCB(0x10);
        _cpu.Regs.B = 0x85;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x0A);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RlB_WithCarryIn()
    {
        SetupCB(0x10);
        _cpu.Regs.B = 0x85;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x0B);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RrB_RotateRight()
    {
        SetupCB(0x18);
        _cpu.Regs.B = 0x81;
        _cpu.Regs.SetFlag(CpuFlags.Carry, false);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x40);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void SlaB_ShiftLeftArithmetic()
    {
        SetupCB(0x20);
        _cpu.Regs.B = 0x85;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x0A);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void SraB_ShiftRightArithmetic()
    {
        SetupCB(0x28);
        _cpu.Regs.B = 0x85;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0xC2);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void SrlB_ShiftRightLogical()
    {
        SetupCB(0x38);
        _cpu.Regs.B = 0x85;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x42);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void SllB_ShiftLeftLoad()
    {
        SetupCB(0x30);
        _cpu.Regs.B = 0x80;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Rlc_HL_UsesMemory()
    {
        SetupCB(0x06);
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x8000, 0x85);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x0B);
        _cpu.Regs.IsCarry.Should().BeTrue();
        _cpu.Cycles.Should().Be(15);
    }

    [Fact]
    public void RlcC()
    {
        SetupCB(0x01);
        _cpu.Regs.C = 0x80;

        _cpu.Step();

        _cpu.Regs.C.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RlcD()
    {
        SetupCB(0x02);
        _cpu.Regs.D = 0x40;

        _cpu.Step();

        _cpu.Regs.D.Should().Be(0x80);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void RlcE()
    {
        SetupCB(0x03);
        _cpu.Regs.E = 0x01;

        _cpu.Step();

        _cpu.Regs.E.Should().Be(0x02);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void RlcH()
    {
        SetupCB(0x04);
        _cpu.Regs.H = 0x80;

        _cpu.Step();

        _cpu.Regs.H.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RlcL()
    {
        SetupCB(0x05);
        _cpu.Regs.L = 0xFF;

        _cpu.Step();

        _cpu.Regs.L.Should().Be(0xFF);
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void RlcZero_SetsZeroFlag()
    {
        SetupCB(0x00);
        _cpu.Regs.B = 0x00;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void RrcB_NoCarry()
    {
        SetupCB(0x08);
        _cpu.Regs.B = 0x02;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void RlB_WithCarry()
    {
        SetupCB(0x10);
        _cpu.Regs.B = 0x00;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x01);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void RrB_WithCarry()
    {
        SetupCB(0x18);
        _cpu.Regs.B = 0x00;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x80);
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void SlaB_Zero()
    {
        SetupCB(0x20);
        _cpu.Regs.B = 0x00;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsCarry.Should().BeFalse();
    }

    [Fact]
    public void SrlB()
    {
        SetupCB(0x38);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x00);
        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void Bit0_B_Set()
    {
        SetupCB(0x40);
        _cpu.Regs.B = 0x01;

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void Bit0_B_Reset()
    {
        SetupCB(0x40);
        _cpu.Regs.B = 0xFE;

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Bit7_A_Set()
    {
        SetupCB(0x7F);
        _cpu.Regs.A = 0x80;

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void Bit7_A_Reset()
    {
        SetupCB(0x7F);
        _cpu.Regs.A = 0x7F;

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
        _cpu.Regs.IsSign.Should().BeFalse();
    }

    [Fact]
    public void Bit_HL_UsesMemory()
    {
        SetupCB(0x46);
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x8000, 0xFF);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeFalse();
        _cpu.Cycles.Should().Be(12);
    }

    [Fact]
    public void Bit_HL_ZeroWhenBitClear()
    {
        SetupCB(0x46);
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x8000, 0x00);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Res0_B_ClearsBit()
    {
        SetupCB(0x80);
        _cpu.Regs.B = 0xFF;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0xFE);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void Res7_A_ClearsBit()
    {
        SetupCB(0xBF);
        _cpu.Regs.A = 0xFF;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x7F);
    }

    [Fact]
    public void Res_HL_ClearsBitInMemory()
    {
        SetupCB(0x86);
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x8000, 0xFF);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0xFE);
        _cpu.Cycles.Should().Be(15);
    }

    [Fact]
    public void Set0_B_SetsBit()
    {
        SetupCB(0xC0);
        _cpu.Regs.B = 0x00;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x01);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void Set7_A_SetsBit()
    {
        SetupCB(0xFF);
        _cpu.Regs.A = 0x00;

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x80);
    }

    [Fact]
    public void Set_HL_SetsBitInMemory()
    {
        SetupCB(0xC6);
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x8000, 0x00);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x01);
        _cpu.Cycles.Should().Be(15);
    }
}
