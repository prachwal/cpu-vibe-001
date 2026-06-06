using CpuBase;

namespace Cpu.Tui.Devices.Pia;

/// <summary>
/// PIA 6520 Peripheral Interface Adapter.
/// Provides two 8-bit ports (A/B) with data direction registers,
/// control lines (CA1/CA2/CB1/CB2), and edge-triggered interrupts.
/// 
/// Register map (6 consecutive addresses):
///   +0: DDR A  (Data Direction Register A)
///   +1: PRA   (Port Register A - data)
///   +2: CRA   (Control Register A)
///   +3: DDR B  (Data Direction Register B)
///   +4: PRB   (Port Register B - data)
///   +5: CRB   (Control Register B)
/// </summary>
public class PiaDevice : IDevice
{
    private readonly byte[] _regs = new byte[6];

    private readonly bool[] _ca1 = new bool[2];
    private readonly bool[] _ca2 = new bool[2];
    private readonly bool[] _cb1 = new bool[2];
    private readonly bool[] _cb2 = new bool[2];
    private readonly bool[] _irqA = new bool[2];
    private readonly bool[] _irqB = new bool[2];

    private readonly ushort _baseAddress;
    private IPiaTerminal? _terminal;

    public const string DeviceName = "PIA";

    public const ushort DDRA = 0;
    public const ushort PRA = 1;
    public const ushort CRA = 2;
    public const ushort DDRB = 3;
    public const ushort PRB = 4;
    public const ushort CRB = 5;

    public const byte CRA_IRQ1 = 0x01;
    public const byte CRA_IRQ2 = 0x02;
    public const byte CRA_DDR = 0x04;
    public const byte CRA_SET = 0x08;
    public const byte CRA_EDGE = 0x10;
    public const byte CRB_IRQ1 = 0x01;
    public const byte CRB_IRQ2 = 0x02;
    public const byte CRB_SET = 0x08;
    public const byte CRB_EDGE = 0x10;

    public string Name => DeviceName;
    public ushort BaseAddress => _baseAddress;

    public PiaDevice(ushort baseAddress)
    {
        _baseAddress = baseAddress;
    }

    public bool Accepts(ushort address)
    {
        return address >= _baseAddress && address <= _baseAddress + 5;
    }

    public byte Read(ushort address)
    {
        ushort offset = (ushort)(address - _baseAddress);
        if (offset > 5) return 0xFF;
        return _regs[offset];
    }

    public void Write(ushort address, byte value)
    {
        ushort offset = (ushort)(address - _baseAddress);
        if (offset > 5) return;

        _regs[offset] = value;

        if (offset == PRA)
            _terminal?.OnPortAWrite(value, (byte)0);
        else if (offset == PRB)
            _terminal?.OnPortBWrite(value, (byte)0);
    }

    public void Load(ushort address, byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            Write((ushort)(address + i), data[i]);
    }

    public void Reset()
    {
        Array.Clear(_regs);
        Array.Clear(_ca1);
        Array.Clear(_ca2);
        Array.Clear(_cb1);
        Array.Clear(_cb2);
        Array.Clear(_irqA);
        Array.Clear(_irqB);
    }

    public void AttachTerminal(IPiaTerminal terminal)
    {
        _terminal = terminal;
    }

    public void SetCa1(bool high)
    {
        bool oldHigh = _ca1[0];
        _ca1[0] = high;
        bool matchEdge = (_regs[CRA] & CRA_EDGE) == 0
            ? (!oldHigh && high)
            : (oldHigh && !high);
        if (matchEdge)
        {
            _irqA[0] = true;
            if ((_regs[CRA] & CRA_IRQ1) != 0)
                _terminal?.OnIrq(IrqSource.CA1);
        }
    }

    public void SetCa2(bool high)
    {
        bool oldHigh = _ca2[0];
        _ca2[0] = high;
        bool matchEdge = (_regs[CRA] & CRA_EDGE) == 0
            ? (!oldHigh && high)
            : (oldHigh && !high);
        if (matchEdge)
        {
            _irqA[1] = true;
            if ((_regs[CRA] & CRA_IRQ2) != 0)
                _terminal?.OnIrq(IrqSource.CA2);
        }
    }

    public void SetCb1(bool high)
    {
        bool oldHigh = _cb1[0];
        _cb1[0] = high;
        bool matchEdge = (_regs[CRB] & CRB_EDGE) == 0
            ? (!oldHigh && high)
            : (oldHigh && !high);
        if (matchEdge)
        {
            _irqB[0] = true;
            if ((_regs[CRB] & CRB_IRQ1) != 0)
                _terminal?.OnIrq(IrqSource.CB1);
        }
    }

    public void SetCb2(bool high)
    {
        bool oldHigh = _cb2[0];
        _cb2[0] = high;
        bool matchEdge = (_regs[CRB] & CRB_EDGE) == 0
            ? (!oldHigh && high)
            : (oldHigh && !high);
        if (matchEdge)
        {
            _irqB[1] = true;
            if ((_regs[CRB] & CRB_IRQ2) != 0)
                _terminal?.OnIrq(IrqSource.CB2);
        }
    }

    public byte ReadPortA()
    {
        byte ddr = _regs[DDRA];
        return (byte)((_regs[PRA] & ddr) | (0xFF & ~ddr));
    }

    public byte ReadPortB()
    {
        byte ddr = _regs[DDRB];
        return (byte)((_regs[PRB] & ddr) | (0xFF & ~ddr));
    }

    public void WritePortA(byte value)
    {
        byte old = _regs[PRA];
        _regs[PRA] = (byte)(value & _regs[DDRA]);
        _terminal?.OnPortAWrite(_regs[PRA], old);
    }

    public void WritePortB(byte value)
    {
        byte old = _regs[PRB];
        _regs[PRB] = (byte)(value & _regs[DDRB]);
        _terminal?.OnPortBWrite(_regs[PRB], old);
    }

    public void ClearIrq(IrqSource source)
    {
        switch (source)
        {
            case IrqSource.CA1: _irqA[0] = false; break;
            case IrqSource.CA2: _irqA[1] = false; break;
            case IrqSource.CB1: _irqB[0] = false; break;
            case IrqSource.CB2: _irqB[1] = false; break;
        }
    }

    public bool IrqPending => _irqA[0] || _irqA[1] || _irqB[0] || _irqB[1];

    public byte PortA => _regs[PRA];
    public byte PortB => _regs[PRB];
    public byte DdrA => _regs[DDRA];
    public byte DdrB => _regs[DDRB];
}

public enum IrqSource
{
    CA1,
    CA2,
    CB1,
    CB2
}
