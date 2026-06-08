using Cpu.Pet.Devices.CbmDos;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class PetIeeeDiskDriveTests
{
    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    private static PetIeeeDiskDrive CreateDrive(string diskFile)
    {
        var drive = new PetIeeeDiskDrive(8);
        drive.Engine.AttachImage(D64Image.Load(Path.Combine(TestDisksDir, diskFile)));
        return drive;
    }

    [Fact]
    public void PrimaryAddress_Is8()
    {
        var drive = new PetIeeeDiskDrive(8);
        drive.PrimaryAddress.Should().Be(8);
    }

    [Fact]
    public void PowerOn_HasWelcomeOnChannel15()
    {
        var drive = CreateDrive("games-1.d64");

        drive.OpenForWrite(15);
        drive.DataAvailable.Should().BeTrue();

        drive.TryRead(out byte b).Should().BeTrue();
        b.Should().Be((byte)'7');
    }

    [Fact]
    public void LoadHello_ReturnsBasicProgram()
    {
        var drive = CreateDrive("games-1.d64");

        drive.OpenForRead(0);
        foreach (byte b in new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F })
            drive.Write(b);
        drive.Close();

        drive.OpenForWrite(0);
        drive.DataAvailable.Should().BeTrue();

        drive.TryRead(out byte lo).Should().BeTrue();
        drive.TryRead(out byte hi).Should().BeTrue();
        ushort loadAddr = (ushort)(hi << 8 | lo);
        loadAddr.Should().BeGreaterThan(0x0100);
    }

    [Fact]
    public void InitializeCommand_ReturnsOk()
    {
        var drive = CreateDrive("games-1.d64");

        drive.OpenForRead(15);
        drive.Write(0x49);
        drive.Close();

        drive.OpenForWrite(15);
        string error = ReadAll(drive);
        error.Should().Contain("00, OK");
    }

    [Fact]
    public void LoadDirectory_ReturnsBasicListing()
    {
        var drive = CreateDrive("games-1.d64");

        drive.OpenForRead(0);
        drive.Write(0x24);
        drive.Close();

        drive.OpenForWrite(0);
        var data = ReadAllBytes(drive);
        data.Length.Should().BeGreaterThan(20);
        data[0].Should().Be(0x01);
        data[1].Should().Be(0x04);
    }

    [Fact]
    public void LoadDirectory_ContainsBlocksFree()
    {
        var drive = CreateDrive("games-1.d64");

        drive.OpenForRead(0);
        drive.Write(0x24);
        drive.Close();

        drive.OpenForWrite(0);
        string listing = ReadAll(drive);
        listing.Should().Contain("BLOCKS FREE");
    }

    [Fact]
    public void FileNotFound_ReturnsErrorOnChannel15()
    {
        var drive = CreateDrive("games-1.d64");

        drive.OpenForRead(0);
        foreach (byte b in new byte[] { 0x58, 0x58, 0x58 })
            drive.Write(b);
        drive.Close();

        drive.OpenForWrite(15);
        string error = ReadAll(drive);
        error.Should().Contain("62,FILE NOT FOUND");
    }

    [Fact]
    public void NoDisk_ReturnsDriveNotReady()
    {
        var drive = new PetIeeeDiskDrive(8);

        drive.OpenForRead(0);
        drive.Write(0x48);
        drive.Close();

        drive.OpenForWrite(15);
        string error = ReadAll(drive);
        error.Should().Contain("74,DRIVE NOT READY");
    }

    private static string ReadAll(PetIeeeDiskDrive drive)
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

    private static byte[] ReadAllBytes(PetIeeeDiskDrive drive)
    {
        using var ms = new global::System.IO.MemoryStream();
        while (drive.DataAvailable)
        {
            if (drive.TryRead(out byte b))
                ms.WriteByte(b);
            else
                break;
        }
        return ms.ToArray();
    }
}
