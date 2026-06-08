namespace Cpu.Pet.Devices.CbmDos;

public sealed class PetIeeeDiskDrive : IIeeeDevice
{
    private readonly CbmDosEngine _engine;

    public int PrimaryAddress { get; }
    public CbmDosEngine Engine => _engine;
    public bool DataAvailable => _engine.DataAvailable;

    public PetIeeeDiskDrive(int deviceNumber)
    {
        PrimaryAddress = deviceNumber;
        _engine = new CbmDosEngine();
    }

    public void OpenForRead(byte secondaryAddr)
    {
        _engine.OpenChannel(secondaryAddr);
    }

    public void OpenForWrite(byte secondaryAddr)
    {
        _engine.OpenChannel(secondaryAddr);
    }

    public void Close()
    {
        _engine.CloseChannel();
    }

    public void Write(byte data)
    {
        _engine.ReceiveByte(data);
    }

    public bool TryRead(out byte data)
    {
        return _engine.TryGetByte(out data);
    }
}
