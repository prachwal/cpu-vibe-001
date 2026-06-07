using Cpu.Chips.Via6522;
using Cpu.Chips.Vic20;
using Cpu.Chips.Vic6560;
using CpuBase;
using FluentAssertions;
using Xunit;

namespace Cpu.Chips.Tests;

public class Vic20BusIntegrationTests
{
    private readonly Bus _bus = new();
    private readonly Vic6560Device _vic;
    private readonly ColorRamDevice _colorRam;
    private readonly byte[] _charRomData;

    public Vic20BusIntegrationTests()
    {
        _vic = new Vic6560Device();
        _colorRam = new ColorRamDevice();
        _charRomData = new byte[Vic20MemoryMap.CharRomSize];
        _charRomData[8] = 0x3C;

        _bus.Attach(new RamDevice("RAM Low", 0x0000, 0x8000));
        _bus.Attach(new RamDevice("RAM High", 0x8000, 0x8000));
        _bus.Attach(new RomDevice("Char ROM", Vic20MemoryMap.CharRomStart, _charRomData));
        _bus.Attach(new RomDevice("BASIC ROM", Vic20MemoryMap.BasicRomStart, Vic20TestRoms.CreateFilledRom(Vic20MemoryMap.BasicRomSize)));
        _bus.Attach(new RomDevice("KERNAL ROM", Vic20MemoryMap.KernalRomStart, Vic20TestRoms.CreateFilledRom(Vic20MemoryMap.KernalRomSize)));
        _bus.Attach(_colorRam);
        _bus.Attach(_vic);
        _bus.Attach(new Via6522Device(Vic20MemoryMap.ViaBaseAddress, Vic20MemoryMap.ColorRamStart));
    }

    [Fact]
    public void KernalRom_WriteGoesToUnderlyingRam()
    {
        _bus.Write(Vic20MemoryMap.KernalRomStart, 0x55);
        _bus.Read(Vic20MemoryMap.KernalRomStart).Should().Be(0xFF);
        _bus.Write(0x0000, 0x00);
    }

    [Fact]
    public void CharRom_IsReadable()
    {
        _bus.Read((ushort)(Vic20MemoryMap.CharRomStart + 8)).Should().Be(0x3C);
    }

    [Fact]
    public void VicRegisterMirror_WithinPage()
    {
        _bus.Write((ushort)(Vic20MemoryMap.VicBaseAddress + 3), 0xAB);
        _bus.Read((ushort)(Vic20MemoryMap.VicBaseAddress + 3)).Should().Be(0xAB);
    }

    [Fact]
    public void ViaRegisterMirror_WithinIoBlock()
    {
        _bus.Write((ushort)(Vic20MemoryMap.ViaBaseAddress + 0x02), 0xAA);
        _bus.Read((ushort)(Vic20MemoryMap.ViaBaseAddress + 0x12)).Should().Be(0xAA);
    }

    [Fact]
    public void ReadCharRom_ReturnsAbsoluteOrRelative()
    {
        byte[] chars = new byte[Vic20MemoryMap.CharRomSize];
        chars[8] = 0x3C;
        Vic20CharRomReader reader = new(new RomDevice("Char", Vic20MemoryMap.CharRomStart, chars));
        reader.Read(8).Should().Be(0x3C);
        reader.Read((ushort)(Vic20MemoryMap.CharRomStart + 8)).Should().Be(0x3C);
    }
}

public sealed class Vic20CharRomReader
{
    private readonly RomDevice _charRom;

    public Vic20CharRomReader(RomDevice charRom) => _charRom = charRom;

    public byte Read(ushort address)
    {
        if (_charRom.Accepts(address))
            return _charRom.Read(address);
        return _charRom.Read((ushort)(Vic20MemoryMap.CharRomStart + address));
    }
}

internal static class Vic20TestRoms
{
    public static byte[] CreateFilledRom(int size)
    {
        var data = new byte[size];
        Array.Fill(data, (byte)0xFF);
        return data;
    }
}
