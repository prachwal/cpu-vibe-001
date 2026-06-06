using FluentAssertions;
using Xunit;
using Z80.Core;
using Z80.Instructions;

namespace Z80.Tests.OpcodeTests;

public class AddIxTest
{
    [Fact]
    public void DD09_ADD_IX_BC()
    {
        var cpu = new Cpu();
        cpu.Regs.IX = 0x1000;
        cpu.Regs.BC = 0x0234;
        cpu.Memory.Write(0x0100, 0xDD);
        cpu.Memory.Write(0x0101, 0x09);
        cpu.Regs.PC = 0x0100;

        cpu.Step(); // DD prefix + ADD IX,BC (single step dispatches both)

        cpu.Regs.IX.Should().Be(0x1234);
        cpu.Regs.PC.Should().Be(0x0102, "DD prefix consumes DD + subopcode");
    }

    [Fact]
    public void DD09_ADD_IX_BC_WithCarry()
    {
        var cpu = new Cpu();
        cpu.Regs.IX = 0xFFFF;
        cpu.Regs.BC = 0x0001;
        cpu.Memory.Write(0x0100, 0xDD);
        cpu.Memory.Write(0x0101, 0x09);
        cpu.Regs.PC = 0x0100;

        cpu.Step();
        cpu.Step();

        cpu.Regs.IX.Should().Be(0x0000);
        cpu.Regs.IsCarry.Should().BeTrue();
    }

    [Fact]
    public void DD23_INC_IX()
    {
        var cpu = new Cpu();
        cpu.Regs.IX = 0x12FF;
        cpu.Memory.Write(0x0100, 0xDD);
        cpu.Memory.Write(0x0101, 0x23);
        cpu.Regs.PC = 0x0100;

        cpu.Step(); // DD prefix + INC IX

        cpu.Regs.IX.Should().Be(0x1300);
        cpu.Regs.PC.Should().Be(0x0102);
    }

    [Fact]
    public void FD09_ADD_IY_BC()
    {
        var cpu = new Cpu();
        cpu.Regs.IY = 0x1000;
        cpu.Regs.BC = 0x0234;
        cpu.Memory.Write(0x0100, 0xFD);
        cpu.Memory.Write(0x0101, 0x09);
        cpu.Regs.PC = 0x0100;

        cpu.Step(); // FD prefix + ADD IY,BC

        cpu.Regs.IY.Should().Be(0x1234);
        cpu.Regs.PC.Should().Be(0x0102);
    }
}
