namespace Z80.Tests.Core;

public class CpuTests
{
    [Fact]
    public void NopExecutes_CyclesIncrement()
    {
        var cpu = new Cpu();
        cpu.Memory.Write(0x0000, 0x00);

        cpu.Step();

        cpu.Cycles.Should().Be(4);
        cpu.Regs.PC.Should().Be(0x0001);
    }

    [Fact]
    public void StackPushPop_WorksCorrectly()
    {
        var cpu = new Cpu();
        cpu.Regs.SP = 0xF000;

        cpu.StackPush(0x1234);
        cpu.Regs.SP.Should().Be(0xEFFE);

        ushort value = cpu.StackPop();
        value.Should().Be(0x1234);
        cpu.Regs.SP.Should().Be(0xF000);
    }

    [Fact]
    public void FetchByte_IncrementsPC()
    {
        var cpu = new Cpu();
        cpu.Regs.PC = 0x0100;
        cpu.Memory.Write(0x0100, 0xAB);

        byte result = cpu.FetchByte();

        result.Should().Be(0xAB);
        cpu.Regs.PC.Should().Be(0x0101);
    }

    [Fact]
    public void FetchWord_IncrementsPCBy2()
    {
        var cpu = new Cpu();
        cpu.Regs.PC = 0x0100;
        cpu.Memory.Write(0x0100, 0x34);
        cpu.Memory.Write(0x0101, 0x12);

        ushort result = cpu.FetchWord();

        result.Should().Be(0x1234);
        cpu.Regs.PC.Should().Be(0x0102);
    }
}
