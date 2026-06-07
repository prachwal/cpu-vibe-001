namespace Cpu.Chips.TED7360;

public sealed class TED7360Chip
{
    private readonly byte[] _reg = new byte[TED7360Constants.RegisterCount];
    private int _rasterCounter;
    private bool _palMode;
    private bool _irq;

    private ushort _timer1, _timer1Latch;
    private ushort _timer2, _timer3;
    private bool _t1Running, _t2Running, _t3Running;

    private byte _keyboardRow;

    private int _flashCounter;
    private int _flashTick;

    public TED7360Chip()
    {
        Reset();
    }

    public byte this[int reg] => _reg[reg & 0x3F];

    public bool PalMode => _palMode;
    public int RasterCounter => _rasterCounter;
    public bool HasInterrupt => _irq;

    public ushort Timer1 => _timer1;
    public ushort Timer2 => _timer2;
    public ushort Timer3 => _timer3;

    public byte ReadByte(ushort address)
    {
        int offset = address & 0x3F;
        if (offset >= 0x20 && offset < 0x3E)
            return _reg[offset];

        switch (offset)
        {
            case TED7360Constants.REG_FF01_TIMER1HI:
                return (byte)(_t1Running ? (_timer1 >> 8) : (_reg[offset] | 0x80));

            case TED7360Constants.REG_FF03_TIMER2HI:
                return (byte)(_timer2 >> 8);

            case TED7360Constants.REG_FF05_TIMER3HI:
                return (byte)(_timer3 >> 8);

            case TED7360Constants.REG_FF08_KEYBOARD:
                return (byte)(_keyboardRow | 0x80);

            case TED7360Constants.REG_FF09_IRQST:
                return _reg[offset];

            case TED7360Constants.REG_FF0A_IRQEN:
                return (byte)(_reg[offset] | 0x80);

            case TED7360Constants.REG_FF0E_SND1FREQLO:
            case TED7360Constants.REG_FF0F_SND2FREQLO:
            case TED7360Constants.REG_FF10_SND2FREQHI:
                return _reg[offset];

            case TED7360Constants.REG_FF13_CHARGEN:
                return (byte)(_reg[offset] | 0x01);

            case TED7360Constants.REG_FF1C_RSTL8:
                return (byte)((_rasterCounter >> 1) & 0x80);

            case TED7360Constants.REG_FF1D_RSTL:
                return (byte)(_rasterCounter & 0xFF);

            case TED7360Constants.REG_FF1E_LINEPOS:
                return (byte)((_reg[offset] & 0x01) | 0xFE);

            case TED7360Constants.REG_FF3E_SWITCHROM:
            case TED7360Constants.REG_FF3F_SWITCHRAM:
                return 0;

            default:
                return (byte)(_reg[offset] | 0x80);
        }
    }

