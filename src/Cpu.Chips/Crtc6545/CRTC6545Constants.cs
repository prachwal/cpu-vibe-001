namespace Cpu.Chips.Crtc6545;

public static class CRTC6545Constants
{
    public const int ADDRESS_REG_OFFSET = 0;
    public const int VALUE_REG_OFFSET = 1;
    public const int REGISTER_COUNT = 2;

    public const int R0_HORIZONTAL_TOTAL = 0;
    public const int R1_HORIZONTAL_DISPLAY = 1;
    public const int R2_HORIZONTAL_SYNC_POS = 2;
    public const int R3_SYNC_WIDTHS = 3;
    public const int R4_VERTICAL_TOTAL = 4;
    public const int R5_VERTICAL_ADJUST = 5;
    public const int R6_VERTICAL_DISPLAY = 6;
    public const int R7_VERTICAL_SYNC_POS = 7;
    public const int R8_MODE_CONTROL = 8;
    public const int R9_SCAN_LINES = 9;
    public const int R10_CURSOR_START = 10;
    public const int R11_CURSOR_END = 11;
    public const int R12_DISPLAY_START_H = 12;
    public const int R13_DISPLAY_START_L = 13;
    public const int R14_CURSOR_H = 14;
    public const int R15_CURSOR_L = 15;
    public const int R16_LIGHTPEN_H = 16;
    public const int R17_LIGHTPEN_L = 17;

    public const int INTERNAL_REGISTER_COUNT = 18;

    public const byte STATUS_VRT = 0x20;
    public const byte STATUS_LRF = 0x40;

    public const byte R8_INTERLACE_BIT = 0;
    public const byte R8_ADDRESS_MODE_BIT = 2;
    public const byte R8_DE_SKEW_BIT = 4;
    public const byte R8_CE_SKEW_BIT = 5;

    public const byte R8_INTERLACE_MASK = 1 << R8_INTERLACE_BIT;
    public const byte R8_ADDRESS_MODE_MASK = 1 << R8_ADDRESS_MODE_BIT;
    public const byte R8_DE_SKEW_MASK = 1 << R8_DE_SKEW_BIT;
    public const byte R8_CE_SKEW_MASK = 1 << R8_CE_SKEW_BIT;

    public const byte R3_HSYNC_WIDTH_MASK = 0x0F;
    public const byte R3_VSYNC_WIDTH_MASK = 0xF0;

    public const byte R10_CURSOR_MODE_MASK = 0x60;
    public const byte R10_CURSOR_START_MASK = 0x1F;
    public const int R10_CURSOR_MODE_SHIFT = 5;

    public const byte CURSOR_MODE_NON_BLINK = 0x00;
    public const byte CURSOR_MODE_DISABLED = 0x20;
    public const byte CURSOR_MODE_BLINK_1_16 = 0x40;
    public const byte CURSOR_MODE_BLINK_1_32 = 0x60;

    public const int MA_ADDR_BITS = 14;
    public const int RA_ADDR_BITS = 5;
}
