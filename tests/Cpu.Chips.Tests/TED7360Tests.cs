using Cpu.Chips.TED7360;
using FluentAssertions;
using Xunit;

namespace Cpu.Chips.Tests;

public class TED7360RegisterTests
{
    private const ushort Base = 0xFF00;
    private readonly TED7360Chip _ted = new();

    [Fact]
    public void Reset_SetsDefaultRegisterValues()
    {
        _ted.Reset();
        _ted[TED7360Constants.REG_FF06_CR1].Should().Be(TED7360Constants.Default_FF06);
        _ted[TED7360Constants.REG_FF0A_IRQEN].Should().Be(TED7360Constants.Default_FF0A);
        _ted[TED7360Constants.REG_FF12_BMPADDR].Should().Be(TED7360Constants.Default_FF12);
        _ted[TED7360Constants.REG_FF13_CHARGEN].Should().Be(TED7360Constants.Default_FF13);
        _ted[TED7360Constants.REG_FF14_SCRADDR].Should().Be(TED7360Constants.Default_FF14);
    }

    [Fact]
    public void ReadByte_UnusedBitsAreOne()
    {
        _ted.Reset();
        for (int r = 0; r < 0x20; r++)
        {
            if (r == 0x09 || r == 0x13 || r == 0x1C || r == 0x1E) continue;
            if (r >= 0x0C && r <= 0x0D) continue;
            if (r == 0x08) continue;
            if (r == 0x0A) continue;
            if (r == 0x1D) continue;
            if (r >= 0x0E && r <= 0x10) continue;
            byte val = _ted.ReadByte((ushort)(Base + r));
            (val & 0x80).Should().Be(0x80, $"reg ${r:X2} unused bit7 should be 1");
        }
    }

    [Fact]
    public void ReadByte_IRQEN_HasBit7Set()
    {
        _ted.Reset();
        (_ted.ReadByte(Base + 0x0A) & 0x80).Should().Be(0x80);
    }

    [Fact]
    public void ReadByte_CHARGEN_HasBit0Set()
    {
        _ted.Reset();
        (_ted.ReadByte(Base + 0x13) & 0x01).Should().Be(0x01);
    }

    [Fact]
    public void WriteRead_RoundTrip_ForGeneralRegisters()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x06, 0x2B);
        byte read = _ted.ReadByte(Base + 0x06);
        read.Should().Be(0xAB, "CR1 write 0x2B → read 0x2B|0x80 (unused bit7=1)");
    }

    [Fact]
    public void Write_FF14_FiltersTop3Bits()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x14, 0xFF);
        (_ted[0x14] & 0xF8).Should().Be(0xF8);
        (_ted[0x14] & 0x07).Should().Be(0x00);
    }

    [Fact]
    public void Write_FF11_FiltersBit5()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x11, 0xFF);
        (_ted[0x11] & 0x20).Should().Be(0x00);
        (_ted[0x11] & 0xDF).Should().Be(0xDF);
    }
}

public class TED7360TimerTests
{
    private const ushort Base = 0xFF00;
    private readonly TED7360Chip _ted = new();

    [Fact]
    public void Timer1_CountsDownAndReloads()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x00, 0x05);
        _ted.WriteByte(Base + 0x01, 0x00);
        _ted.Timer1.Should().Be(0x0005);
        for (int i = 0; i < 5; i++)
            _ted.Update();
        _ted.Timer1.Should().Be(0x0000, "timer reached zero");
        _ted.Update();
        (_ted[TED7360Constants.REG_FF09_IRQST] & TED7360Constants.IRQST_TIMER1).Should().Be(TED7360Constants.IRQST_TIMER1);
        _ted.Timer1.Should().Be(0x0005, "timer1 should reload on underflow");
    }

    [Fact]
    public void Timer2_StartsFromFFFF_FiresOnUnderflow()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x02, 0x01);
        _ted.WriteByte(Base + 0x03, 0x00);
        for (int i = 0; i < 2; i++)
            _ted.Update();
        (_ted[TED7360Constants.REG_FF09_IRQST] & TED7360Constants.IRQST_TIMER2).Should().Be(TED7360Constants.IRQST_TIMER2);
    }

    [Fact]
    public void IRQ_ClearedByWritingToFF09()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x0A, 0x88);
        _ted.WriteByte(Base + 0x00, 0x01);
        _ted.WriteByte(Base + 0x01, 0x00);
        _ted.Update();
        _ted.Update();
        _ted.HasInterrupt.Should().BeTrue();
        _ted.WriteByte(Base + 0x09, TED7360Constants.IRQST_TIMER1);
        _ted.HasInterrupt.Should().BeFalse();
    }

    [Fact]
    public void Timer1_DoesNotFire_WhenNotStarted()
    {
        _ted.Reset();
        _ted.WriteByte(Base + 0x00, 0x10);
        _ted.Update();
        (_ted[TED7360Constants.REG_FF09_IRQST] & TED7360Constants.IRQST_TIMER1).Should().Be(0);
    }
}

public class TED7360RasterTests
{
    private const ushort Base = 0xFF00;
    private readonly TED7360Chip _ted = new();

    [Fact]
    public void Raster_IncrementsOnUpdate()
    {
        _ted.Reset();
        _ted.ReadByte(Base + 0x1D).Should().Be(0);
        _ted.Update();
        _ted.RasterCounter.Should().Be(1);
    }

    [Fact]
    public void Raster_WrapsAtScanlineCount()
    {
        _ted.Reset();
        for (int i = 0; i < TED7360Constants.TotalScanlinesNtsc; i++)
            _ted.Update();
        _ted.RasterCounter.Should().Be(0);
    }
}

public class TED7360PaletteTests
{
    [Fact]
    public void ToRgb_Black_ReturnsZero()
    {
        var (r, g, b) = TED7360Palette.ToRgb(0x00);
        r.Should().Be(0); g.Should().Be(0); b.Should().Be(0);
    }

    [Fact]
    public void ToRgb_WhiteFullLuma_ReturnsNonZero()
    {
        var (r, g, b) = TED7360Palette.ToRgb(0x71);
        r.Should().BeGreaterThan(0);
        g.Should().BeGreaterThan(0);
        b.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Pack_ProducesCorrectByte()
    {
        TED7360Palette.Pack(7, 1).Should().Be(0x71);
    }

    [Fact]
    public void Chroma0_AllLumas_AreBlack()
    {
        for (int l = 0; l <= 7; l++)
        {
            var (r, g, b) = TED7360Palette.ToRgb(TED7360Palette.Pack(l, 0));
            (r, g, b).Should().Be((0, 0, 0), $"luma={l} chroma=0 should be black");
        }
    }
}

public class TED7360PalModeTests
{
    [Fact]
    public void SetPalMode_SetsCr2Bit6()
    {
        var ted = new TED7360Chip();
        ted.SetPalMode(false);
        (ted[TED7360Constants.REG_FF07_CR2] & TED7360Constants.CR2_NTSCPAL).Should().Be(TED7360Constants.CR2_NTSCPAL);
    }

    [Fact]
    public void SetPalMode_ClearsCr2Bit6()
    {
        var ted = new TED7360Chip();
        ted.SetPalMode(true);
        (ted[TED7360Constants.REG_FF07_CR2] & TED7360Constants.CR2_NTSCPAL).Should().Be(0);
    }
}
