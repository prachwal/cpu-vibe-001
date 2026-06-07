namespace Cpu.Chips.Crtc6545;

public class CRTC6545Chip 
{
    private byte _addressRegister;
    private readonly byte[] _regs;

    private ushort _hCounter;
    private ushort _vCounter;
    private ushort _adjCounter;
    private ushort _raCounter;

    private ushort _maCounter;
    private ushort _maStart;
    private ushort _maLineStart;

    private ushort _lpHigh;
    private ushort _lpLow;

    private bool _lpenRegistered;

    private byte _deSkew;
    private byte _ceSkew;

    private int _blinkCounter;
    private bool _blinkState;

    public ushort BaseAddress { get; set; } = 0xE880;

    public ushort MACounter => _maCounter;
    public ushort RACounter => _raCounter;
    public bool HSync { get; private set; }
    public bool VSync { get; private set; }
    public bool DisplayEnable { get; private set; }
    public bool CursorEnable { get; private set; }
    public bool VerticalBlanking { get; private set; }
    public bool InAdjustLines { get; private set; }

    public byte R0 { get => _regs[0]; set => _regs[0] = value; }
    public byte R1 { get => _regs[1]; set => _regs[1] = value; }
    public byte R2 { get => _regs[2]; set => _regs[2] = value; }
    public byte R3 { get => _regs[3]; set => _regs[3] = value; }
    public byte R4 => (byte)(_regs[4] & 0x7F);
    public byte R5 => (byte)(_regs[5] & 0x1F);
    public byte R6 => (byte)(_regs[6] & 0x7F);
    public byte R7 => (byte)(_regs[7] & 0x7F);
    public byte R8 { get => _regs[8]; set => _regs[8] = value; }
    public byte R9 => (byte)(_regs[9] & 0x1F);
    public byte R10 { get => _regs[10]; set => _regs[10] = value; }
    public byte R11 => (byte)(_regs[11] & 0x1F);
    public ushort R12 => (ushort)(_regs[12] & 0x3F);
    public byte R13 { get => _regs[13]; set => _regs[13] = value; }
    public ushort R14 => (ushort)(_regs[14] & 0x3F);
    public byte R15 { get => _regs[15]; set => _regs[15] = value; }

    public byte R16 => (byte)(_lpHigh & 0x3F);
    public byte R17 => (byte)(_lpLow & 0xFF);

    public byte DisplayStartHigh { get => (byte)(_regs[12] & 0x3F); set => _regs[12] = (byte)(value & 0x3F); }
    public byte DisplayStartLow { get => _regs[13]; set => _regs[13] = value; }
    public ushort CursorAddress => (ushort)(((_regs[14] & 0x3F) << 8) | _regs[15]);

    public bool HasInterrupt => false;
    public bool AcknowledgeInterrupt() => false;

    public CRTC6545Chip()
    {
        _regs = new byte[CRTC6545Constants.INTERNAL_REGISTER_COUNT];
        Reset();
    }

    public CRTC6545Chip(ushort baseAddress) : this()
    {
        BaseAddress = baseAddress;
    }

    public void Reset()
    {
        _addressRegister = 0;
        Array.Clear(_regs, 0, _regs.Length);

        _hCounter = 0;
        _vCounter = 0;
        _adjCounter = 0;
        _raCounter = 0;

        _maCounter = 0;
        _maStart = 0;
        _maLineStart = 0;

        _lpHigh = 0;
        _lpLow = 0;
        _lpenRegistered = false;

        _deSkew = 0;
        _ceSkew = 0;

        _blinkCounter = 0;
        _blinkState = true;

        HSync = false;
        VSync = false;
        DisplayEnable = false;
        CursorEnable = false;
        VerticalBlanking = false;
        InAdjustLines = false;
    }

    public byte ReadByte(ushort address)
    {
        int offset = address - BaseAddress;
        if (offset < 0 || offset >= CRTC6545Constants.REGISTER_COUNT)
            return 0xFF;

        if (offset == CRTC6545Constants.ADDRESS_REG_OFFSET)
        {
            return ReadStatus();
        }
        else
        {
            return ReadRegister(_addressRegister);
        }
    }

    public void WriteByte(ushort address, byte value)
    {
        int offset = address - BaseAddress;
        if (offset < 0 || offset >= CRTC6545Constants.REGISTER_COUNT)
            return;

        if (offset == CRTC6545Constants.ADDRESS_REG_OFFSET)
        {
            _addressRegister = (byte)(value & 0x1F);
        }
        else
        {
            WriteRegister(_addressRegister, value);
        }
    }

