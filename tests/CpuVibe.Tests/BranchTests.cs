using FluentAssertions;

namespace CpuVibe.Tests;

public class BranchTests
{
    private readonly Cpu _cpu = new();

    [Fact]
    public void Bcc_BranchesWhenCarryClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x90);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Bcc_DoesNotBranchWhenCarrySet()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x90);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0202);
    }

    [Fact]
    public void Bcs_BranchesWhenCarrySet()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xB0);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Bcs_DoesNotBranchWhenCarryClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0xB0);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0202);
    }

    [Fact]
    public void Beq_BranchesWhenZeroSet()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xF0);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Beq_DoesNotBranchWhenZeroClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xF0);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0202);
    }

    [Fact]
    public void Bne_BranchesWhenZeroClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0200, 0xD0);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Bmi_BranchesWhenNegativeSet()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Negative;
        _cpu.Memory.Write(0x0200, 0x30);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Bpl_BranchesWhenNegativeClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Negative;
        _cpu.Memory.Write(0x0200, 0x10);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Bvc_BranchesWhenOverflowClear()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Overflow;
        _cpu.Memory.Write(0x0200, 0x50);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Bvs_BranchesWhenOverflowSet()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Overflow;
        _cpu.Memory.Write(0x0200, 0x70);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0212);
    }

    [Fact]
    public void Branch_NegativeOffset()
    {
        _cpu.Regs.PC = 0x0210;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x0210, 0xD0);
        _cpu.Memory.Write(0x0211, 0xFE);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0210);
    }

    [Fact]
    public void Branch_CrossesPage_ExtraCycle()
    {
        _cpu.Regs.PC = 0x02FD;
        _cpu.Regs.P &= ~CpuFlags.Zero;
        _cpu.Memory.Write(0x02FD, 0xD0);
        _cpu.Memory.Write(0x02FE, 0x02);

        _cpu.Step();

        _cpu.Regs.PC.Should().Be(0x0301);
        _cpu.Cycles.Should().Be(4);
    }

    [Fact]
    public void Bcc_CyclesAre2_WhenNotTaken()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P |= CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x90);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(2);
    }

    [Fact]
    public void Bcc_CyclesAre3_WhenTaken()
    {
        _cpu.Regs.PC = 0x0200;
        _cpu.Regs.P &= ~CpuFlags.Carry;
        _cpu.Memory.Write(0x0200, 0x90);
        _cpu.Memory.Write(0x0201, 0x10);

        _cpu.Step();

        _cpu.Cycles.Should().Be(3);
    }
}
