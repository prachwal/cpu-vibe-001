using Cpu.Pet.Devices.CbmDos;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class CbmDosSaveTests
{
    private static readonly byte[] TestProgram =
    [
        0x01, 0x10,
        0x0E, 0x10, 0x0A, 0x00, 0x99, 0x20, 0x22, 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x22, 0x00, 0x00, 0x00
    ];

    [Fact]
    public void SaveProgram_WritesToD64()
    {
        var image = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        var dirList = image.ReadDirectory();
        dirList.Should().NotBeEmpty("disk should have files");

        var engine = new CbmDosEngine();
        engine.AttachImage(image);

        engine.OpenChannel(1);
        foreach (byte b in new byte[] { 0x54, 0x45, 0x53, 0x54, 0x50, 0x52, 0x47 })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(1);
        foreach (byte b in TestProgram)
            engine.ReceiveByte(b);
        engine.CloseChannel();

        var updatedDir = image.ReadDirectory();
        updatedDir.Should().Contain(e => e.Filename.Contains("TESTPRG"));
    }

    [Fact]
    public void SaveThenLoad_ReturnsSameData()
    {
        var image = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));

        var engine = new CbmDosEngine();
        engine.AttachImage(image);

        engine.OpenChannel(1);
        foreach (byte b in new byte[] { 0x53, 0x41, 0x56, 0x45, 0x54, 0x45, 0x53, 0x54 })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(1);
        foreach (byte b in TestProgram)
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(0);
        foreach (byte b in new byte[] { 0x53, 0x41, 0x56, 0x45, 0x54, 0x45, 0x53, 0x54 })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(0);
        var loadedData = new global::System.Collections.Generic.List<byte>();
        while (engine.DataAvailable)
        {
            if (engine.TryGetByte(out byte b))
                loadedData.Add(b);
            else
                break;
        }

        loadedData.Count.Should().Be(TestProgram.Length);
        for (int i = 0; i < TestProgram.Length; i++)
            loadedData[i].Should().Be(TestProgram[i]);
    }

    [Fact]
    public void SaveWithoutFilename_ReturnsSyntaxError()
    {
        var image = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        var engine = new CbmDosEngine();
        engine.AttachImage(image);

        engine.OpenChannel(1);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("30,SYNTAX ERROR");
    }

    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    private static string CollectOutput(CbmDosEngine engine)
    {
        using var ms = new global::System.IO.MemoryStream();
        while (engine.DataAvailable)
        {
            if (engine.TryGetByte(out byte b))
                ms.WriteByte(b);
            else
                break;
        }
        return global::System.Text.Encoding.ASCII.GetString(ms.ToArray());
    }
}
