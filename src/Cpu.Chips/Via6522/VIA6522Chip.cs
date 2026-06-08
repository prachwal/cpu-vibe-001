namespace Cpu.Chips.Via6522;

public class VIA6522Chip 
{
    private byte _orb;
    private byte _ora;
    private byte _ddrb;
    private byte _ddra;
    private ushort _t1c;
    private ushort _t1l;
    private ushort _t2c;
    private ushort _t2l;
    private byte _sr;
    private byte _acr;
    private byte _pcr;
    private byte _ifr;
    private byte _ier;

    private byte _iraLatched;
    private byte _irbLatched;

    private bool _ca1;
    private bool _ca2;
    private bool _cb1;
    private bool _cb2;
    private bool _previousCa1;
    private bool _previousCa2;
    private bool _previousCb1;
    private bool _previousCb2;

    private bool _ca2Output;

    private int _srBitCount;
    private bool _srLoading;

    private bool _t1Pb7State;
    private bool _t1Running;
    private bool _t2Running;

    private bool _irq;

    private int _phi2Cycle;

    public ushort BaseAddress { get; set; } = 0xE800;

    public byte ORB => _orb;
    public byte ORA => _ora;
    public byte DDRB => _ddrb;
    public byte DDRA => _ddra;
    public ushort T1C => _t1c;
    public ushort T1L => _t1l;
    public ushort T2C => _t2c;
    public byte SR => _sr;
    public byte ACR => _acr;
    public byte PCR => _pcr;
    public byte IFR => _ifr;
    public byte IER => _ier;

    public bool CA1 { get => _ca1; set { _previousCa1 = _ca1; _ca1 = value; } }
    public bool CA2 { get => _ca2; set { _previousCa2 = _ca2; _ca2 = value; } }
    public bool CB1 { get => _cb1; set { _previousCb1 = _cb1; _cb1 = value; } }
    public bool CB2 { get => _cb2; set { _previousCb2 = _cb2; _cb2 = value; } }
    public bool CA2Output => _ca2Output;

    public bool IRQ => _irq;
    public bool HasInterrupt => _irq;

    public byte PortAExternalInput { get; set; }
    public byte PortBExternalInput { get; set; }

    public Action<byte>? OnPortBWrite { get; set; }

    public VIA6522Chip() { Reset(); }
    public VIA6522Chip(ushort baseAddress) { BaseAddress = baseAddress; Reset(); }

    public void Reset()
    {
        _orb = 0; _ora = 0; _ddrb = 0; _ddra = 0;
        _t1c = 0; _t1l = 0; _t2c = 0; _t2l = 0;
        _sr = 0; _acr = 0; _pcr = 0; _ifr = 0; _ier = 0;
        _iraLatched = 0; _irbLatched = 0;
        _ca1 = false; _ca2 = false; _cb1 = false; _cb2 = false;
        _previousCa1 = false; _previousCa2 = false;
        _previousCb1 = false; _previousCb2 = false;
        _ca2Output = false;
        _srBitCount = 0; _srLoading = false;
        _t1Pb7State = false;
        _t1Running = false; _t2Running = false;
        _irq = false;
        _phi2Cycle = 0;
        PortAExternalInput = 0;
        PortBExternalInput = 0;
    }

