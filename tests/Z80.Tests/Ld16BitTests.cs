namespace Z80.Tests;

public class Ld16BitTests
{
    private readonly Cpu _cpu = new();

    [Theory]
    [InlineData(0x01, "BC")]
    [InlineData(0x11, "DE")]
    [InlineData(0x21, "HL")]
    [InlineData(0x31, "SP")]
    public void LdRrNn_Loads16BitImmediate(byte opcode, string regName)
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, opcode);
        _cpu.Memory.Write(0x0101, 0x34);
        _cpu.Memory.Write(0x0102, 0x12);

        _cpu.Step();

        switch (regName)
        {
            case "BC": _cpu.Regs.BC.Should().Be(0x1234); break;
            case "DE": _cpu.Regs.DE.Should().Be(0x1234); break;
            case "HL": _cpu.Regs.HL.Should().Be(0x1234); break;
            case "SP": _cpu.Regs.SP.Should().Be(0x1234); break;
        }
        _cpu.Cycles.Should().Be(10);
    }

    [Fact]
    public void LdNnHl_StoresHLToMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0xAB;
        _cpu.Regs.L = 0xCD;
        _cpu.Memory.Write(0x0100, 0x22);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0xCD);
        _cpu.Memory.Read(0x8001).Should().Be(0xAB);
        _cpu.Cycles.Should().Be(16);
    }

    [Fact]
    public void LdHlNn_LoadsHLFromMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x2A);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);
        _cpu.Memory.Write(0x8000, 0x34);
        _cpu.Memory.Write(0x8001, 0x12);

        _cpu.Step();

        _cpu.Regs.HL.Should().Be(0x1234);
        _cpu.Cycles.Should().Be(16);
    }

    [Fact]
    public void LdNnA_StoresAToMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0100, 0x32);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x42);
        _cpu.Cycles.Should().Be(13);
    }

    [Fact]
    public void LdANn_LoadsAFromMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x3A);
        _cpu.Memory.Write(0x0101, 0x00);
        _cpu.Memory.Write(0x0102, 0x80);
        _cpu.Memory.Write(0x8000, 0x99);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x99);
        _cpu.Cycles.Should().Be(13);
    }

    [Fact]
    public void LdSpHl_LoadsSPFromHL()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0xF000;
        _cpu.Memory.Write(0x0100, 0xF9);

        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0xF000);
        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void PushPop_BC_WorksCorrectly()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.B = 0x12;
        _cpu.Regs.C = 0x34;
        _cpu.Memory.Write(0x0100, 0xC5);

        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0x0EFFE);
        _cpu.Memory.Read(0xEFFF).Should().Be(0x12);
        _cpu.Memory.Read(0x0EFFE).Should().Be(0x34);

        _cpu.Regs.PC = 0x0101;
        _cpu.Memory.Write(0x0101, 0xC1);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x12);
        _cpu.Regs.C.Should().Be(0x34);
        _cpu.Regs.SP.Should().Be(0xF000);
    }

    [Fact]
    public void PushPop_DE_WorksCorrectly()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.D = 0xAB;
        _cpu.Regs.E = 0xCD;
        _cpu.Memory.Write(0x0100, 0xD5);

        _cpu.Step();

        _cpu.Regs.PC = 0x0101;
        _cpu.Memory.Write(0x0101, 0xD1);

        _cpu.Step();

        _cpu.Regs.D.Should().Be(0xAB);
        _cpu.Regs.E.Should().Be(0xCD);
    }

    [Fact]
    public void PushPop_AF_RestoresFlags()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.F = 0xC5;
        _cpu.Memory.Write(0x0100, 0xF5);

        _cpu.Step();

        _cpu.Regs.PC = 0x0101;
        _cpu.Regs.A = 0;
        _cpu.Regs.F = 0;
        _cpu.Memory.Write(0x0101, 0xF1);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
        _cpu.Regs.F.Should().Be(0xC5);
    }

    [Fact]
    public void ExDeHl_SwapsDEAndHL()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.D = 0x12;
        _cpu.Regs.E = 0x34;
        _cpu.Regs.H = 0xAB;
        _cpu.Regs.L = 0xCD;
        _cpu.Memory.Write(0x0100, 0xEB);

        _cpu.Step();

        _cpu.Regs.D.Should().Be(0xAB);
        _cpu.Regs.E.Should().Be(0xCD);
        _cpu.Regs.H.Should().Be(0x12);
        _cpu.Regs.L.Should().Be(0x34);
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void ExAfAf_SwapsAFAndShadow()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x11;
        _cpu.Regs.F = 0x22;
        _cpu.Regs.A_ = 0x33;
        _cpu.Regs.F_ = 0x44;
        _cpu.Memory.Write(0x0100, 0x08);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x33);
        _cpu.Regs.F.Should().Be(0x44);
        _cpu.Regs.A_.Should().Be(0x11);
        _cpu.Regs.F_.Should().Be(0x22);
    }

    [Fact]
    public void Exx_SwapsRegisterSets()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0x11; _cpu.Regs.C = 0x22;
        _cpu.Regs.D = 0x33; _cpu.Regs.E = 0x44;
        _cpu.Regs.H = 0x55; _cpu.Regs.L = 0x66;
        _cpu.Regs.B_ = 0x77; _cpu.Regs.C_ = 0x88;
        _cpu.Regs.D_ = 0x99; _cpu.Regs.E_ = 0xAA;
        _cpu.Regs.H_ = 0xBB; _cpu.Regs.L_ = 0xCC;
        _cpu.Memory.Write(0x0100, 0xD9);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x77);
        _cpu.Regs.C.Should().Be(0x88);
        _cpu.Regs.D.Should().Be(0x99);
        _cpu.Regs.E.Should().Be(0xAA);
        _cpu.Regs.H.Should().Be(0xBB);
        _cpu.Regs.L.Should().Be(0xCC);
        _cpu.Regs.B_.Should().Be(0x11);
        _cpu.Regs.C_.Should().Be(0x22);
        _cpu.Regs.D_.Should().Be(0x33);
        _cpu.Regs.E_.Should().Be(0x44);
        _cpu.Regs.H_.Should().Be(0x55);
        _cpu.Regs.L_.Should().Be(0x66);
    }

    [Fact]
    public void ExSpHl_SwapsTopOfStackWithHL()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.H = 0x11;
        _cpu.Regs.L = 0x22;
        _cpu.Memory.Write(0xF000, 0x33);
        _cpu.Memory.Write(0xF001, 0x44);
        _cpu.Memory.Write(0x0100, 0xE3);

        _cpu.Step();

        _cpu.Regs.H.Should().Be(0x44);
        _cpu.Regs.L.Should().Be(0x33);
        _cpu.Memory.Read(0xF000).Should().Be(0x22);
        _cpu.Memory.Read(0xF001).Should().Be(0x11);
        _cpu.Cycles.Should().Be(19);
    }

    [Fact]
    public void PushPop_StackFlow_WorksCorrectly()
    {
        _cpu.Regs.SP = 0xF000;
        _cpu.Regs.HL = 0x1234;

        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0xE5);
        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0x0EFFE);

        _cpu.Regs.HL = 0x5678;
        _cpu.Regs.PC = 0x0101;
        _cpu.Memory.Write(0x0101, 0xE5);
        _cpu.Step();

        _cpu.Regs.SP.Should().Be(0x0EFFC);

        _cpu.Regs.PC = 0x0102;
        _cpu.Memory.Write(0x0102, 0xE1);
        _cpu.Step();
        _cpu.Regs.HL.Should().Be(0x5678);

        _cpu.Regs.PC = 0x0103;
        _cpu.Memory.Write(0x0103, 0xE1);
        _cpu.Step();
        _cpu.Regs.HL.Should().Be(0x1234);

        _cpu.Regs.SP.Should().Be(0xF000);
    }
}
