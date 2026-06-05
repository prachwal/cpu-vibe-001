using FluentAssertions;
using Z80.Core;

namespace Z80.Tests;

public class PushMcatTest
{
    [Fact]
    public void StackPush_WritesHighByte_AtSpPlusOne_LowByte_AtSp()
    {
        var cpu = new Cpu();
        cpu.Regs.SP = 0x1D8B;

        cpu.StackPush(0xABCD);

        cpu.Regs.SP.Should().Be(0x1D89);
        cpu.Memory.Read((ushort)0x1D89).Should().Be(0xCD, "low byte at SP");
        cpu.Memory.Read((ushort)0x1D8A).Should().Be(0xAB, "high byte at SP+1");
    }

    [Fact]
    public void PushIX_WritesCorrectValues()
    {
        var cpu = new Cpu();
        cpu.Regs.SP = 0x1D8B;
        cpu.Regs.IX = 0xABCD;

        // Write DD E5 at PC=0x0100
        cpu.Memory.Write(0x0100, 0xDD);
        cpu.Memory.Write(0x0101, 0xE5);
        cpu.Regs.PC = 0x0100;
        cpu.Step();
        cpu.Step();

        cpu.Regs.SP.Should().Be((ushort)(0x1D8B - 2));
        cpu.Memory.Read((ushort)0x1D89).Should().Be(0xCD, "low byte at SP");
        cpu.Memory.Read((ushort)0x1D8A).Should().Be(0xAB, "high byte at SP+1");
    }

    [Fact]
    public void PushIY_WritesCorrectValues()
    {
        var cpu = new Cpu();
        cpu.Regs.SP = 0x1D8B;
        cpu.Regs.IY = 0x1234;

        cpu.Memory.Write(0x0100, 0xFD);
        cpu.Memory.Write(0x0101, 0xE5);
        cpu.Regs.PC = 0x0100;
        cpu.Step();
        cpu.Step();

        cpu.Regs.SP.Should().Be((ushort)(0x1D8B - 2));
        cpu.Memory.Read((ushort)0x1D89).Should().Be(0x34, "low byte at SP");
        cpu.Memory.Read((ushort)0x1D8A).Should().Be(0x12, "high byte at SP+1");
    }

    [Fact]
    public void SixPushes_FillsMcat_Correctly()
    {
        var cpu = new Cpu();
        cpu.Regs.SP = 0x1D8B;

        cpu.Regs.A = 0x11; cpu.Regs.F = 0x22;
        cpu.Regs.B = 0x33; cpu.Regs.C = 0x44;
        cpu.Regs.D = 0x55; cpu.Regs.E = 0x66;
        cpu.Regs.H = 0x77; cpu.Regs.L = 0x88;
        cpu.Regs.IX = 0xAA2B;
        cpu.Regs.IY = 0xBB99;

        // push iy, push ix, push hl, push de, push bc, push af
        byte[] code = [0xFD, 0xE5, 0xDD, 0xE5, 0xE5, 0xD5, 0xC5, 0xF5];
        cpu.Memory.Load(0x0100, code);
        cpu.Regs.PC = 0x0100;
        for (int i = 0; i < code.Length; i++)
            cpu.Step();

        cpu.Regs.SP.Should().Be((ushort)(0x1D8B - 12));

        cpu.Memory.Read((ushort)0x1D7F).Should().Be(0x22, "F");
        cpu.Memory.Read((ushort)0x1D80).Should().Be(0x11, "A");
        cpu.Memory.Read((ushort)0x1D81).Should().Be(0x44, "C");
        cpu.Memory.Read((ushort)0x1D82).Should().Be(0x33, "B");
        cpu.Memory.Read((ushort)0x1D83).Should().Be(0x66, "E");
        cpu.Memory.Read((ushort)0x1D84).Should().Be(0x55, "D");
        cpu.Memory.Read((ushort)0x1D85).Should().Be(0x88, "L");
        cpu.Memory.Read((ushort)0x1D86).Should().Be(0x77, "H");
        cpu.Memory.Read((ushort)0x1D87).Should().Be(0x2B, "IXL");
        cpu.Memory.Read((ushort)0x1D88).Should().Be(0xAA, "IXH");
        cpu.Memory.Read((ushort)0x1D89).Should().Be(0x99, "IYL");
        cpu.Memory.Read((ushort)0x1D8A).Should().Be(0xBB, "IYH");
    }
}