    public byte ReadByte(ushort address)
    {
        int offset = address - BaseAddress;
        if (offset < 0 || offset >= VIA6522Constants.REGISTER_COUNT)
            return 0xFF;

        switch (offset)
        {
            case VIA6522Constants.ORB_OFFSET:
                return ReadPortB();

            case VIA6522Constants.ORA_OFFSET:
                return ReadPortA();

            case VIA6522Constants.DDRB_OFFSET:
                return _ddrb;

            case VIA6522Constants.DDRA_OFFSET:
                return _ddra;

            case VIA6522Constants.T1CL_OFFSET:
                ClearIFRBit(VIA6522Constants.IFR_TIMER1_BIT);
                return (byte)(_t1c & 0xFF);

            case VIA6522Constants.T1CH_OFFSET:
                return (byte)((_t1c >> 8) & 0xFF);

            case VIA6522Constants.T1LL_OFFSET:
                return (byte)(_t1l & 0xFF);

            case VIA6522Constants.T1LH_OFFSET:
                return (byte)((_t1l >> 8) & 0xFF);

            case VIA6522Constants.T2CL_OFFSET:
                ClearIFRBit(VIA6522Constants.IFR_TIMER2_BIT);
                return (byte)(_t2c & 0xFF);

            case VIA6522Constants.T2CH_OFFSET:
                return (byte)((_t2c >> 8) & 0xFF);

            case VIA6522Constants.SR_OFFSET:
                ClearIFRBit(VIA6522Constants.IFR_SR_BIT);
                return _sr;

            case VIA6522Constants.ACR_OFFSET:
                return _acr;

            case VIA6522Constants.PCR_OFFSET:
                return _pcr;

            case VIA6522Constants.IFR_OFFSET:
                return _ifr;

            case VIA6522Constants.IER_OFFSET:
                return (byte)(_ier | VIA6522Constants.IER_SET_CLEAR_MASK);

            case VIA6522Constants.ORA_NOHANDSHAKE_OFFSET:
                return ReadPortA();

            default: return 0xFF;
        }
    }

    public void WriteByte(ushort address, byte value)
    {
        int offset = address - BaseAddress;
        if (offset < 0 || offset >= VIA6522Constants.REGISTER_COUNT)
            return;

        switch (offset)
        {
            case VIA6522Constants.ORB_OFFSET:
                _orb = value;
                OnPortBWrite?.Invoke(value);
                break;

            case VIA6522Constants.ORA_OFFSET:
                _ora = value;
                UpdateCA2Handshake();
                break;

            case VIA6522Constants.DDRB_OFFSET:
                _ddrb = value;
                OnPortBWrite?.Invoke(_orb);
                break;

            case VIA6522Constants.DDRA_OFFSET:
                _ddra = value;
                break;

            case VIA6522Constants.T1CL_OFFSET:
                _t1l = (ushort)((_t1l & 0xFF00) | value);
                break;

            case VIA6522Constants.T1CH_OFFSET:
                _t1l = (ushort)((_t1l & 0x00FF) | ((ushort)value << 8));
                _t1c = _t1l;
                _t1Running = true;
                _t1Pb7State = false;
                ClearIFRBit(VIA6522Constants.IFR_TIMER1_BIT);
                if ((_pcr & 0x0E) == VIA6522Constants.CA2_PULSE)
                    _ca2Output = false;
                break;

            case VIA6522Constants.T1LL_OFFSET:
                _t1l = (ushort)((_t1l & 0xFF00) | value);
                break;

            case VIA6522Constants.T1LH_OFFSET:
                _t1l = (ushort)((_t1l & 0x00FF) | ((ushort)value << 8));
                _t1c = _t1l;
                break;

            case VIA6522Constants.T2CL_OFFSET:
                _t2l = (ushort)((_t2l & 0xFF00) | value);
                break;

            case VIA6522Constants.T2CH_OFFSET:
                _t2l = (ushort)((_t2l & 0x00FF) | ((ushort)value << 8));
                _t2c = _t2l;
                _t2Running = true;
                ClearIFRBit(VIA6522Constants.IFR_TIMER2_BIT);
                break;

            case VIA6522Constants.SR_OFFSET:
                _sr = value;
                ClearIFRBit(VIA6522Constants.IFR_SR_BIT);
                byte srMode = (byte)(_acr & VIA6522Constants.ACR_SR_MODE_MASK);
                if (srMode >= VIA6522Constants.SR_OUT_FREE)
                {
                    _srLoading = true;
                    _srBitCount = 0;
                }
                break;

            case VIA6522Constants.ACR_OFFSET:
                _acr = value;
                break;

            case VIA6522Constants.PCR_OFFSET:
                _pcr = value;
                UpdateCA2Output();
                UpdateCB2Output();
                break;

            case VIA6522Constants.IFR_OFFSET:
                _ifr &= (byte)~value;
                UpdateIRQ();
                break;

            case VIA6522Constants.IER_OFFSET:
                if ((value & VIA6522Constants.IER_SET_CLEAR_MASK) != 0)
                    _ier |= (byte)(value & ~VIA6522Constants.IER_SET_CLEAR_MASK);
                else
                    _ier &= (byte)~value;
                UpdateIRQ();
                break;

            case VIA6522Constants.ORA_NOHANDSHAKE_OFFSET:
                _ora = value;
                break;
        }
    }

