using FluentAssertions;
using Xunit;
using Cpu.Tui.Devices.Pia;

namespace Cpu.Tui.Tests;

public class PiaDeviceTests
{
    private const ushort BaseAddr = 0x8000;

    // PIA memory map offsets (not internal register indices):
    // offset 0 = DDRA (CRA bit2=0) or PRA (CRA bit2=1)
    // offset 1 = CRA
    // offset 2 = DDRB (CRB bit2=0) or PRB (CRB bit2=1)
    // offset 3 = CRB
    private const ushort MemPRA = 0;
    private const ushort MemCRA = 1;
    private const ushort MemPRB = 2;
    private const ushort MemCRB = 3;

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
        pia.Write(BaseAddr, 0xFF);                       // DDRA
        pia.Write((ushort)(BaseAddr + MemCRA), 0x04);    // CRA: bit2=1 for PRA
        pia.Write(BaseAddr, 0xAA);                       // PRA

        pia.Reset();

        pia.DdrA.Should().Be(0);
        pia.PortA.Should().Be(0);
    }

    [Fact]
    public void Write_DDR_A()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write(BaseAddr, 0xF0);                       // DDRA (CRA bit2=0 by default)
        pia.DdrA.Should().Be(0xF0);
    }

    [Fact]
    public void Write_Port_A()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write(BaseAddr, 0xFF);                       // DDRA = all outputs
        pia.Write((ushort)(BaseAddr + MemCRA), 0x04);    // CRA: bit2=1 → enable PRA access
        pia.Write(BaseAddr, 0x55);                       // PRA at offset 0
        pia.PortA.Should().Be(0x55);
    }

    [Fact]
    public void Write_CRA()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRA), 0x1F);    // CRA at offset 1
        pia.Read((ushort)(BaseAddr + MemCRA)).Should().Be(0x1F);
    }

    [Fact]
    public void Read_DDR_A()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write(BaseAddr, 0xAA);                       // DDRA (CRA bit2=0)
        pia.Read(BaseAddr).Should().Be(0xAA);            // Read DDR A
    }

    [Fact]
    public void Read_Port_A_Respects_DDR()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write(BaseAddr, 0xF0);                       // DDRA
        pia.Write((ushort)(BaseAddr + MemCRA), 0x04);    // CRA: bit2=1 → PRA access
        pia.Write(BaseAddr, 0x30);                       // PRA

        byte result = pia.ReadPortA();
        result.Should().Be(0x3F);
    }

    [Fact]
    public void PortB_WorksSame_AsPortA()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + 2), 0xFF);              // DDRB (CRB bit2=0)
        pia.Write((ushort)(BaseAddr + MemCRB), 0x04);         // CRB: bit2=1 → PRB access
        pia.Write((ushort)(BaseAddr + MemPRB), 0x42);         // PRB at offset 2
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
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ1);

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
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ1 | PiaDevice.CRA_EDGE);

        pia.SetCa1(true);
        pia.SetCa1(false);

        pia.IrqPending.Should().BeTrue();
        irqs.Should().Contain(IrqSource.CA1);
    }

    [Fact]
    public void ClearIrq_ClearsFlag()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ1);
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
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ2);

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
        pia.Write((ushort)(BaseAddr + MemCRB), PiaDevice.CRB_IRQ1);

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
        pia.Write((ushort)(BaseAddr + MemCRB), PiaDevice.CRB_IRQ2);

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
        var mock = new MockTerminal(writes, isB: false);
        pia.AttachTerminal(mock);
        pia.Write(BaseAddr, 0xFF);                       // DDRA = all outputs
        pia.Write((ushort)(BaseAddr + MemCRA), 0x04);   // CRA: bit2=1 → PRA
        pia.Write(BaseAddr, 0x42);                       // PRA at offset 0

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
        pia.Write((ushort)(BaseAddr + 2), 0xFF);         // DDRB = all outputs
        pia.Write((ushort)(BaseAddr + MemCRB), 0x04);   // CRB: bit2=1 → PRB
        pia.Write((ushort)(BaseAddr + MemPRB), 0x99);   // PRB at offset 2

        writes.Should().HaveCount(1);
        writes[0].val.Should().Be(0x99);
    }

    [Fact]
    public void MultipleIRQs_AllTracked()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ1 | PiaDevice.CRA_IRQ2);
        pia.Write((ushort)(BaseAddr + MemCRB), PiaDevice.CRB_IRQ1 | PiaDevice.CRB_IRQ2);

        pia.SetCa1(false); pia.SetCa1(true);
        pia.SetCa2(false); pia.SetCa2(true);
        pia.SetCb1(false); pia.SetCb1(true);
        pia.SetCb2(false); pia.SetCb2(true);

        pia.IrqPending.Should().BeTrue();
    }

    [Fact]
    public void CRA_Read_ReturnsIRQFlagsInBit7()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ1);
        pia.SetCa1(false);
        pia.SetCa1(true);

        byte cra = pia.Read((ushort)(BaseAddr + MemCRA));
        (cra & 0x80).Should().Be(0x80, "CA1 IRQ flag should be visible in CRA bit 7");
    }

    [Fact]
    public void CRA_Read_NoIRQ_ReturnsBit7Clear()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRA), PiaDevice.CRA_IRQ1);

        byte cra = pia.Read((ushort)(BaseAddr + MemCRA));
        (cra & 0x80).Should().Be(0, "CRA bit 7 should be 0 when no IRQ pending");
    }

    [Fact]
    public void CRA_Write_MasksBits6And7()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRA), 0xFF);  // try to set all bits

        byte cra = pia.Read((ushort)(BaseAddr + MemCRA));
        (cra & 0xC0).Should().Be(0, "CRA bits 6-7 are read-only IRQ flags");
        (cra & 0x3F).Should().Be(0x3F, "CRA bits 0-5 should be writable");
    }

    [Fact]
    public void CRB_Write_MasksBits6And7()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + MemCRB), 0xFF);

        byte crb = pia.Read((ushort)(BaseAddr + MemCRB));
        (crb & 0xC0).Should().Be(0, "CRB bits 6-7 are read-only");
        (crb & 0x3F).Should().Be(0x3F, "CRB bits 0-5 should be writable");
    }

    [Fact]
    public void ReadPRA_ClearsCA1IRQ()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write(BaseAddr, 0xFF);                          // DDRA = all outputs
        pia.Write((ushort)(BaseAddr + MemCRA), 0x04);       // CRA: bit2=1 → PRA
        pia.SetCa1(false);
        pia.SetCa1(true);                                   // CA1 IRQ set

        pia.Read(BaseAddr);                                  // Read PRA (offset 0)

        byte cra = pia.Read((ushort)(BaseAddr + MemCRA));
        (cra & 0x80).Should().Be(0, "PRA read should clear CA1 IRQ flag");
    }

    [Fact]
    public void ReadPRB_ClearsCB1IRQ()
    {
        var pia = new PiaDevice(BaseAddr);
        pia.Write((ushort)(BaseAddr + 2), 0xFF);             // DDRB = all outputs
        pia.Write((ushort)(BaseAddr + MemCRB), 0x04);       // CRB: bit2=1 → PRB
        pia.SetCb1(false);
        pia.SetCb1(true);                                   // CB1 IRQ set

        pia.Read((ushort)(BaseAddr + MemPRB));               // Read PRB (offset 2)

        byte crb = pia.Read((ushort)(BaseAddr + MemCRB));
        (crb & 0x80).Should().Be(0, "PRB read should clear CB1 IRQ flag");
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
