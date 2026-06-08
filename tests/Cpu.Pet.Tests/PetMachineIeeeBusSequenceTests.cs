using Cpu.Board.Core;
using Cpu.Pet.System;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class PetMachineIeeeBusSequenceTests
{
    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    private static PetMachine CreateMachine()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        var machine = new PetMachine(profile);
        machine.MountDisk(Path.Combine(TestDisksDir, "games-1.d64"));
        return machine;
    }

    [Fact]
    public void IeeeListen_DataOut_ReachesDrive()
    {
        using var machine = CreateMachine();
        var bus = machine.IeeeBus;
        var drive = machine.DiskDrive;

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x45);
        bus.OnDioWrite(0x4C);
        bus.OnDioWrite(0x4C);
        bus.OnDioWrite(0x4F);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        drive.Engine.OpenChannel(0);
        drive.DataAvailable.Should().BeTrue();

        drive.TryRead(out byte lo).Should().BeTrue();
        drive.TryRead(out byte hi).Should().BeTrue();
        ushort loadAddr = (ushort)(hi << 8 | lo);
        loadAddr.Should().BeGreaterThan(0x0100);
    }

    [Fact]
    public void IeeeListenTalk_LoadsHello()
    {
        using var machine = CreateMachine();
        var bus = machine.IeeeBus;
        var drive = machine.DiskDrive;

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        foreach (byte b in new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F })
            bus.OnDioWrite(b);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        drive.DataAvailable.Should().BeTrue();
        drive.TryRead(out byte lo).Should().BeTrue();
        drive.TryRead(out byte hi).Should().BeTrue();

        ushort loadAddr = (ushort)(hi << 8 | lo);
        loadAddr.Should().BeGreaterThan(0x0100);
    }

    [Fact]
    public void IeeeCommandInit_ReturnsOk()
    {
        using var machine = CreateMachine();
        var bus = machine.IeeeBus;
        var drive = machine.DiskDrive;

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x6F);
        bus.OnATNWrite(false);

        bus.OnDioWrite(0x49);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        drive.Engine.OpenChannel(15);
        drive.DataAvailable.Should().BeTrue();

        string error = ReadErrorFromDrive(drive);
        error.Should().Contain("00, OK");
    }

    [Fact]
    public void ViaPortBInput_ReflectsBusHandshake()
    {
        using var machine = CreateMachine();
        var bus = machine.IeeeBus;

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);

        byte pb = bus.GetViaPortBInput();
        (pb & 0x01).Should().Be(1);
        (pb & 0x40).Should().Be(0);
        (pb & 0x80).Should().Be(0);
    }

    [Fact]
    public void Pia2PortBWrite_ThroughMemoryBus_ReachesIeeeBinding()
    {
        using var machine = CreateMachine();
        var bus = machine.IeeeBus;

        machine.Board.Bus.Write(0xE822, 0xFF);
        machine.Board.Bus.Write(0xE823, 0x04);
        machine.Board.Bus.Write(0xE840, 0x04);
        machine.Board.Bus.Write(0xE822, 0xD7);
        machine.Board.Bus.Write(0xE823, 0x34);

        bus.LastDio.Should().Be(0x28);
    }

    private static string ReadErrorFromDrive(Cpu.Pet.Devices.CbmDos.PetIeeeDiskDrive drive)
    {
        using var ms = new global::System.IO.MemoryStream();
        while (drive.DataAvailable)
        {
            if (drive.TryRead(out byte b))
                ms.WriteByte(b);
            else
                break;
        }
        return global::System.Text.Encoding.ASCII.GetString(ms.ToArray());
    }
}
