using Cpu.Chips.Vic6560;
using Cpu.Chips.Vic20;
using FluentAssertions;
using Xunit;

namespace Cpu.Chips.Tests;

public class Vic6560RegisterTests
{
    private const ushort Base = 0x9000;
    private readonly Vic6560Chip _vic = new(Base);

    [Fact]
    public void AllSixteenRegisters_ReadWrite()
    {
        _vic.Reset();
        for (int i = 0; i < 16; i++)
        {
            _vic.WriteByte((ushort)(Base + i), (byte)(0xA0 + i));
            _vic.ReadByte((ushort)(Base + i)).Should().Be((byte)(0xA0 + i));
        }
    }

    [Fact]
    public void CR0_InterlaceBit()
    {
        _vic.Reset();
        _vic.WriteByte(Base + 0x00, 0x80);
        _vic.InterlaceMode.Should().BeTrue();
    }

    [Fact]
    public void CR2_HBit_AndColumns()
    {
        _vic.Reset();
        _vic.WriteByte(Base + 0x02, 0x96);
        (_vic[0x02] & 0x80).Should().Be(0x80);
        _vic.Columns.Should().Be(0x16);
    }

    [Fact]
    public void CR5_ScreenAddr_DecodesCorrectly()
    {
        _vic.Reset();
        _vic.WriteByte(Base + 0x05, 0xC0);
        _vic.WriteByte(Base + 0x02, 0x16);
        _vic.ScreenMatrixBase.Should().Be(0x3000);
        _vic.ScreenAddr.Should().Be(0x1000);
    }

    [Fact]
    public void CR5_CharAddr_DecodesWithA15Inversion()
    {
        _vic.Reset();
        _vic.WriteByte(Base + 0x05, 0xC0);
        _vic.CharMatrixBase.Should().Be(0);
        _vic.CharAddr.Should().Be(0x8000);
    }

    [Fact]
    public void CR15_ScreenColor_Reverse_Border()
    {
        _vic.Reset();
        _vic.WriteByte(Base + 0x0F, 0x9B);
        _vic.ScreenColor.Should().Be(0x09);
        _vic.ReverseMode.Should().BeTrue();
        _vic.BorderColor.Should().Be(0x03);
    }

    [Fact]
    public void AddressDecode_MirrorsWithinPage()
    {
        _vic.Reset();
        _vic.WriteByte(Base + 0x00, 0xFF);
        _vic.ReadByte(Base + 0x10).Should().Be(0xFF);
        _vic.ReadByte(Base + 0x20).Should().Be(0xFF);
    }
}

public class Vic6560RasterTests
{
    private const ushort Base = 0x9000;
    private readonly Vic6560Chip _vic = new(Base);

    [Fact]
    public void Raster_IncrementsOnUpdate()
    {
        _vic.Reset();
        _vic.Raster.Should().Be(0);
        _vic.Update();
        _vic.Raster.Should().Be(1);
    }

    [Fact]
    public void Raster_WrapsAtScanlineCount()
    {
        _vic.Reset();
        for (int i = 0; i < _vic.TotalScanlines; i++)
            _vic.Update();
        _vic.Raster.Should().Be(0);
    }

    [Fact]
    public void RasterBit8_ReflectedInCR3()
    {
        _vic.Reset();
        for (int i = 0; i < 256; i++)
            _vic.Update();
        (_vic.ReadByte(Base + 0x03) & 0x80).Should().Be(0x80);
    }
}

public class Vic6560AudioTests
{
    private readonly Vic6560Chip _vic = new();

    [Fact]
    public void GetAudioSample_ZeroWhenDisabled()
    {
        _vic.Reset();
        _vic.GetAudioSample().Should().Be(0);
    }

    [Fact]
    public void GetAudioSample_ScalesWithVolume()
    {
        _vic.Reset();
        _vic.WriteByte(0x900A, 0xFF);
        _vic.WriteByte(0x900E, 0x0F);
        _vic.AdvanceOscillators(0.001);
        Math.Abs(_vic.GetAudioSample()).Should().BeGreaterThan(0);
    }
}

public class ColorRamDeviceTests
{
    [Fact]
    public void Write_StoresOnlyLowNibble()
    {
        var color = new ColorRamDevice();
        color.Write(Vic20MemoryMap.ColorRamStart, 0xAF);
        color.Read(Vic20MemoryMap.ColorRamStart).Should().Be(0x0F);
    }
}
