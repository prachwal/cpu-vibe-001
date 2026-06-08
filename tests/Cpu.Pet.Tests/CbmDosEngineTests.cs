using Cpu.Pet.Devices.CbmDos;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class CbmDosEngineTests
{
    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    private static CbmDosEngine CreateEngineWithDisk(string diskFile)
    {
        var engine = new CbmDosEngine();
        var img = D64Image.Load(Path.Combine(TestDisksDir, diskFile));
        engine.AttachImage(img);
        return engine;
    }

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

    [Fact]
    public void PowerOn_SendsWelcomeMessage()
    {
        var engine = new CbmDosEngine();
        var img = D64Image.Load(Path.Combine(TestDisksDir, "games-1.d64"));
        engine.AttachImage(img);

        engine.OpenChannel(15);
        engine.DataAvailable.Should().BeTrue();
        engine.TryGetByte(out byte b).Should().BeTrue();
        b.Should().Be((byte)'7');
    }

    [Fact]
    public void CommandChannel_Initialize_ReturnsOk()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(15);
        engine.ReceiveByte(0x49);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("00, OK");
    }

    [Fact]
    public void CommandChannel_UnknownCommand_ReturnsSyntaxError()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(15);
        engine.ReceiveByte(0x5A);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("30,SYNTAX ERROR");
    }

    [Fact]
    public void OpenFile_ByFilename_ReturnsLoadAddress()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(0);
        foreach (byte b in new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(0);
        engine.DataAvailable.Should().BeTrue();
        engine.TryGetByte(out byte lo).Should().BeTrue();
        engine.TryGetByte(out byte hi).Should().BeTrue();

        ushort loadAddr = (ushort)(hi << 8 | lo);
        loadAddr.Should().BeGreaterThan(0x0100);
    }

    [Fact]
    public void OpenFile_Nonexistent_ReturnsFileNotFoundError()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(0);
        foreach (byte b in new byte[] { 0x4E, 0x4F, 0x4E, 0x45, 0x58, 0x49, 0x53, 0x54 })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("62,FILE NOT FOUND");
    }

    [Fact]
    public void DirectoryListing_ReturnsBasicProgram()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(0);
        engine.ReceiveByte(0x24);
        engine.CloseChannel();

        engine.OpenChannel(0);
        string output = CollectOutput(engine);

        output[0].Should().Be('\x01');
        output[1].Should().Be('\x04');
    }

    [Fact]
    public void DirectoryListing_ContainsBasicLines()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(0);
        engine.ReceiveByte(0x24);
        engine.CloseChannel();

        engine.OpenChannel(0);
        string output = CollectOutput(engine);

        output.Should().Contain("BLOCKS FREE");
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(0);
        engine.ReceiveByte(0x24);
        engine.CloseChannel();

        engine.Reset();

        engine.OpenChannel(15);
        engine.DataAvailable.Should().BeTrue();
        CollectOutput(engine).Should().Contain("73,CBM DOS");
    }

    [Fact]
    public void NoDiskMounted_ReturnsDriveNotReady()
    {
        var engine = new CbmDosEngine();

        engine.OpenChannel(0);
        foreach (byte b in new byte[] { 0x54, 0x45, 0x53, 0x54 })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("74,DRIVE NOT READY");
    }

    [Fact]
    public void FileNotFound_ErrorChannelWorksMultipleTimes()
    {
        var engine = CreateEngineWithDisk("games-1.d64");

        engine.OpenChannel(0);
        foreach (byte b in new byte[] { 0x58, 0x58, 0x58 })
            engine.ReceiveByte(b);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("62,FILE NOT FOUND");

        engine.OpenChannel(15);
        engine.ReceiveByte(0x49);
        engine.CloseChannel();

        engine.OpenChannel(15);
        CollectOutput(engine).Should().Contain("00, OK");
    }
}
