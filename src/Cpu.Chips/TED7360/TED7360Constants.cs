namespace Cpu.Chips.TED7360;

public static class TED7360Constants
{
    public const int RegisterCount = 64;

    public const byte Default_FF06 = 0x1B;
    public const byte Default_FF07_Ntsc = 0x48;
    public const byte Default_FF07_Pal = 0x08;
    public const byte Default_FF0A = 0x00;
    public const byte Default_FF12 = 0xC4;
    public const byte Default_FF13 = 0xD1;
    public const byte Default_FF14 = 0x0F;

    public const int REG_FF00_TIMER1LO = 0x00;
    public const int REG_FF01_TIMER1HI = 0x01;
    public const int REG_FF02_TIMER2LO = 0x02;
    public const int REG_FF03_TIMER2HI = 0x03;
    public const int REG_FF04_TIMER3LO = 0x04;
    public const int REG_FF05_TIMER3HI = 0x05;

    public const int REG_FF06_CR1 = 0x06;
    public const byte CR1_TEST = 0x80;
    public const byte CR1_ECM = 0x40;
    public const byte CR1_BMM = 0x20;
    public const byte CR1_DEN = 0x10;
    public const byte CR1_RSEL = 0x08;
    public const byte CR1_YSCROLL_MASK = 0x07;

    public const int REG_FF07_CR2 = 0x07;
    public const byte CR2_RVSDIS = 0x80;
    public const byte CR2_NTSCPAL = 0x40;
    public const byte CR2_TEDOFF = 0x20;
    public const byte CR2_MCM = 0x10;
    public const byte CR2_CSEL = 0x08;
    public const byte CR2_XSCROLL_MASK = 0x07;

    public const int REG_FF08_KEYBOARD = 0x08;

    public const int REG_FF09_IRQST = 0x09;
    public const byte IRQST_IRQ = 0x80;
    public const byte IRQST_TIMER3 = 0x40;
    public const byte IRQST_TIMER2 = 0x10;
    public const byte IRQST_TIMER1 = 0x08;
    public const byte IRQST_ILP = 0x04;
    public const byte IRQST_IRST = 0x02;

    public const int REG_FF0A_IRQEN = 0x0A;
    public const int REG_FF0B_RSTCMP = 0x0B;
    public const byte IRQEN_TIMER3 = 0x40;
    public const byte IRQEN_TIMER2 = 0x10;
    public const byte IRQEN_TIMER1 = 0x08;
    public const byte IRQEN_LPIE = 0x04;
    public const byte IRQEN_ERST = 0x02;
    public const byte IRQEN_RSTCMP8 = 0x01;

    public const int REG_FF0C_CURSORHI = 0x0C;
    public const int REG_FF0D_CURSORLO = 0x0D;

    public const int REG_FF0E_SND1FREQLO = 0x0E;
    public const int REG_FF0F_SND2FREQLO = 0x0F;
    public const int REG_FF10_SND2FREQHI = 0x10;

    public const int REG_FF11_SCR = 0x11;
    public const byte SCR_SNDDC = 0x80;
    public const byte SCR_SND2NOISE = 0x40;
    public const byte SCR_SND2ON = 0x20;
    public const byte SCR_SND1ON = 0x10;
    public const byte SCR_VOLUME_MASK = 0x0F;

    public const int REG_FF12_BMPADDR = 0x12;
    public const int REG_FF13_CHARGEN = 0x13;
    public const byte CHARGEN_SINGLECLK = 0x02;
    public const byte CHARGEN_ROMRAM = 0x01;

    public const int REG_FF14_SCRADDR = 0x14;

    public const int REG_FF15_B0C = 0x15;
    public const int REG_FF16_B1C = 0x16;
    public const int REG_FF17_B2C = 0x17;
    public const int REG_FF18_B3C = 0x18;
    public const int REG_FF19_EC = 0x19;

    public const int REG_FF1A_STCHPOSHI = 0x1A;
    public const int REG_FF1B_STCHPOSLO = 0x1B;

    public const int REG_FF1C_RSTL8 = 0x1C;
    public const int REG_FF1D_RSTL = 0x1D;
    public const int REG_FF1E_LINEPOS = 0x1E;

    public const int REG_FF1F_FLASH = 0x1F;
    public const byte FLASH_FLASH_MASK = 0x78;
    public const byte FLASH_CHRAST_MASK = 0x07;

    public const int REG_FF3E_SWITCHROM = 0x3E;
    public const int REG_FF3F_SWITCHRAM = 0x3F;

    public const double Phi2Ntsc = 14_318_181.0 / 14.0;
    public const double Phi2Pal = 17_734_475.0 / 14.0;

    public const int TotalScanlinesNtsc = 262;
    public const int TotalScanlinesPal = 312;

    public const int VisibleScanlines = 200;
    public const int MaxCharsPerRowNtsc = 40;
    public const int MaxCharsPerRowPal = 40;

    public const int VideoWidth = 320;
    public const int VideoHeight = 200;

    public static int TotalScanlines(bool pal) => pal ? TotalScanlinesPal : TotalScanlinesNtsc;
    public static double Phi2(bool pal) => pal ? Phi2Pal : Phi2Ntsc;
    public static byte Default_FF07(bool pal) => pal ? Default_FF07_Pal : Default_FF07_Ntsc;
}