    public void Update()
    {
        _phi2Cycle++;

        CheckEdgeTriggers();

        _previousCa1 = _ca1;
        _previousCa2 = _ca2;
        _previousCb1 = _cb1;
        _previousCb2 = _cb2;

        UpdateTimers();

        UpdateShiftRegister();

        UpdateIRQ();
    }

    private byte ReadPortA()
    {
        byte result = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((_ddra & (1 << i)) != 0)
            {
                if ((_ora & (1 << i)) != 0)
                    result |= (byte)(1 << i);
            }
            else
            {
                byte input = (_acr & VIA6522Constants.ACR_PA_LATCH_MASK) != 0 ? _iraLatched : PortAExternalInput;
                if ((input & (1 << i)) != 0)
                    result |= (byte)(1 << i);
            }
        }
        ClearIFRBit(VIA6522Constants.IFR_CA1_BIT);
        ClearIFRBit(VIA6522Constants.IFR_CA2_BIT);
        UpdateCA2Handshake();
        return result;
    }

    private byte ReadPortB()
    {
        byte result = 0;
        bool pb7Override = (_acr & VIA6522Constants.ACR_T1_OUTPUT_MASK) != 0;

        for (int i = 0; i < 7; i++)
        {
            if ((_ddrb & (1 << i)) != 0)
            {
                if ((_orb & (1 << i)) != 0)
                    result |= (byte)(1 << i);
            }
            else
            {
                byte input = (_acr & VIA6522Constants.ACR_PB_LATCH_MASK) != 0 ? _irbLatched : PortBExternalInput;
                if ((input & (1 << i)) != 0)
                    result |= (byte)(1 << i);
            }
        }

        if (pb7Override)
        {
            if (_t1Pb7State)
                result |= 0x80;
        }
        else
        {
            if ((_ddrb & 0x80) != 0)
            {
                if ((_orb & 0x80) != 0)
                    result |= 0x80;
            }
            else
            {
                byte input = (_acr & VIA6522Constants.ACR_PB_LATCH_MASK) != 0 ? _irbLatched : PortBExternalInput;
                if ((input & 0x80) != 0)
                    result |= 0x80;
            }
        }

        ClearIFRBit(VIA6522Constants.IFR_CB1_BIT);
        ClearIFRBit(VIA6522Constants.IFR_CB2_BIT);

        return result;
    }

    private void CheckEdgeTriggers()
    {
        bool ca1Rising = (_pcr & VIA6522Constants.PCR_CA1_EDGE_MASK) != 0;

        if (_ca1 && !_previousCa1)
        {
            if (ca1Rising)
            {
                SetIFRBit(VIA6522Constants.IFR_CA1_BIT);
                LatchPortA();
            }
        }
        else if (!_ca1 && _previousCa1)
        {
            if (!ca1Rising)
            {
                SetIFRBit(VIA6522Constants.IFR_CA1_BIT);
                LatchPortA();
            }
        }

        byte ca2Mode = (byte)(_pcr & VIA6522Constants.PCR_CA2_CONTROL_MASK);
        if (ca2Mode == VIA6522Constants.CA2_INPUT_NEG || ca2Mode == VIA6522Constants.CA2_IND_INT_NEG)
        {
            if (!_ca2 && _previousCa2)
                SetIFRBit(VIA6522Constants.IFR_CA2_BIT);
        }
        else if (ca2Mode == VIA6522Constants.CA2_INPUT_POS || ca2Mode == VIA6522Constants.CA2_IND_INT_POS)
        {
            if (_ca2 && !_previousCa2)
                SetIFRBit(VIA6522Constants.IFR_CA2_BIT);
        }

        bool cb1Rising = (_pcr & VIA6522Constants.PCR_CB1_EDGE_MASK) != 0;

        if (_cb1 && !_previousCb1)
        {
            if (cb1Rising)
            {
                SetIFRBit(VIA6522Constants.IFR_CB1_BIT);
                LatchPortB();
                ClockSRExternal();
            }
        }
        else if (!_cb1 && _previousCb1)
        {
            if (!cb1Rising)
            {
                SetIFRBit(VIA6522Constants.IFR_CB1_BIT);
                LatchPortB();
                ClockSRExternal();
            }
        }

        byte cb2Mode = (byte)(_pcr & VIA6522Constants.PCR_CB2_CONTROL_MASK);
        if (cb2Mode == VIA6522Constants.CB2_INPUT_NEG || cb2Mode == VIA6522Constants.CB2_IND_INT_NEG)
        {
            if (!_cb2 && _previousCb2)
                SetIFRBit(VIA6522Constants.IFR_CB2_BIT);
        }
        else if (cb2Mode == VIA6522Constants.CB2_INPUT_POS || cb2Mode == VIA6522Constants.CB2_IND_INT_POS)
        {
            if (_cb2 && !_previousCb2)
                SetIFRBit(VIA6522Constants.IFR_CB2_BIT);
        }
    }

    private void LatchPortA()
    {
        if ((_acr & VIA6522Constants.ACR_PA_LATCH_MASK) != 0)
        {
            _iraLatched = PortAExternalInput;
        }
    }

    private void LatchPortB()
    {
        if ((_acr & VIA6522Constants.ACR_PB_LATCH_MASK) != 0)
        {
            _irbLatched = PortBExternalInput;
        }
    }

    private void UpdateTimers()
    {
        if (_t1Running)
        {
            _t1c--;
            if (_t1c == 0xFFFF)
            {
                SetIFRBit(VIA6522Constants.IFR_TIMER1_BIT);
                _t1Pb7State = !_t1Pb7State;

                bool t1FreeRun = (_acr & VIA6522Constants.ACR_T1_CONTROL_MASK) != 0;
                if (t1FreeRun)
                    _t1c = _t1l;
                else
                    _t1Running = false;

                if ((_pcr & VIA6522Constants.PCR_CA2_CONTROL_MASK) == VIA6522Constants.CA2_PULSE)
                    _ca2Output = true;
            }
        }

        bool t2PulseCount = (_acr & VIA6522Constants.ACR_T2_CONTROL_MASK) != 0;
        if (_t2Running && !t2PulseCount)
        {
            _t2c--;
            if (_t2c == 0xFFFF)
            {
                SetIFRBit(VIA6522Constants.IFR_TIMER2_BIT);
                _t2Running = false;
            }
        }
    }

    public void ClockT2External()
    {
        if (!_t2Running) return;
        bool t2PulseCount = (_acr & VIA6522Constants.ACR_T2_CONTROL_MASK) != 0;
        if (t2PulseCount)
        {
            _t2c--;
            if (_t2c == 0xFFFF)
            {
                SetIFRBit(VIA6522Constants.IFR_TIMER2_BIT);
                _t2Running = false;
                UpdateIRQ();
            }
        }
    }

    private void UpdateShiftRegister()
    {
        byte srMode = (byte)(_acr & VIA6522Constants.ACR_SR_MODE_MASK);
        if (srMode == VIA6522Constants.SR_DISABLED) return;

        if (_srLoading)
        {
            _srLoading = false;
            _srBitCount = 0;
        }

        bool clockThisCycle = false;

        if (srMode == VIA6522Constants.SR_IN_PHI2 || srMode == VIA6522Constants.SR_OUT_PHI2)
        {
            clockThisCycle = (_phi2Cycle & 1) == 0;
        }
        else if (srMode == VIA6522Constants.SR_OUT_FREE)
        {
            clockThisCycle = (_phi2Cycle & 1) == 0;
        }

        if (clockThisCycle)
        {
            ClockSR();
        }
    }

    private void ClockSR()
    {
        byte srMode = (byte)(_acr & VIA6522Constants.ACR_SR_MODE_MASK);

        if (srMode == VIA6522Constants.SR_IN_PHI2 || srMode == VIA6522Constants.SR_IN_CB1)
        {
            _sr = (byte)((_sr >> 1) | (CB2Value() ? 0x80 : 0));
        }
        else
        {
            _sr = (byte)((_sr << 1) | 0);
        }

        _srBitCount++;
        if (_srBitCount >= 8)
        {
            SetIFRBit(VIA6522Constants.IFR_SR_BIT);
            _srBitCount = 0;

            if (srMode == VIA6522Constants.SR_OUT_FREE)
            {
                _srLoading = true;
            }
        }
    }

    private void ClockSRExternal()
    {
        byte srMode = (byte)(_acr & VIA6522Constants.ACR_SR_MODE_MASK);
        bool externalClock = srMode == VIA6522Constants.SR_IN_CB1 || srMode == VIA6522Constants.SR_OUT_CB1;
        if (!externalClock) return;

        ClockSR();
    }

    private bool CB2Value()
    {
        return _cb2;
    }

    private void UpdateCA2Output()
    {
        byte ca2Mode = (byte)(_pcr & VIA6522Constants.PCR_CA2_CONTROL_MASK);
        switch (ca2Mode)
        {
            case VIA6522Constants.CA2_LOW:
                _ca2Output = false; break;
            case VIA6522Constants.CA2_HIGH:
                _ca2Output = true; break;
            case VIA6522Constants.CA2_HANDSHAKE:
            case VIA6522Constants.CA2_PULSE:
                _ca2Output = true; break;
        }
    }

    private void UpdateCA2Handshake()
    {
        byte ca2Mode = (byte)(_pcr & VIA6522Constants.PCR_CA2_CONTROL_MASK);
        if (ca2Mode == VIA6522Constants.CA2_HANDSHAKE || ca2Mode == VIA6522Constants.CA2_PULSE)
        {
            _ca2Output = false;
        }
    }

    private void UpdateCB2Output()
    {
        byte cb2Mode = (byte)(_pcr & VIA6522Constants.PCR_CB2_CONTROL_MASK);
        switch (cb2Mode)
        {
            case VIA6522Constants.CB2_LOW:
                _cb2 = false; break;
            case VIA6522Constants.CB2_HIGH:
                _cb2 = true; break;
            case VIA6522Constants.CB2_HANDSHAKE:
            case VIA6522Constants.CB2_PULSE:
                _cb2 = true; break;
        }
    }

    private void SetIFRBit(int bit)
    {
        _ifr |= (byte)(1 << bit);
    }

    private void ClearIFRBit(int bit)
    {
        _ifr &= (byte)~(1 << bit);
    }

    private void UpdateIRQ()
    {
        bool anyEnabled = false;
        for (int i = 0; i < 7; i++)
        {
            if ((_ifr & (1 << i)) != 0 && (_ier & (1 << i)) != 0)
            {
                anyEnabled = true;
                break;
            }
        }

        if (anyEnabled)
        {
            _ifr |= VIA6522Constants.IFR_IRQ_MASK;
            _irq = true;
        }
        else
        {
            unchecked { _ifr &= (byte)~VIA6522Constants.IFR_IRQ_MASK; }
            _irq = false;
        }
    }

    public bool AcknowledgeInterrupt()
    {
        if (_irq)
        {
            _irq = false;
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        return $"VIA6522 @ 0x{BaseAddress:X4}\n" +
               $"  ORB=0x{_orb:X2}, ORA=0x{_ora:X2}, DDRB=0x{_ddrb:X2}, DDRA=0x{_ddra:X2}\n" +
               $"  T1C=0x{_t1c:X4}, T1L=0x{_t1l:X4}, T2C=0x{_t2c:X4}\n" +
               $"  SR=0x{_sr:X2}, ACR=0x{_acr:X2}, PCR=0x{_pcr:X2}\n" +
               $"  IFR=0x{_ifr:X2}, IER=0x{_ier:X2}, IRQ={_irq}";
    }
}
