namespace Z80.Tests;

public class DdFdTests
{
    private readonly Cpu _cpu = new();

    private void SetupDD(byte ddOpcode)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0xDD);
        _cpu.Memory.Write(0x0101, ddOpcode);
    }

    private void SetupFD(byte fdOpcode)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0xFD);
        _cpu.Memory.Write(0x0101, fdOpcode);
    }

    [Fact]
    public void LdIxNn()
    {
        SetupDD(0x21);
        _cpu.Memory.Write(0x0102, 0x34);
        _cpu.Memory.Write(0x0103, 0x12);

        _cpu.Step();

        _cpu.Regs.IX.Should().Be(0x1234);
        _cpu.Cycles.Should().Be(14);
    }

    [Fact]
    public void LdIyNn()
    {
        SetupFD(0x21);
        _cpu.Memory.Write(0x0102, 0x78);
        _cpu.Memory.Write(0x0103, 0x56);

        _cpu.Step();

        _cpu.Regs.IY.Should().Be(0x5678);
    }

    [Fact]
    public void LdBIxD()
    {
        SetupDD(0x46);
        _cpu.Regs.IX = 0x8000;
        _cpu.Memory.Write(0x0102, 0x02);
        _cpu.Memory.Write(0x8002, 0xAB);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0xAB);
        _cpu.Cycles.Should().Be(19);
    }

    [Fact]
    public void LdDIxD()
    {
        SetupDD(0x56);
        _cpu.Regs.IX = 0x8000;
        _cpu.Memory.Write(0x0102, 0x05);
        _cpu.Memory.Write(0x8005, 0xCD);

        _cpu.Step();

        _cpu.Regs.D.Should().Be(0xCD);
    }

    [Fact]
    public void LdAIxD()
    {
        SetupDD(0x7E);
        _cpu.Regs.IX = 0x8000;
        _cpu.Memory.Write(0x0102, 0x03);
        _cpu.Memory.Write(0x8003, 0x42);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void LdIxDB()
    {
        SetupDD(0x70);
        _cpu.Regs.IX = 0x8000;
        _cpu.Regs.B = 0x55;
        _cpu.Memory.Write(0x0102, 0x04);

        _cpu.Step();

        _cpu.Memory.Read(0x8004).Should().Be(0x55);
        _cpu.Cycles.Should().Be(19);
    }

    [Fact]
    public void LdIxDA()
    {
        SetupDD(0x77);
        _cpu.Regs.IX = 0x8000;
        _cpu.Regs.A = 0x99;
        _cpu.Memory.Write(0x0102, 0x01);

        _cpu.Step();

        _cpu.Memory.Read(0x8001).Should().Be(0x99);
    }

    [Fact]
    public void LdIxDN()
    {
        SetupDD(0x36);
        _cpu.Regs.IX = 0x8000;
        _cpu.Memory.Write(0x0102, 0x05);
        _cpu.Memory.Write(0x0103, 0xFF);

        _cpu.Step();

        _cpu.Memory.Read(0x8005).Should().Be(0xFF);
        _cpu.Cycles.Should().Be(19);
    }

    [Fact]
    public void IncIxD()
    {
        SetupDD(0x34);
        _cpu.Regs.IX = 0x8000;
        _cpu.Memory.Write(0x0102, 0x03);
        _cpu.Memory.Write(0x8003, 0x7F);

        _cpu.Step();

        _cpu.Memory.Read(0x8003).Should().Be(0x80);
        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void DecIxD()
    {
        SetupDD(0x35);
        _cpu.Regs.IX = 0x8000;
        _cpu.Memory.Write(0x0102, 0x03);
        _cpu.Memory.Write(0x8003, 0x00);

        _cpu.Step();

        _cpu.Memory.Read(0x8003).Should().Be(0xFF);
        _cpu.Regs.IsSign.Should().BeTrue();
    }

    [Fact]
    public void AddIxDe()
    {
        SetupDD(0x19);
        _cpu.Regs.IX = 0x1000;
        _cpu.Regs.DE = 0x2000;

        _cpu.Step();

        _cpu.Regs.IX.Should().Be(0x3000);
        _cpu.Cycles.Should().Be(15);
    }

    [Fact]
    public void AddIyBc()
    {
        SetupFD(0x09);
        _cpu.Regs.IY = 0x5000;
        _cpu.Regs.BC = 0x1000;

        _cpu.Step();

        _cpu.Regs.IY.Should().Be(0x6000);
    }

    [Fact]
    public void DdFallback_LdBImmediate_ExecutesBaseInstructionAndConsumesOperand()
    {
        SetupDD(0x06);
        _cpu.Memory.Write(0x0102, 0x42);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x42);
        _cpu.Regs.PC.Should().Be(0x0103);
        _cpu.Cycles.Should().Be(11);
    }

    [Fact]
    public void FdFallback_JpNn_ExecutesBaseInstructionAndConsumesAddress()
    {
        SetupFD(0xC3);
        _cpu.Memory.Write(0x0102, 0x34);
        _cpu.Memory.Write(0x0103, 0x12);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x1234);
        _cpu.Cycles.Should().Be(14);
    }

    [Fact]
    public void DdFallback_IncB_ExecutesBaseInstruction()
    {
        SetupDD(0x04);
        _cpu.Regs.B = 0x7F;

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x80);
        _cpu.Regs.IsSign.Should().BeTrue();
        _cpu.Regs.PC.Should().Be(0x0102);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void Dd64_LdIxhIxh_IsUndocumentedNoOp()
    {
        SetupDD(0x64);
        Z80.Instructions.DdFd.NopDdFd.ResetCount();
        _cpu.Regs.IX = 0x1234;

        _cpu.Step();

        _cpu.Regs.IX.Should().Be(0x1234);
        Z80.Instructions.DdFd.NopDdFd.CallCount.Should().Be(0);
        _cpu.Regs.PC.Should().Be(0x0102);
        _cpu.Cycles.Should().Be(8);
    }

    [Fact]
    public void PushIx()
    {
        SetupDD(0xE5);
        _cpu.Regs.IX = 0x1234;
        _cpu.Regs.SP = 0xF000;

        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0xEFFE);
        _cpu.Memory.Read(0xEFFE).Should().Be(0x34);
        _cpu.Memory.Read(0xEFFF).Should().Be(0x12);
        _cpu.Cycles.Should().Be(15);
    }

    [Fact]
    public void PopIx()
    {
        SetupDD(0xE1);
        _cpu.Regs.SP = 0xEFFE;
        _cpu.Memory.Write(0xEFFE, 0x78);
        _cpu.Memory.Write(0xEFFF, 0x56);

        _cpu.Step();

        _cpu.Regs.IX.Should().Be(0x5678);
        _cpu.Regs.SP.Should().Be(0xF000);
        _cpu.Cycles.Should().Be(14);
    }

    [Fact]
    public void AddAIxD()
    {
        SetupDD(0x86);
        _cpu.Regs.IX = 0x8000;
        _cpu.Regs.A = 0x10;
        _cpu.Memory.Write(0x0102, 0x02);
        _cpu.Memory.Write(0x8002, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x15);
        _cpu.Cycles.Should().Be(19);
    }

    [Fact]
    public void SubAIxD()
    {
        SetupDD(0x96);
        _cpu.Regs.IX = 0x8000;
        _cpu.Regs.A = 0x20;
        _cpu.Memory.Write(0x0102, 0x01);
        _cpu.Memory.Write(0x8001, 0x05);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x1B);
    }

    [Fact]
    public void CpAIxD()
    {
        SetupDD(0xBE);
        _cpu.Regs.IX = 0x8000;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0102, 0x03);
        _cpu.Memory.Write(0x8003, 0x42);

        _cpu.Step();

        _cpu.Regs.IsZero.Should().BeTrue();
    }

    [Fact]
    public void LdNnIx()
    {
        SetupDD(0x22);
        _cpu.Regs.IX = 0x1234;
        _cpu.Memory.Write(0x0102, 0x00);
        _cpu.Memory.Write(0x0103, 0x80);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x34);
        _cpu.Memory.Read(0x8001).Should().Be(0x12);
    }

    [Fact]
    public void LdIxNn2()
    {
        SetupDD(0x2A);
        _cpu.Memory.Write(0x0102, 0x00);
        _cpu.Memory.Write(0x0103, 0x80);
        _cpu.Memory.Write(0x8000, 0x78);
        _cpu.Memory.Write(0x8001, 0x56);

        _cpu.Step();

        _cpu.Regs.IX.Should().Be(0x5678);
    }

    [Fact]
    public void NegativeDisplacement()
    {
        SetupDD(0x46);
        _cpu.Regs.IX = 0x8010;
        _cpu.Memory.Write(0x0102, 0xFE);
        _cpu.Memory.Write(0x800E, 0xBB);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0xBB);
    }

    [Fact]
    public void ExSpIx()
    {
        SetupDD(0xE3);
        _cpu.Regs.IX = 0x1234;
        _cpu.Regs.SP = 0xF000;
        _cpu.Memory.Write(0xF000, 0x78);
        _cpu.Memory.Write(0xF001, 0x56);

        _cpu.Step();

        _cpu.Regs.IX.Should().Be(0x5678);
        _cpu.Memory.Read(0xF000).Should().Be(0x34);
        _cpu.Memory.Read(0xF001).Should().Be(0x12);
        _cpu.Cycles.Should().Be(23);
    }
}
