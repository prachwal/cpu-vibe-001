namespace Z80.Tests;

public class LdTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void LdB_C_LoadsCIntoB()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.C = 0x42;
        _cpu.Memory.Write(0x0100, 0x41);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x42);
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void LdD_A_LoadsAIntoD()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0xAB;
        _cpu.Memory.Write(0x0100, 0x57);

        _cpu.Step();

        _cpu.Regs.D.Should().Be(0xAB);
    }

    [Fact]
    public void LdA_B_LoadsBIntoA()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0xCD;
        _cpu.Memory.Write(0x0100, 0x78);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xCD);
    }

    [Fact]
    public void LdB_B_LoadsBIntoB()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0x55;
        _cpu.Memory.Write(0x0100, 0x40);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x55);
    }

    [Fact]
    public void LdE_L_LoadsLIntoE()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.L = 0x99;
        _cpu.Memory.Write(0x0100, 0x5D);

        _cpu.Step();

        _cpu.Regs.E.Should().Be(0x99);
    }

    [Fact]
    public void LdB_N_LoadsImmediateIntoB()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x06);
        _cpu.Memory.Write(0x0101, 0x42);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x42);
        _cpu.Regs.PC.Should().Be(0x0102);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void LdA_N_LoadsImmediateIntoA()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x3E);
        _cpu.Memory.Write(0x0101, 0xFF);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0xFF);
    }

    [Fact]
    public void LdL_N_LoadsImmediateIntoL()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x2E);
        _cpu.Memory.Write(0x0101, 0x34);

        _cpu.Step();

        _cpu.Regs.L.Should().Be(0x34);
    }

    [Fact]
    public void LdHl_N_LoadsImmediateIntoMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.HL = 0x8000;
        _cpu.Memory.Write(0x0100, 0x36);
        _cpu.Memory.Write(0x0101, 0xAB);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0xAB);
        _cpu.Regs.PC.Should().Be(0x0102);
        _cpu.Cycles.Should().Be(10);
    }

    [Fact]
    public void LdB_H_LoadsHIntoB()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0x12;
        _cpu.Memory.Write(0x0100, 0x44);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x12);
    }

    [Fact]
    public void LdH_B_LoadsBIntoH()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0xAB;
        _cpu.Memory.Write(0x0100, 0x60);

        _cpu.Step();

        _cpu.Regs.H.Should().Be(0xAB);
    }

    [Fact]
    public void LdH_L_LoadsLIntoH()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.L = 0x34;
        _cpu.Memory.Write(0x0100, 0x65);

        _cpu.Step();

        _cpu.Regs.H.Should().Be(0x34);
    }

    [Fact]
    public void LdB_Hl_LoadsFromHLIntoB()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0x80;
        _cpu.Regs.L = 0x00;
        _cpu.Memory.Write(0x8000, 0x55);
        _cpu.Memory.Write(0x0100, 0x46);

        _cpu.Step();

        _cpu.Regs.B.Should().Be(0x55);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void LdHl_B_LoadsBIntoMemory()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0x80;
        _cpu.Regs.L = 0x00;
        _cpu.Regs.B = 0x77;
        _cpu.Memory.Write(0x0100, 0x70);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x77);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void LdA_Bc_LoadsFromBC()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.B = 0x80;
        _cpu.Regs.C = 0x00;
        _cpu.Memory.Write(0x8000, 0x42);
        _cpu.Memory.Write(0x0100, 0x02);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void LdA_De_LoadsFromDE()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.D = 0x80;
        _cpu.Regs.E = 0x00;
        _cpu.Memory.Write(0x8000, 0x99);
        _cpu.Memory.Write(0x0100, 0x12);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x99);
    }

    [Fact]
    public void LdBc_A_StoresAAtBC()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.B = 0x80;
        _cpu.Regs.C = 0x00;
        _cpu.Memory.Write(0x0100, 0x0A);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x42);
        _cpu.Cycles.Should().Be(7);
    }

    [Fact]
    public void LdDe_A_StoresAAtDE()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x77;
        _cpu.Regs.D = 0x80;
        _cpu.Regs.E = 0x00;
        _cpu.Memory.Write(0x0100, 0x1A);

        _cpu.Step();

        _cpu.Memory.Read(0x8000).Should().Be(0x77);
    }

    [Fact]
    public void Halt_SetsHaltedFlag()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x76);

        _cpu.Step();

        _cpu.Halted.Should().BeTrue();
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void Halt_DoesNotAdvancePC()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Memory.Write(0x0100, 0x76);

        _cpu.Step();
        _cpu.Step();
        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0101);
        _cpu.Cycles.Should().Be(12);
    }

    [Fact]
    public void AllLdRrOpcodes_TakeCorrectCycles()
    {
        for (int op = 0x40; op <= 0x7F; op++)
        {
            if (op == 0x76) continue;

            _cpu.Regs.PC = 0x0100;
            _cpu.Regs.B = 0; _cpu.Regs.C = 0; _cpu.Regs.D = 0;
            _cpu.Regs.E = 0; _cpu.Regs.H = 0; _cpu.Regs.L = 0;
            _cpu.Regs.A = 0;
            _cpu.Memory.Write(0x0100, (byte)op);
            _cpu.Memory.Write(0x8000, 0x42);
            _cpu.Cycles = 0;

            _cpu.Step();

            bool involvesHL = (op & 7) == 6 || ((op >> 3) & 7) == 6;
            long expected = involvesHL ? 7 : 4;
            _cpu.Cycles.Should().Be(expected, $"opcode 0x{op:X2} should take {expected} cycles");
        }
    }

    [Fact]
    public void LdE_H_LoadsHIntoE()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.H = 0xBE;
        _cpu.Memory.Write(0x0100, 0x5C);

        _cpu.Step();

        _cpu.Regs.E.Should().Be(0xBE);
    }

    [Fact]
    public void LdC_D_LoadsDIntoC()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.D = 0xEF;
        _cpu.Memory.Write(0x0100, 0x4A);

        _cpu.Step();

        _cpu.Regs.C.Should().Be(0xEF);
    }

    [Fact]
    public void LdA_A_LoadsAIntoA()
    {
        _cpu.Regs.PC = 0x0100;
        _cpu.Regs.A = 0x99;
        _cpu.Memory.Write(0x0100, 0x7F);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x99);
    }
}
