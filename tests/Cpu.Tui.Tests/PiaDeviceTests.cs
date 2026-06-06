using FluentAssertions;
using Xunit;
using Cpu.Tui.Devices.Pia;

namespace Cpu.Tui.Tests;

public class PiaDeviceTests
{
    private const ushort BaseAddr = 0x8000;

    [Fact]
    public void Accepts_ReturnsTrue_ForBaseAddress()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Accepts(BaseAddr).Should().BeTrue();
    }

    [Fact]
    public void Accepts_ReturnsTrue_ForLastRegister()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Accepts((ushort)(BaseAddr + 5)).Should().BeTrue();
    }

    [Fact]
    public void Accepts_ReturnsFalse_OutsideRange()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Accepts((ushort)(BaseAddr - 1)).Should().BeFalse();
        pia.Accepts((ushort)(BaseAddr + 6)).Should().BeFalse();
    }

    [Fact]
    public void Reset_ClearsAllRegisters()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.DDRA), 0xFF);
        pia.Write((ushort)(BaseAddr + PiaDevice.PRA), 0xAA);

        pia.Reset();

        pia.DdrA.Should().Be(0);
        pia.PortA.Should().Be(0);
    }

    [Fact]
    public void Write_DDR_A()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.DDRA), 0xF0);
        pia.DdrA.Should().Be(0xF0);
    }

    [Fact]
    public void Write_Port_A()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.PRA), 0x55);
        pia.PortA.Should().Be(0x55);
    }

    [Fact]
    public void Write_CRA()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRA), 0x1F);
        pia.Read((ushort)(BaseAddr + PiaDevice.CRA)).Should().Be(0x1F);
    }

    [Fact]
    public void Read_DDR_A()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.DDRA), 0xAA);
        pia.Read((ushort)(BaseAddr + PiaDevice.DDRA)).Should().Be(0xAA);
    }

    [Fact]
    public void Read_Port_A_Respects_DDR()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.DDRA), 0xF0);
        pia.Write((ushort)(BaseAddr + PiaDevice.PRA), 0x30);

        byte result = pia.ReadPortA();
        result.Should().Be(0x3F);
    }

    [Fact]
    public void PortB_WorksSame_AsPortA()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.DDRB), 0xFF);
        pia.Write((ushort)(BaseAddr + PiaDevice.PRB), 0x42);
        pia.PortB.Should().Be(0x42);
    }

    [Fact]
    public void IrqNotPending_AfterReset()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.IrqPending.Should().BeFalse();
    }

    [Fact]
    public void SetCa1_RisingEdge_TriggersIRQ()
    {
        var pia = new PiaDevice(BaseAddr);
        var irqs = new List<IrqSource>();
        var mock = new MockTerminal(irqs);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRA), PiaDevice.CRA_IRQ1);

        pia.SetCa1(false);
        pia.SetCa1(true);

        pia.IrqPending.Should().BeTrue();
        irqs.Should().Contain(IrqSource.CA1);
    }

    [Fact]
    public void SetCa1_FallingEdge_TriggersIRQ_WhenConfigured()
    {
        var pia = new PiaDevice(BaseAddr);
        var irqs = new List<IrqSource>();
        var mock = new MockTerminal(irqs);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRA), PiaDevice.CRA_IRQ1 | PiaDevice.CRA_EDGE);

        pia.SetCa1(true);
        pia.SetCa1(false);

        pia.IrqPending.Should().BeTrue();
        irqs.Should().Contain(IrqSource.CA1);
    }

    [Fact]
    public void ClearIrq_ClearsFlag()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRA), PiaDevice.CRA_IRQ1);
        pia.SetCa1(false);
        pia.SetCa1(true);

        pia.ClearIrq(IrqSource.CA1);
        pia.IrqPending.Should().BeFalse();
    }

    [Fact]
    public void SetCa2_RisingEdge_TriggersIRQ()
    {
        var pia = new PiaDevice(BaseAddr);
        var irqs = new List<IrqSource>();
        var mock = new MockTerminal(irqs);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRA), PiaDevice.CRA_IRQ2);

        pia.SetCa2(false);
        pia.SetCa2(true);

        pia.IrqPending.Should().BeTrue();
        irqs.Should().Contain(IrqSource.CA2);
    }

    [Fact]
    public void SetCb1_RisingEdge_TriggersIRQ()
    {
        var pia = new PiaDevice(BaseAddr);
        var irqs = new List<IrqSource>();
        var mock = new MockTerminal(irqs);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRB), PiaDevice.CRB_IRQ1);

        pia.SetCb1(false);
        pia.SetCb1(true);

        pia.IrqPending.Should().BeTrue();
        irqs.Should().Contain(IrqSource.CB1);
    }

    [Fact]
    public void SetCb2_RisingEdge_TriggersIRQ()
    {
        var pia = new PiaDevice(BaseAddr);
        var irqs = new List<IrqSource>();
        var mock = new MockTerminal(irqs);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRB), PiaDevice.CRB_IRQ2);

        pia.SetCb2(false);
        pia.SetCb2(true);

        pia.IrqPending.Should().BeTrue();
        irqs.Should().Contain(IrqSource.CB2);
    }

    [Fact]
    public void PortAWrite_CallsTerminalCallback()
    {
        var pia = new PiaDevice(BaseAddr);
        var writes = new List<(byte val, byte old)>();
        var mock = new MockTerminal(writes);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.PRA), 0x42);

        writes.Should().HaveCount(1);
        writes[0].val.Should().Be(0x42);
    }

    [Fact]
    public void PortBWrite_CallsTerminalCallback()
    {
        var pia = new PiaDevice(BaseAddr);
        var writes = new List<(byte val, byte old)>();
        var mock = new MockTerminal(writes, isB: true);
        pia.AttachTerminal(mock);
        pia.Write((ushort)(BaseAddr + PiaDevice.PRB), 0x99);

        writes.Should().HaveCount(1);
        writes[0].val.Should().Be(0x99);
    }

    [Fact]
    public void MultipleIRQs_AllTracked()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRA), PiaDevice.CRA_IRQ1 | PiaDevice.CRA_IRQ2);
        pia.Write((ushort)(BaseAddr + PiaDevice.CRB), PiaDevice.CRB_IRQ1 | PiaDevice.CRB_IRQ2);

        pia.SetCa1(false); pia.SetCa1(true);
        pia.SetCa2(false); pia.SetCa2(true);
        pia.SetCb1(false); pia.SetCb1(true);
        pia.SetCb2(false); pia.SetCb2(true);

        pia.IrqPending.Should().BeTrue();
    }

    private class MockTerminal : IPiaTerminal
    {
        private readonly List<IrqSource>? _irqs;
        private readonly List<(byte val, byte old)>? _writes;

        public MockTerminal(List<IrqSource> irqs) => _irqs = irqs;
        public MockTerminal(List<(byte val, byte old)> writes, bool isB = false) => _writes = writes;

        public void OnPortAWrite(byte newValue, byte oldValue) => _writes?.Add((newValue, oldValue));
        public void OnPortBWrite(byte newValue, byte oldValue) => _writes?.Add((newValue, oldValue));
        public void OnIrq(IrqSource source) => _irqs?.Add(source);
    }
}
