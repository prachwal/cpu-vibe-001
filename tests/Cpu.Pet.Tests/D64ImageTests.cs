using Cpu.Pet.Devices.CbmDos;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class D64ImageTests
{
    private static string TestDisksDir =>
        Path.Combine(        AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    [Fact]
    public void LoadGames1_DiskName()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        img.DiskName.Should().Be("GAMES-1");
        img.DiskId.Should().Be("AE");
        img.DosType.Should().Be("2A");
    }

    [Fact]
    public void LoadGames1_DirectoryHasFiles()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        var dir = img.ReadDirectory();
        dir.Should().HaveCountGreaterThan(30);
    }

    [Fact]
    public void LoadGames1_ContainsHello()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        var dir = img.ReadDirectory();
        dir.Should().Contain(e => e.Filename.Contains("ACE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoadGames1_ContainsBattleship()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        var dir = img.ReadDirectory();
        dir.Should().Contain(e => e.Filename.Contains("BATTLESHIP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoadUtils_DiskName()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "utils.d64"));
        img.DiskName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LoadUtils_DirectoryHasFiles()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "utils.d64"));
        var dir = img.ReadDirectory();
        dir.Should().HaveCountGreaterThan(20);
    }

    [Fact]
    public void ReadFile_ReturnsData()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        var dir = img.ReadDirectory();

        var prg = dir.FirstOrDefault(e => e.Type == FileType.Prg);
        prg.Should().NotBe(default, "at least one PRG should exist");

        byte[] data = img.ReadFile(prg);
        data.Length.Should().BeGreaterThan(2);
        data[0].Should().BeGreaterThan(0, "PRG load address low byte should be non-zero");
    }

    [Fact]
    public void TrackSectorToOffset_StandardPositions()
    {
        D64Image.TrackSectorToOffset(18, 0).Should().Be(0x16500);
        D64Image.TrackSectorToOffset(1, 0).Should().Be(0);
        D64Image.TrackSectorToOffset(18, 1).Should().Be(0x16500 + 256);
        D64Image.TrackSectorToOffset(35, 16).Should().Be(174592);
        (D64Image.TrackSectorToOffset(35, 16) + 255).Should().Be(174847);
    }

    [Fact]
    public void CreateEmpty_HasCorrectSize()
    {
        var data = D64Image.CreateEmpty();
        data.Length.Should().Be(174848);
    }
}
