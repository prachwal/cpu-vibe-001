using Cpu.Pet.Devices;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public sealed class PetIeeeBusTests
{
    [Fact]
    public void Idle_InitialState()
    {
        var bus = new PetIeeeBus();
        bus.LastDio.Should().Be(0);
    }

    [Fact]
    public void ListenAndDataOut_SendsBytesToDevice()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x45);
        bus.OnDioWrite(0x4C);

        dev.ReceivedBytes.Should().Equal(0x48, 0x45, 0x4C);
        dev.LastReadSec.Should().Be(0);
    }

    [Fact]
    public void TalkAndDataIn_ReadsBytesFromDevice()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x01, 0x02, 0x03);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        bus.OnDioRead().Should().Be(0x01);
        bus.Tick();
        bus.OnDioRead().Should().Be(0x02);
        bus.Tick();
        bus.OnDioRead().Should().Be(0x03);
        dev.LastWriteSec.Should().Be(0);
    }

    [Fact]
    public void DataIn_NoMoreData_Returns0xFF()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.OnDioRead().Should().Be(0xFF);
    }

    [Fact]
    public void Unlisten_ClearsListenerAndCloses()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x48);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        dev.IsOpen.Should().BeFalse();
        dev.ReceivedBytes.Should().Equal(0x48);
    }

    [Fact]
    public void Untalk_ClearsTalkerAndCloses()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x99);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        bus.OnDioRead().Should().Be(0x99);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x5F);
        bus.OnATNWrite(false);

        dev.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void SecondDevice_AddressedCorrectly()
    {
        var bus = new PetIeeeBus();
        var dev8 = new MockDevice(8);
        var dev9 = new MockDevice(9);
        bus.AttachDevice(dev8);
        bus.AttachDevice(dev9);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x29);
        bus.OnDioWrite(0x61);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0xFF);

        dev8.ReceivedBytes.Should().BeEmpty();
        dev9.ReceivedBytes.Should().Equal(0xFF);
        dev9.LastReadSec.Should().Be(1);
    }

    [Fact]
    public void ViaPortBInput_AfterCommandHandshake()
    {
        var bus = new PetIeeeBus();
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);

        byte pb = bus.GetViaPortBInput();
        (pb & 0x01).Should().Be(0x01);
        (pb & 0x40).Should().Be(0x40);
        (pb & 0x80).Should().Be(0x80);
    }

    [Fact]
    public void ViaPortBInput_AfterReset()
    {
        var bus = new PetIeeeBus();
        bus.Reset();

        byte pb = bus.GetViaPortBInput();
        (pb & 0x01).Should().Be(0);
        (pb & 0x40).Should().Be(0);
        (pb & 0x80).Should().Be(0x80);
    }

    [Fact]
    public void ListenThenTalk_SwitchesDirection()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x99);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x48);
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        dev.CloseCount.Should().Be(2);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        bus.OnDioRead().Should().Be(0x99);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x41);

        bus.Reset();

        bus.LastDio.Should().Be(0);
        dev.ReceivedBytes.Should().Equal(0x41);
        dev.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void MultipleListen_UpdatesAddress()
    {
        var bus = new PetIeeeBus();
        var dev9 = new MockDevice(9);
        bus.AttachDevice(dev9);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x29);
        bus.OnDioWrite(0x62);
        bus.OnATNWrite(false);

        bus.OnDioWrite(0x99);

        dev9.ReceivedBytes.Should().Equal(0x99);
        dev9.LastReadSec.Should().Be(2);
    }

    [Fact]
    public void PortBBinding_ReadPins_ReturnsFFWhenBusIdle()
    {
        var bus = new PetIeeeBus();
        var binding = new PetIeeePortBBinding(bus);

        byte pins = binding.ReadPins();
        pins.Should().Be(0xFF, "bus idle → all high");
    }

    [Fact]
    public void PortBBinding_ReadPins_ReturnsBusDataInDataInMode()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x55);
        bus.AttachDevice(dev);
        var binding = new PetIeeePortBBinding(bus);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.Tick();

        byte pins = binding.ReadPins();
        pins.Should().Be(0x55);
    }

    [Fact]
    public void PortBBinding_WritePins_ForwardsToBusWhenDdrOutput()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);
        var binding = new PetIeeePortBBinding(bus);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        binding.WritePins(0x42, 0xFF);

        dev.ReceivedBytes.Should().Equal(0x42);
    }

    [Fact]
    public void PortBBinding_WritePins_IgnoredWhenNotAllOutput()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);
        var binding = new PetIeeePortBBinding(bus);

        binding.WritePins(0x42, 0x00);

        dev.ReceivedBytes.Should().BeEmpty();
    }



    private sealed class MockDevice : IIeeeDevice
    {
        private readonly Queue<byte> _dataToSend = new();

        public int PrimaryAddress { get; }
        public List<byte> ReceivedBytes { get; } = [];
        public bool IsOpen { get; private set; }
        public int CloseCount { get; private set; }
        public byte LastReadSec { get; private set; }
        public byte LastWriteSec { get; private set; }
        public bool DataAvailable => _dataToSend.Count > 0;

        public MockDevice(int primaryAddr) => PrimaryAddress = primaryAddr;

        public void QueueBytes(params byte[] data)
        {
            foreach (byte b in data)
                _dataToSend.Enqueue(b);
        }

        public void OpenForRead(byte secondaryAddr)
        {
            IsOpen = true;
            LastReadSec = secondaryAddr;
        }

        public void OpenForWrite(byte secondaryAddr)
        {
            IsOpen = true;
            LastWriteSec = secondaryAddr;
        }

        public void Close()
        {
            IsOpen = false;
            CloseCount++;
        }

        public void Write(byte data) => ReceivedBytes.Add(data);

        public bool TryRead(out byte data)
        {
            if (_dataToSend.TryDequeue(out data))
                return true;
            data = 0;
            return false;
        }
    }
}
