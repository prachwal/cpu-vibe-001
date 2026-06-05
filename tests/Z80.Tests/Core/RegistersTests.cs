namespace Z80.Tests.Core;

public class RegistersTests
{
    [Fact]
    public void Reset_SetsDefaultValues()
    {
        var regs = new Registers();
        regs.A = 0xFF;
        regs.B = 0x42;
        regs.SP = 0x1234;
        regs.PC = 0x5678;

        regs.Reset();

        regs.A.Should().Be(0);
        regs.B.Should().Be(0);
        regs.SP.Should().Be(0xF000);
        regs.PC.Should().Be(0x0000);
    }

    [Fact]
    public void BC_CombinesBAndC()
    {
        var regs = new Registers();
        regs.B = 0x12;
        regs.C = 0x34;

        regs.BC.Should().Be(0x1234);
    }

    [Fact]
    public void BC_SplitCorrectly()
    {
        var regs = new Registers();
        regs.BC = 0xABCD;

        regs.B.Should().Be(0xAB);
        regs.C.Should().Be(0xCD);
    }

    [Fact]
    public void HL_CombinesHAndL()
    {
        var regs = new Registers();
        regs.H = 0xFF;
        regs.L = 0x01;

        regs.HL.Should().Be(0xFF01);
    }

    [Fact]
    public void HL_SplitCorrectly()
    {
        var regs = new Registers();
        regs.HL = 0x8000;

        regs.H.Should().Be(0x80);
        regs.L.Should().Be(0x00);
    }

    [Fact]
    public void AF_CombinesAAndF()
    {
        var regs = new Registers();
        regs.A = 0x42;
        regs.F = 0xC5;

        regs.AF.Should().Be(0x42C5);
    }

    [Fact]
    public void DE_CombinesDAndE()
    {
        var regs = new Registers();
        regs.D = 0xBE;
        regs.E = 0xEF;

        regs.DE.Should().Be(0xBEEF);
    }

    [Theory]
    [InlineData(0x00, true)]
    [InlineData(0x01, false)]
    [InlineData(0x03, true)]
    [InlineData(0x07, false)]
    [InlineData(0xFF, true)]
    public void CalculateParity(byte value, bool expected)
    {
        Registers.CalculateParity(value).Should().Be(expected);
    }
}