    public void WriteByte(ushort address, byte value)
    {
        int offset = address & 0x3F;
        if (offset >= 0x20 && offset < 0x3E)
        {
            _reg[offset] = value;
            return;
        }

        switch (offset)
        {
            case TED7360Constants.REG_FF00_TIMER1LO:
                _timer1 = (ushort)((_timer1 & 0xFF00) | value);
                _t1Running = false;
                _reg[offset] = value;
                return;

            case TED7360Constants.REG_FF01_TIMER1HI:
                _timer1 = (ushort)((_timer1 & 0x00FF) | (value << 8));
                _timer1Latch = _timer1;
                _t1Running = true;
                _reg[offset] = value;
                ClearIrqFlag(TED7360Constants.IRQST_TIMER1);
                return;

            case TED7360Constants.REG_FF02_TIMER2LO:
                _timer2 = (ushort)((_timer2 & 0xFF00) | value);
                _t2Running = false;
                _reg[offset] = value;
                return;

            case TED7360Constants.REG_FF03_TIMER2HI:
                _timer2 = (ushort)((_timer2 & 0x00FF) | (value << 8));
                _t2Running = true;
                _reg[offset] = value;
                ClearIrqFlag(TED7360Constants.IRQST_TIMER2);
                return;

            case TED7360Constants.REG_FF04_TIMER3LO:
                _timer3 = (ushort)((_timer3 & 0xFF00) | value);
                _t3Running = false;
                _reg[offset] = value;
                return;

            case TED7360Constants.REG_FF05_TIMER3HI:
                _timer3 = (ushort)((_timer3 & 0x00FF) | (value << 8));
                _t3Running = true;
                _reg[offset] = value;
                ClearIrqFlag(TED7360Constants.IRQST_TIMER3);
                return;

            case TED7360Constants.REG_FF08_KEYBOARD:
                _keyboardRow = value;
                _reg[offset] = value;
                return;

            case TED7360Constants.REG_FF09_IRQST:
                _reg[offset] &= (byte)~value;
                UpdateIrq();
                return;

            case TED7360Constants.REG_FF0C_CURSORHI:
                _reg[offset] = (byte)(value & 0x03);
                return;

            case TED7360Constants.REG_FF0A_IRQEN:
                _reg[offset] = (byte)(value & 0x5F);
                UpdateIrq();
                return;

            case TED7360Constants.REG_FF11_SCR:
                _reg[offset] = (byte)(value & 0xDF);
                return;

            case TED7360Constants.REG_FF12_BMPADDR:
                _reg[offset] = (byte)(value & 0x3F);
                return;

            case TED7360Constants.REG_FF13_CHARGEN:
                _reg[offset] = (byte)((value & 0xFE) | 0x01);
                return;

            case TED7360Constants.REG_FF14_SCRADDR:
                _reg[offset] = (byte)(value & 0xF8);
                return;

            case TED7360Constants.REG_FF15_B0C:
            case TED7360Constants.REG_FF16_B1C:
            case TED7360Constants.REG_FF17_B2C:
            case TED7360Constants.REG_FF18_B3C:
            case TED7360Constants.REG_FF19_EC:
                _reg[offset] = (byte)(value & 0x7F);
                return;

            case TED7360Constants.REG_FF1A_STCHPOSHI:
                _reg[offset] = (byte)(value & 0x03);
                return;

            case TED7360Constants.REG_FF1C_RSTL8:
                return;

            case TED7360Constants.REG_FF1D_RSTL:
                _rasterCounter = (_rasterCounter & 0x100) | value;
                _reg[offset] = value;
                return;

            case TED7360Constants.REG_FF1F_FLASH:
                _reg[offset] = (byte)((_reg[offset] & 0x80) | (value & 0x7F));
                return;

            case TED7360Constants.REG_FF3E_SWITCHROM:
            case TED7360Constants.REG_FF3F_SWITCHRAM:
                _reg[offset] = value;
                return;

            default:
                _reg[offset] = value;
                return;
        }
    }

    public void SetKeyboardColumns(byte columns)
    {
        _keyboardRow = columns;
    }

    public byte KeyboardRow => _keyboardRow;

    public void Reset()
    {
        Array.Clear(_reg, 0, _reg.Length);
        _rasterCounter = 0;
        _palMode = false;
        _irq = false;
        _timer1 = 0; _timer1Latch = 0;
        _timer2 = 0xFFFF; _timer3 = 0xFFFF;
        _t1Running = false; _t2Running = false; _t3Running = false;
        _keyboardRow = 0;
        _flashCounter = 0; _flashTick = 0;

        _reg[TED7360Constants.REG_FF06_CR1] = TED7360Constants.Default_FF06;
        _reg[TED7360Constants.REG_FF07_CR2] = TED7360Constants.Default_FF07_Pal;
        _reg[TED7360Constants.REG_FF0A_IRQEN] = TED7360Constants.Default_FF0A;
        _reg[TED7360Constants.REG_FF12_BMPADDR] = TED7360Constants.Default_FF12;
        _reg[TED7360Constants.REG_FF13_CHARGEN] = TED7360Constants.Default_FF13;
        _reg[TED7360Constants.REG_FF14_SCRADDR] = TED7360Constants.Default_FF14;
    }

