namespace Cpu.Pet.Devices;

public interface IPortBinding
{
    byte ReadPins();
    void WritePins(byte value, byte ddMask);
    bool HasInputReady { get; }
    bool IsOutputReady { get; }
}
