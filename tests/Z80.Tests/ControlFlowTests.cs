namespace Z80.Tests;

public class ControlFlowTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void JpNn_JumpsToAddress()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0xC3);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Cycles.Should().Be(10);
    }

    [Fact]
    public void JpNz_JumpsWhenNotZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Zero, false);
        _cpu.Memory.Write(0x0100, 0xC2);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x8000);
    }

    [Fact]
    public void JpNz_DoesNotJumpWhenZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Zero, true);
        _cpu.Memory.Write(0x0100, 0xC2);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x0103);
    }

    [Fact]
    public void JpZ_JumpsWhenZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Zero, true);
        _cpu.Memory.Write(0x0100, 0xCA);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x8000);
    }

    [Fact]
    public void JpC_JumpsWhenCarry()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Carry, true);
        _cpu.Memory.Write(0x0100, 0xDA);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x8000);
    }

    [Fact]
    public void JpPe_JumpsWhenParityOverflow()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.ParityOverflow, true);
        _cpu.Memory.Write(0x0100, 0xEA);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x8000);
    }

    [Fact]
    public void JpHl_JumpsToHL()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x0100, 0xE9);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void JrD_JumpsRelative()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x18);
        _cpu.Memory.Write(0x0101, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0112);
        _cpu.Cycles.Should().Be(12);
    }

    [Fact]
    public void JrD_JumpsNegative()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x18);
        _cpu.Memory.Write(0x0101, 0xFE);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0100);
    }

    [Fact]
    public void JrNz_JumpsWhenNotZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Zero, false);
        _cpu.Memory.Write(0x0100, 0x20);
        _cpu.Memory.Write(0x0101, 0x05);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0107);
        _cpu.Cycles.Should().Be(12);
    }

    [Fact]
    public void JrNz_DoesNotJumpWhenZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SetFlag(CpuFlags.Zero, true);
        _cpu.Memory.Write(0x0100, 0x20);
        _cpu.Memory.Write(0x0101, 0x05);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0102);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void Djnz_JumpsWhileBNotZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0x02;
        _cpu.Memory.Write(0x0100, 0x10);
        _cpu.Memory.Write(0x0101, 0x05);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x01);
        _cpu.Regs.PC.Should().Be(0x0107);
        _cpu.Cycles.Should().Be(13);
    }

    [Fact]
    public void Djnz_DoesNotJumpWhenBZero()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0x01;
        _cpu.Memory.Write(0x0100, 0x10);
        _cpu.Memory.Write(0x0101, 0x05);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x00);
        _cpu.Regs.PC.Should().Be(0x0102);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void CallNn_CallsSubroutine()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Write(0x0100, 0xCD);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Regs.SP.Should().Be(0xEFFE);
        _cpu.Memory.Read(0xEFFE).Should().Be(0x03);
        _cpu.Memory.Read(0xEFFF).Should().Be(0x01);
        _cpu.Cycles.Should().Be(17);
    }

    [Fact]
    public void Ret_ReturnsFromSubroutine()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xEFFE;
        _cpu.Memory.Write(0xEFFE, 0x00);
        _cpu.Memory.Write(0xEFFF, 0x80);
        _cpu.Memory.Write(0x0100, 0xC9);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Regs.SP.Should().Be(0xF000);
        _cpu.Cycles.Should().Be(10);
    }

    [Fact]
    public void CallRet_RoundTrip()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Write(0x0100, 0xCD);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);
        _cpu.Memory.Write(0x8000, 0xC9);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x8000);

        _cpu.Step();
        _cpu.Regs.PC.Should().Be(0x0103);
        _cpu.Regs.SP.Should().Be(0xF000);
    }

    [Theory]
    [InlineData(0xC7, 0x00)]
    [InlineData(0xCF, 0x08)]
    [InlineData(0xD7, 0x10)]
    [InlineData(0xDF, 0x18)]
    [InlineData(0xE7, 0x20)]
    [InlineData(0xEF, 0x28)]
    [InlineData(0xF7, 0x30)]
    [InlineData(0xFF, 0x38)]
    public void Rst_JumpsToPageZero(byte opcode, ushort address)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Write(0x0100, opcode);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(address);
        _cpu.Regs.SP.Should().Be(0xEFFE);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void Di_DisablesInterrupts()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Iff1 = true;
        _cpu.Iff2 = true;
        _cpu.Memory.Write(0x0100, 0xF3);

        _cpu.Step();

        _cpu.Iff1.Should().BeFalse();
        _cpu.Iff2.Should().BeFalse();
    }

    [Fact]
    public void Ei_EnablesInterrupts()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Iff1 = false;
        _cpu.Iff2 = false;
        _cpu.Memory.Write(0x0100, 0xFB);

        _cpu.Step();

        _cpu.Iff1.Should().BeTrue();
        _cpu.Iff2.Should().BeTrue();
    }

    [Fact]
    public void CallCcNn_CallsWhenConditionMet()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.SetFlag(CpuFlags.Zero, true);
        _cpu.Memory.Write(0x0100, 0xCC);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Cycles.Should().Be(17);
    }

    [Fact]
    public void CallCcNn_DoesNotCallWhenConditionNotMet()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.SetFlag(CpuFlags.Zero, false);
        _cpu.Memory.Write(0x0100, 0xCC);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0103);
        _cpu.Cycles.Should().Be(10);
    }

    [Fact]
    public void RetCc_ReturnsWhenConditionMet()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xEFFE;
        _cpu.Regs.SetFlag(CpuFlags.Zero, true);
        _cpu.Memory.Write(0xEFFE, 0x00);
        _cpu.Memory.Write(0xEFFF, 0x80);
        _cpu.Memory.Write(0x0100, 0xC8);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x8000);
        _cpu.Regs.SP.Should().Be(0xF000);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void RetCc_DoesNotReturnWhenConditionNotMet()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xEFFE;
        _cpu.Regs.SetFlag(CpuFlags.Zero, false);
        _cpu.Memory.Write(0x0100, 0xC8);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0101);
        _cpu.Cycles.Should().Be(5);
    }
}
