namespace Cpu.Pet.Devices;

public interface IIeeeDevice
{
    int PrimaryAddress { get; }
    bool DataAvailable { get; }
    void OpenForRead(byte secondaryAddr);
    void OpenForWrite(byte secondaryAddr);
    void Close();
    void Write(byte data);
    bool TryRead(out byte data);
}
