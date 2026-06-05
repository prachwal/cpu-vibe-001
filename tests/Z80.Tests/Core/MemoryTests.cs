namespace Z80.Tests.Core;

public class MemoryTests
{
    [Fact]
    public void ReadWrite_WorksCorrectly()
    {
        var mem = new Memory();
        mem.Write(0x0000, 0x42);
        mem.Write(0xFFFF, 0xFF);

        mem.Read(0x0000).Should().Be(0x42);
        mem.Read(0xFFFF).Should().Be(0xFF);
    }

    [Fact]
    public void Load_LoadsData()
    {
        var mem = new Memory();
        byte[] data = { 0x10, 0x20, 0x30, 0x40 };
        mem.Load(0x8000, data);

        mem.Read(0x8000).Should().Be(0x10);
        mem.Read(0x8001).Should().Be(0x20);
        mem.Read(0x8002).Should().Be(0x30);
        mem.Read(0x8003).Should().Be(0x40);
    }

    [Fact]
    public void Reset_ClearsMemory()
    {
        var mem = new Memory();
        mem.Write(0x0000, 0x42);
        mem.Write(0xFFFF, 0xFF);

        mem.Reset();

        mem.Read(0x0000).Should().Be(0);
        mem.Read(0xFFFF).Should().Be(0);
    }
}
