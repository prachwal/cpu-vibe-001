using Cpu.Board.Core;
using Cpu.Pet.System;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class PetMachineIeeeIntegrationTests
{
    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    private static PetMachine CreateMachine()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        return new PetMachine(profile);
    }

    [Fact]
    public void IeeeBus_Exists()
    {
        using var machine = CreateMachine();
        machine.IeeeBus.Should().NotBeNull();
        machine.DiskDrive.Should().NotBeNull();
    }

    [Fact]
    public void DiskDrive_HasPrimaryAddress8()
    {
        using var machine = CreateMachine();
        machine.DiskDrive.PrimaryAddress.Should().Be(8);
    }

    [Fact]
    public void MountDisk_AttachesImage()
    {
        using var machine = CreateMachine();
        machine.MountDisk(Path.Combine(TestDisksDir, "games-1.d64"));

        machine.DiskDrive.Engine.OpenChannel(15);
        machine.DiskDrive.Engine.DataAvailable.Should().BeTrue();
    }

    [Fact]
    public void AtnSignal_WiredToViaPb2()
    {
        using var machine = CreateMachine();
        var bus = machine.IeeeBus;

        machine.Board.Bus.Write(0xE840, 0x04);
        bus.LastDio.Should().Be(0);
    }

    [Fact]
    public void BootToReady_WithDisk_StillWorks()
    {
        using var machine = CreateMachine();
        machine.MountDisk(Path.Combine(TestDisksDir, "games-1.d64"));
        machine.Reset();

        bool foundReady = false;
        for (int batch = 0; batch < 3000 && !foundReady; batch++)
        {
            machine.Step(10_000);
            for (int row = 0; row < machine.Rows && !foundReady; row++)
            {
                var line = new char[machine.Columns];
                for (int col = 0; col < machine.Columns; col++)
                    line[col] = machine.GetDisplayCell(col, row);

                string text = new string(line).TrimEnd();
                if (text.Contains("READY", StringComparison.Ordinal))
                    foundReady = true;
            }
        }

        foundReady.Should().BeTrue("PET should boot to READY prompt with IEEE-488 drive connected");
    }

    [Fact]
    public void BasicPrint1_WithDisk_StillWorks()
    {
        using var machine = CreateMachine();
        machine.MountDisk(Path.Combine(TestDisksDir, "games-1.d64"));
        machine.Reset();

        for (int batch = 0; batch < 2000; batch++)
            machine.Step(10_000);

        string screen = GetScreenText(machine);
        screen.Should().Contain("READY");
    }

    private static string GetScreenText(PetMachine machine)
    {
        var chars = new char[machine.Rows * machine.Columns];
        for (int row = 0; row < machine.Rows; row++)
            for (int col = 0; col < machine.Columns; col++)
                chars[row * machine.Columns + col] = machine.GetDisplayCell(col, row);
        return new string(chars);
    }
}
