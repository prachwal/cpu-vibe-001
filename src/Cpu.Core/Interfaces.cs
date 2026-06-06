namespace CpuBase;

/// <summary>
/// Shared memory interface for all CPU emulators.
/// </summary>
public interface IMemory
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
    void Load(ushort address, byte[] data);
    void Reset();
}

/// <summary>
/// Shared CPU interface for all CPU emulators.
/// Stack operations are CPU-specific (Z80=16bit, 6502=8bit) so not in this interface.
/// </summary>
public interface ICpu
{
    long Cycles { get; set; }
    IMemory Memory { get; }
    void Step();
    void Reset();
}