    private byte ReadStatus()
    {
        byte status = 0;
        if (VerticalBlanking)
            status |= CRTC6545Constants.STATUS_VRT;
        if (_lpenRegistered)
            status |= CRTC6545Constants.STATUS_LRF;
        return status;
    }

    private byte ReadRegister(int reg)
    {
        switch (reg)
        {
            case CRTC6545Constants.R12_DISPLAY_START_H:
                return (byte)(_regs[12] & 0x3F);
            case CRTC6545Constants.R13_DISPLAY_START_L:
                return _regs[13];
            case CRTC6545Constants.R14_CURSOR_H:
                return (byte)(_regs[14] & 0x3F);
            case CRTC6545Constants.R15_CURSOR_L:
                return _regs[15];
            case CRTC6545Constants.R16_LIGHTPEN_H:
                _lpenRegistered = false;
                return (byte)(_lpHigh & 0x3F);
            case CRTC6545Constants.R17_LIGHTPEN_L:
                _lpenRegistered = false;
                return (byte)(_lpLow & 0xFF);
            default:
                return 0;
        }
    }

    private void WriteRegister(int reg, byte value)
    {
        switch (reg)
        {
            case CRTC6545Constants.R0_HORIZONTAL_TOTAL:
                _regs[0] = value; break;
            case CRTC6545Constants.R1_HORIZONTAL_DISPLAY:
                _regs[1] = value; break;
            case CRTC6545Constants.R2_HORIZONTAL_SYNC_POS:
                _regs[2] = value; break;
            case CRTC6545Constants.R3_SYNC_WIDTHS:
                _regs[3] = value; break;
            case CRTC6545Constants.R4_VERTICAL_TOTAL:
                _regs[4] = (byte)(value & 0x7F); break;
            case CRTC6545Constants.R5_VERTICAL_ADJUST:
                _regs[5] = (byte)(value & 0x1F); break;
            case CRTC6545Constants.R6_VERTICAL_DISPLAY:
                _regs[6] = (byte)(value & 0x7F); break;
            case CRTC6545Constants.R7_VERTICAL_SYNC_POS:
                _regs[7] = (byte)(value & 0x7F); break;
            case CRTC6545Constants.R8_MODE_CONTROL:
                _regs[8] = value; break;
            case CRTC6545Constants.R9_SCAN_LINES:
                _regs[9] = (byte)(value & 0x1F); break;
            case CRTC6545Constants.R10_CURSOR_START:
                _regs[10] = value; break;
            case CRTC6545Constants.R11_CURSOR_END:
                _regs[11] = (byte)(value & 0x1F); break;
            case CRTC6545Constants.R12_DISPLAY_START_H:
                _regs[12] = (byte)(value & 0x3F); break;
            case CRTC6545Constants.R13_DISPLAY_START_L:
                _regs[13] = value; break;
            case CRTC6545Constants.R14_CURSOR_H:
                _regs[14] = (byte)(value & 0x3F); break;
            case CRTC6545Constants.R15_CURSOR_L:
                _regs[15] = value; break;
        }
    }

