using FluentAssertions;

namespace Mos6502.Tests;

public class StaTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void StaZeroPage_StoreAtoMemory()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0x85);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x0010).Should().Be(0x42);
    }

    [Fact]
    public void StaZeroPage_DoesNotAffectA()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0x85);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.A.Should().Be(0x42);
    }

    [Fact]
    public void StaZeroPage_DoesNotAffectFlags()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.P = CpuFlags.Zero | CpuFlags.Negative;
        _cpu.Memory.Write(0x0200, 0x85);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Zero);
        _cpu.Regs.P.Should().HaveFlag(CpuFlags.Negative);
    }

    [Fact]
    public void StaZeroPage_CyclesAre3()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0x85);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }

    [Fact]
    public void StaZeroPageX_StoreAtoZpPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x95);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x0015).Should().Be(0x42);
    }

    [Fact]
    public void StaZeroPageX_WrapsAround()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x10;
        _cpu.Memory.Write(0x0200, 0x95);
        _cpu.Memory.Write(0x0201, 0xF0);

        _cpu.Step();

        _cpu.Memory.Read(0x0000).Should().Be(0x42);
    }

    [Fact]
    public void StaZeroPageX_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x95);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void StaAbsolute_StoreAtoAddress()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0x8D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0x42);
    }

    [Fact]
    public void StaAbsolute_CyclesAre4()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Memory.Write(0x0200, 0x8D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void StaAbsoluteX_StoreAtoAddrPlusX()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x9D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0x42);
    }

    [Fact]
    public void StaAbsoluteX_CyclesAre5()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x05;
        _cpu.Memory.Write(0x0200, 0x9D);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void StaAbsoluteY_StoreAtoAddrPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x99);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0x42);
    }

    [Fact]
    public void StaAbsoluteY_CyclesAre5()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x99);
        _cpu.Memory.Write(0x0201, 0x00);
        _cpu.Memory.Write(0x0202, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(5);
    }

    [Fact]
    public void StaIndirectX_StoreAtoPointer()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0x81);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1000).Should().Be(0x42);
    }

    [Fact]
    public void StaIndirectX_CyclesAre6()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.X = 0x04;
        _cpu.Memory.Write(0x0200, 0x81);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0024, 0x00);
        _cpu.Memory.Write(0x0025, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void StaIndirectY_StoreAtoPointerPlusY()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x91);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);

        _cpu.Step();

        _cpu.Memory.Read(0x1005).Should().Be(0x42);
    }

    [Fact]
    public void StaIndirectY_CyclesAre6()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x42;
        _cpu.Regs.Y = 0x05;
        _cpu.Memory.Write(0x0200, 0x91);
        _cpu.Memory.Write(0x0201, 0x20);
        _cpu.Memory.Write(0x0020, 0x00);
        _cpu.Memory.Write(0x0021, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(6);
    }

    [Fact]
    public void StaZeroPage_CanOverwriteItself()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.A = 0x00;
        _cpu.Memory.Write(0x0200, 0x85);
        _cpu.Memory.Write(0x0201, 0x02);

        _cpu.Step();

        _cpu.Memory.Read(0x0002).Should().Be(0x00);
    }
}
