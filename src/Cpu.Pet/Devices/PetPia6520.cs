using CpuBase;

namespace Cpu.Pet.Devices;

/// <summary>
/// MOS 6821 PIA at $E810 with PET 4-byte register layout and two port bindings.
/// Port A = keyboard rows, Port B = keyboard columns + IEEE-488 DIO bus.
/// </summary>
public sealed class PetPia6520 : IDevice
{
    private readonly IPortBinding _bindingA;
    private readonly IPortBinding _bindingB;
    private byte _ddra;
    private byte _ddrb;
    private byte _cra;
    private byte _crb;
    private byte _ora;
    private byte _orb;

    public ushort BaseAddress { get; }
    public string Name => "PET-PIA";
    public Action<byte>? OnCrbWrite { get; set; }

    public PetPia6520(ushort baseAddress, IPortBinding bindingA, IPortBinding bindingB)
    {
        BaseAddress = baseAddress;
        _bindingA = bindingA;
        _bindingB = bindingB;
    }

    public bool Accepts(ushort address) =>
        (address & 0xFFF0) == 0xE810 || (address & 0xFFF0) == 0xE820;

    public byte Read(ushort address)
    {
        int offset = (address - BaseAddress) & 3;
        return offset switch
        {
            0 => ReadPortA(),
            1 => ReadControlRegisterA(),
            2 => ReadPortB(),
            3 => ReadControlRegisterB(),
            _ => 0xFF
        };
    }

    public void Write(ushort address, byte value)
    {
        int offset = (address - BaseAddress) & 3;
        switch (offset)
        {
            case 0:
                if ((_cra & 0x04) == 0)
                    _ddra = value;
                else
                {
                    _ora = value;
                    _bindingA.WritePins(value, _ddra);
                }
                break;
            case 1:
                _cra = value;
                break;
            case 2:
                if ((_crb & 0x04) == 0)
                    _ddrb = value;
                else
                {
                    _orb = (byte)(value & _ddrb);
                    _bindingB.WritePins(value, _ddrb);
                }
                break;
            case 3:
                _crb = value;
                OnCrbWrite?.Invoke(value);
                break;
        }
    }

    public void Reset()
    {
        _ddra = 0;
        _ddrb = 0;
        _cra = 0;
        _crb = 0;
        _ora = 0;
        _orb = 0;
    }

    public bool IrqPending =>
        (_cra & 0x80) != 0 || (_cra & 0x10) != 0 || (_crb & 0x80) != 0 || (_crb & 0x10) != 0;

    public void SignalCB1(bool rising)
    {
        bool expectRising = (_crb & 0x02) != 0;
        if (rising == expectRising)
            _crb |= 0x80;
    }

    private byte ReadPortA()
    {
        if ((_cra & 0x04) == 0)
            return _ddra;

        _cra &= 0x6F;
        return (byte)((_ora & _ddra) | (_bindingA.ReadPins() & ~_ddra));
    }

    private byte ReadControlRegisterA()
    {
        byte cra = _cra;
        if (_bindingA.HasInputReady)
            cra |= 0x80;
        else
            cra &= 0x7F;
        return cra;
    }

    private byte ReadPortB()
    {
        if ((_crb & 0x04) == 0)
            return _ddrb;

        _crb &= 0x6F;
        return (byte)((_orb & _ddrb) | (_bindingB.ReadPins() & ~_ddrb));
    }

    private byte ReadControlRegisterB() => _crb;
}
