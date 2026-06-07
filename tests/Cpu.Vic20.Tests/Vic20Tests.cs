using Cpu.Chips.Vic20;
using Cpu.Chips.Vic6560;
using Cpu.Vic20.System;
using Cpu.Vic20.Video;
using FluentAssertions;
using Xunit;

namespace Cpu.Vic20.Tests;

public class Vic20VideoTests
{
    [Fact]
    public void RenderFrame_NormalText_RendersInkAndPaper()
    {
        var vic = new Vic6560Chip();
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 2, 0x16);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 3, 0x0C);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 0x0F, 0x20);

        byte[] chars = new byte[Vic20MemoryMap.CharRomSize];
        for (int i = 0; i < 8; i++)
            chars[i * 8] = 0xFF;

        var video = new Vic20Video(
            vic,
            _ => (byte)0,
            offset => chars[offset & 0xFFF],
            _ => 0x01);

        video.RenderFrame();
        video.Pixels.ToArray().Should().Contain(0x01);
        video.Pixels.ToArray().Should().Contain(0x02);
    }

    [Fact]
    public void RenderFrame_ReverseMode_SwapsInkAndPaper()
    {
        var vic = new Vic6560Chip();
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 2, 0x01);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 3, 0x02);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 0x0F, 0x98);

        byte[] chars = new byte[Vic20MemoryMap.CharRomSize];
        chars[0] = 0xFF;

        var video = new Vic20Video(
            vic,
            _ => 0,
            offset => chars[offset & 0xFFF],
            _ => 0x01);

        video.RenderFrame();
        video.Pixels[0].Should().Be(0x09);
    }

    [Fact]
    public void RenderFrame_Multicolor_UsesSharedColors()
    {
        var vic = new Vic6560Chip();
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 2, 0x01);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 3, 0x02);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 0x0E, 0xA0);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 0x0F, 0x20);

        byte[] chars = new byte[Vic20MemoryMap.CharRomSize];
        chars[0] = 0x55;

        var video = new Vic20Video(
            vic,
            _ => 0,
            offset => chars[offset & 0xFFF],
            _ => 0x09);

        video.RenderFrame();
        video.Pixels.ToArray().Should().Contain(0x0A);
    }

    [Fact]
    public void RenderFrame_ReverseScreenCode_SwapsInkAndPaper()
    {
        var vic = new Vic6560Chip();
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 2, 0x16);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 3, 0x0C);
        vic.WriteByte(Vic20MemoryMap.VicBaseAddress + 0x0F, 0x20);

        byte[] chars = new byte[Vic20MemoryMap.CharRomSize];
        chars[0] = 0xFF;

        var video = new Vic20Video(
            vic,
            _ => 0x80,
            offset => chars[offset & 0x7F],
            _ => 0x01);

        video.RenderFrame();
        video.Pixels[0].Should().Be(0x02);
    }

    [Fact]
    public void ToPixelBuffer_MapsPaletteIndices()
    {
        var vic = new Vic6560Chip();
        var video = new Vic20Video(vic, _ => 0, _ => 0xFF, _ => 0);
        video.RenderFrame();
        var buffer = video.ToPixelBuffer();
        buffer.Width.Should().Be(Vic20Video.MaxWidth);
        buffer.Height.Should().Be(Vic20Video.MaxHeight);
    }
}

public class Vic20KeyboardTests
{
    [Fact]
    public void Matrix_ReadColumn_ReturnsActiveKeys()
    {
        var matrix = new Devices.Vic20KeyboardMatrix();
        matrix.SetRowSelect(0xFE);
        matrix.Press(0, 1);
        matrix.ReadColumns().Should().NotBe(0xFF);
    }
}

public class Vic20MachineTests
{
    [Fact]
    public void LoadProfile_AttachesVicAndVia()
    {
        using var machine = Vic20Machine.Load("vic20-ntsc.json");
        machine.Vic.Chip.Columns.Should().Be(0);
        machine.Via.BaseAddress.Should().Be(Vic20MemoryMap.ViaBaseAddress);
    }

    [Fact]
    public void ColorRam_WriteStoresNibble()
    {
        using var machine = Vic20Machine.Load("vic20-ntsc.json");
        machine.ColorRam.Write(Vic20MemoryMap.ColorRamStart, 0xAF);
        machine.ColorRam.Read(Vic20MemoryMap.ColorRamStart).Should().Be(0x0F);
    }
}