    public void SetPalMode(bool pal)
    {
        _palMode = pal;
        byte cr2 = _reg[TED7360Constants.REG_FF07_CR2];
        if (pal)
            cr2 &= unchecked((byte)~TED7360Constants.CR2_NTSCPAL);
        else
            cr2 |= TED7360Constants.CR2_NTSCPAL;
        _reg[TED7360Constants.REG_FF07_CR2] = cr2;
    }

    public void Update()
    {
        _rasterCounter++;
        int total = TED7360Constants.TotalScanlines(_palMode);
        if (_rasterCounter >= total)
        {
            _rasterCounter = 0;
            _flashTick++;
            if (_flashTick >= 16)
            {
                _flashTick = 0;
                _flashCounter = (_flashCounter + 1) & 0x0F;
                _reg[TED7360Constants.REG_FF1F_FLASH] =
                    (byte)((_reg[TED7360Constants.REG_FF1F_FLASH] & 0x87) | (_flashCounter << 3));
            }
        }

        UpdateTimers();
        CheckRasterIrq();
    }

    public bool AcknowledgeInterrupt()
    {
        if (!_irq) return false;
        _irq = false;
        return true;
    }

    private void UpdateTimers()
    {
        if (_t1Running)
        {
            _timer1--;
            if (_timer1 == 0xFFFF)
            {
                _timer1 = _timer1Latch;
                SetIrqFlag(TED7360Constants.IRQST_TIMER1);
            }
        }

        if (_t2Running)
        {
            _timer2--;
            if (_timer2 == 0xFFFF)
            {
                _t2Running = false;
                SetIrqFlag(TED7360Constants.IRQST_TIMER2);
            }
        }

        if (_t3Running)
        {
            _timer3--;
            if (_timer3 == 0xFFFF)
            {
                _t3Running = false;
                SetIrqFlag(TED7360Constants.IRQST_TIMER3);
            }
        }
    }

    private void CheckRasterIrq()
    {
        int compare = (_reg[TED7360Constants.REG_FF0B_RSTCMP]) | ((_reg[TED7360Constants.REG_FF0A_IRQEN] & 1) << 8);
        if ((_rasterCounter & 0x1FF) == (compare & 0x1FF))
        {
            SetIrqFlag(TED7360Constants.IRQST_IRST);
        }
    }

    private void SetIrqFlag(byte bit)
    {
        _reg[TED7360Constants.REG_FF09_IRQST] |= bit;
        UpdateIrq();
    }

    private void ClearIrqFlag(byte bit)
    {
        _reg[TED7360Constants.REG_FF09_IRQST] &= (byte)~bit;
        UpdateIrq();
    }

    private void UpdateIrq()
    {
        byte iflags = _reg[TED7360Constants.REG_FF09_IRQST];
        byte ien = _reg[TED7360Constants.REG_FF0A_IRQEN];
        bool any = false;
        if ((iflags & TED7360Constants.IRQST_TIMER1) != 0 && (ien & TED7360Constants.IRQEN_TIMER1) != 0)
            any = true;
        if ((iflags & TED7360Constants.IRQST_TIMER2) != 0 && (ien & TED7360Constants.IRQEN_TIMER2) != 0)
            any = true;
        if ((iflags & TED7360Constants.IRQST_TIMER3) != 0 && (ien & TED7360Constants.IRQEN_TIMER3) != 0)
            any = true;
        if ((iflags & TED7360Constants.IRQST_IRST) != 0 && (ien & TED7360Constants.IRQEN_ERST) != 0)
            any = true;

        if (any)
        {
            _reg[TED7360Constants.REG_FF09_IRQST] |= TED7360Constants.IRQST_IRQ;
            _irq = true;
        }
        else
        {
            _reg[TED7360Constants.REG_FF09_IRQST] &= unchecked((byte)~TED7360Constants.IRQST_IRQ);
            _irq = false;
        }
    }
}
