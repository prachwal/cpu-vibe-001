namespace Cpu.Chips.TED7360;

public sealed class TED7360Video
{
    private readonly byte[] _pixels = new byte[TED7360Constants.VideoWidth * TED7360Constants.VideoHeight];
    private readonly TED7360Chip _chip;
    private byte[] _flashState;

    public TED7360Video(TED7360Chip chip)
    {
        _chip = chip;
        _flashState = new byte[0];
    }

    public ReadOnlySpan<byte> Pixels => _pixels;

    public bool IsPAL => _chip.PalMode;

    public void RenderFrame(Func<ushort, byte> readMemory, Func<ushort, byte> readCharRom)
    {
        Array.Clear(_pixels, 0, _pixels.Length);

        byte cr1 = _chip[TED7360Constants.REG_FF06_CR1];
        byte cr2 = _chip[TED7360Constants.REG_FF07_CR2];
        byte cr12 = _chip[TED7360Constants.REG_FF12_BMPADDR];

        bool den = (cr1 & TED7360Constants.CR1_DEN) != 0;
        if (!den) return;

        bool ecm = (cr1 & TED7360Constants.CR1_ECM) != 0;
        bool bmm = (cr1 & TED7360Constants.CR1_BMM) != 0;
        bool mcm = (cr2 & TED7360Constants.CR2_MCM) != 0;
        bool rvdis = (cr2 & TED7360Constants.CR2_RVSDIS) != 0;
        bool rsel = (cr1 & TED7360Constants.CR1_RSEL) != 0;
        bool csel = (cr2 & TED7360Constants.CR2_CSEL) != 0;

        int rows = rsel ? 24 : 25;
        int cols = csel ? 38 : 40;

        int screenAddrBase = (_chip[TED7360Constants.REG_FF14_SCRADDR] & 0xF8) << 8;
        int chargenAddr = (_chip[TED7360Constants.REG_FF13_CHARGEN] & 0xFC) << 8;
        bool bmprom = (cr12 & 0x04) != 0;

        byte ec = _chip[TED7360Constants.REG_FF19_EC];
        byte b0c = _chip[TED7360Constants.REG_FF15_B0C];
        byte b1c = _chip[TED7360Constants.REG_FF16_B1C];
        byte b2c = _chip[TED7360Constants.REG_FF17_B2C];
        byte b3c = _chip[TED7360Constants.REG_FF18_B3C];

        bool isBitmap = bmm;
        bool isEcm = ecm && !bmm;

        int flashPhase = (_chip[TED7360Constants.REG_FF1F_FLASH] >> 3) & 0x0F;
        bool flashOn = flashPhase >= 8;

        if (_flashState.Length != rows * cols)
            _flashState = new byte[rows * cols];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int charOffset = row * cols + col;
                int screenAddr = screenAddrBase + charOffset;
                int colorAddr = screenAddrBase + 0x400 + charOffset;

                byte charCode = bmprom
                    ? readMemory((ushort)screenAddr)
                    : readCharRom((ushort)screenAddr);
                byte colorByte = readMemory((ushort)colorAddr);

                bool flash = (colorByte & 0x80) != 0;
                if (flash && flashOn)
                    charCode = 0x20;

                if (!isBitmap)
                {
                    int effectiveCode;
                    int foregroundColor;
                    byte bg = b0c;

                    if (isEcm)
                    {
                        effectiveCode = charCode & 0x3F;
                        int bgSel = (charCode >> 6) & 0x03;
                        bg = bgSel switch { 1 => b1c, 2 => b2c, 3 => b3c, _ => b0c };
                        foregroundColor = colorByte & 0x7F;
                    }
                    else if (mcm)
                    {
                        effectiveCode = rvdis ? charCode : (charCode & 0x7F);
                        bool hires = (colorByte & 0x08) != 0;
                        foregroundColor = (byte)((colorByte & 0x70) | (colorByte & 0x07));
                        if (!hires) { }
                    }
                    else
                    {
                        effectiveCode = rvdis ? charCode : (charCode & 0x7F);
                        bool inverse = rvdis ? false : (charCode & 0x80) != 0;
                        foregroundColor = colorByte & 0x7F;
                        if (inverse)
                            (bg, foregroundColor) = ((byte)(foregroundColor & 0x7F), bg);
                    }

                    int charAddr = chargenAddr + effectiveCode * 8;
                    for (int ly = 0; ly < 8; ly++)
                    {
                        byte glyph = readCharRom((ushort)(charAddr + ly));
                        int py = row * 8 + ly;
                        for (int lx = 0; lx < 8; lx++)
                        {
                            int px = col * 8 + lx;
                            if (px >= TED7360Constants.VideoWidth || py >= TED7360Constants.VideoHeight)
                                continue;

                            bool pixelOn = (glyph & (1 << (7 - lx))) != 0;
                            byte pixelColor;
                            if (mcm && !isEcm)
                            {
                                int pair = (glyph >> (6 - (lx / 2))) & 0x03;
                                pixelColor = (byte)(pair switch
                                {
                                    0 => (int)bg,
                                    1 => (int)b1c,
                                    2 => (int)b2c,
                                    3 => (int)foregroundColor,
                                    _ => 0
                                });
                            }
                            else
                            {
                                pixelColor = pixelOn ? (byte)(foregroundColor & 0x7F) : bg;
                            }
                            _pixels[py * TED7360Constants.VideoWidth + px] = pixelColor;
                        }
                    }
                }
            }
        }
    }
}
