using Cpu.Board.Core;
using Cpu.Pet.System;
using Xunit;
using FluentAssertions;

namespace Cpu.Pet.Tests;

public sealed class IeeeDiagnosticTests
{
    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    [Fact]
    public void ForceBusSequence()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        using var machine = new PetMachine(profile);
        machine.MountDisk(Path.Combine(TestDisksDir, "games-1.d64"));
        machine.Reset();

        var bus = machine.IeeeBus;
        var drive = machine.DiskDrive;

        // Simulate what the KERNAL does for LISTEN 8 + SECONDARY 0:
        // 1. Assert ATN
        bus.OnATNWrite(true);
        // 2. Send LISTEN 8 byte
        bus.OnDioWrite(0x28);
        // 3. Send SECONDARY 0 byte
        bus.OnDioWrite(0x60);
        // 4. Deassert ATN → DataOut mode
        bus.OnATNWrite(false);
        // 5. Send filename "$"
        bus.OnDioWrite(0x24);
        // 6. Assert ATN → close channel
        bus.OnATNWrite(true);
        // 7. Send UNLISTEN
        bus.OnDioWrite(0x3F);
        // 8. Deassert ATN → idle
        bus.OnATNWrite(false);
        
        // Check if the drive processed the directory listing
        global::System.Console.Error.WriteLine($"Drive DataAvailable after seq: {drive.DataAvailable}");
        
        // Now TALK 8 + SECONDARY 0:
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48); // TALK 8
        bus.OnDioWrite(0x60); // SEC 0
        bus.OnATNWrite(false); // DataIn mode
        bus.Tick();
        
        global::System.Console.Error.WriteLine($"Drive DataAvailable after talk: {drive.DataAvailable}");
        
        if (drive.DataAvailable)
        {
            drive.TryRead(out byte lo);
            drive.TryRead(out byte hi);
            ushort addr = (ushort)(hi << 8 | lo);
            global::System.Console.Error.WriteLine($"Load address: ${addr:X4}");
        }
    }
}