    public void Update()
    {
        int hTotal = _regs[CRTC6545Constants.R0_HORIZONTAL_TOTAL] + 1;
        int hDisplay = _regs[CRTC6545Constants.R1_HORIZONTAL_DISPLAY];
        int hSyncPos = _regs[CRTC6545Constants.R2_HORIZONTAL_SYNC_POS];
        int hSyncWidth = (_regs[CRTC6545Constants.R3_SYNC_WIDTHS] & 0x0F);
        if (hSyncWidth == 0) hSyncWidth = 16;

        int vTotal = R4 + 1;
        int vAdjust = R5;
        int vDisplay = R6;
        int vSyncPos = R7;
        int vSyncWidth = (_regs[CRTC6545Constants.R3_SYNC_WIDTHS] >> 4) & 0x0F;
        if (vSyncWidth == 0) vSyncWidth = 16;

        int scanLines = R9 + 1;

        byte cursorMode = (byte)(_regs[CRTC6545Constants.R10_CURSOR_START] & CRTC6545Constants.R10_CURSOR_MODE_MASK);
        int cursorStart = _regs[CRTC6545Constants.R10_CURSOR_START] & CRTC6545Constants.R10_CURSOR_START_MASK;
        int cursorEnd = R11;

        bool hSyncActive = HSync;
        bool vSyncActive = VSync;

        int deBefore = DisplayEnable ? 1 : 0;
        int ceBefore = CursorEnable ? 1 : 0;

        bool deNow = false;
        bool ceNow = false;
        bool hSyncNow = false;
        bool vSyncNow = false;

        bool hBlank = _hCounter >= hDisplay;
        bool vBlank = _vCounter >= vDisplay && !InAdjustLines;
        VerticalBlanking = vBlank || InAdjustLines;

        if (!hBlank && !vBlank)
            deNow = true;

        if (_hCounter >= hSyncPos && _hCounter < hSyncPos + hSyncWidth)
            hSyncNow = true;

        if (vSyncActive || (_vCounter >= vSyncPos && _vCounter < vSyncPos + vSyncWidth))
        {
            vSyncNow = true;
            if (!InAdjustLines)
                VerticalBlanking = true;
        }

        if (!vBlank && !InAdjustLines)
        {
            if (_hCounter < hDisplay)
            {
                if (_hCounter == 0)
                    _maCounter = _maLineStart;
                else
                    _maCounter++;
            }
        }

        ushort cursorAddr = CursorAddress;
        bool cursorMatch = _maCounter == cursorAddr;

        bool cursorLine = _raCounter >= cursorStart && _raCounter <= cursorEnd && _blinkState;
        ceNow = cursorMatch && cursorLine &&
                cursorMode != CRTC6545Constants.CURSOR_MODE_DISABLED && !vBlank;

        _deSkew = (byte)((_deSkew << 1) | (deNow ? 1 : 0));
        _ceSkew = (byte)((_ceSkew << 1) | (ceNow ? 1 : 0));

        int deDelay = (_regs[CRTC6545Constants.R8_MODE_CONTROL] & CRTC6545Constants.R8_DE_SKEW_MASK) != 0 ? 1 : 0;
        int ceDelay = (_regs[CRTC6545Constants.R8_MODE_CONTROL] & CRTC6545Constants.R8_CE_SKEW_MASK) != 0 ? 1 : 0;

        DisplayEnable = ((_deSkew >> deDelay) & 1) != 0;
        CursorEnable = ((_ceSkew >> ceDelay) & 1) != 0;
        HSync = hSyncNow;
        VSync = vSyncNow;

        _hCounter++;
        if (_hCounter >= hTotal)
        {
            _hCounter = 0;

            _raCounter++;
            if (_raCounter >= scanLines)
            {
                _raCounter = 0;

                if (!InAdjustLines && _vCounter < vDisplay)
                {
                    _maLineStart += (ushort)hDisplay;
                }

                _vCounter++;

                if (!InAdjustLines && _vCounter == vTotal)
                {
                    if (vAdjust > 0)
                    {
                        InAdjustLines = true;
                        _adjCounter = 0;
                    }
                    else
                    {
                        _maCounter = _maStart;
                        _maLineStart = _maStart;
                        _vCounter = 0;
                        FrameComplete();
                    }
                }
                else if (InAdjustLines)
                {
                    _adjCounter++;
                    if (_adjCounter >= vAdjust)
                    {
                        InAdjustLines = false;
                        _maCounter = _maStart;
                        _maLineStart = _maStart;
                        _vCounter = 0;
                        FrameComplete();
                    }
                }
            }
        }
    }

    private void FrameComplete()
    {
        _blinkCounter++;
        int blinkRate = ((_regs[CRTC6545Constants.R10_CURSOR_START] & CRTC6545Constants.R10_CURSOR_MODE_MASK) == CRTC6545Constants.CURSOR_MODE_BLINK_1_32) ? 32 : 16;
        if (_blinkCounter >= blinkRate)
        {
            _blinkCounter = 0;
            _blinkState = !_blinkState;
        }
    }

    public void LightpenStrobe()
    {
        _lpHigh = (ushort)((_maCounter >> 8) & 0x3F);
        _lpLow = (byte)(_maCounter & 0xFF);
        _lpenRegistered = true;
    }

    public byte GetRegister(int reg)
    {
        return _regs[reg];
    }

    public void SetRegister(int reg, byte value)
    {
        WriteRegister(reg, value);
    }

    /// <summary>
    /// Set the display start address from R12/R13.
    /// </summary>
    public void UpdateDisplayStart()
    {
        _maStart = (ushort)(((_regs[12] & 0x3F) << 8) | _regs[13]);
        _maCounter = _maStart;
        _maLineStart = _maStart;
    }

    public override string ToString()
    {
        return $"CRTC6545 @ 0x{BaseAddress:X4}\n" +
               $"  H={_hCounter} V={_vCounter} RA={_raCounter} MA=0x{_maCounter:X4}\n" +
               $"  DE={DisplayEnable} HS={HSync} VS={VSync} CE={CursorEnable}\n" +
               $"  R0={R0} R1={R1} R2={R2} R3={R3:X2}\n" +
               $"  R4={R4} R5={R5} R6={R6} R7={R7}\n" +
               $"  R8={R8:X2} R9={R9} R10={R10:X2} R11={R11}";
    }
}
