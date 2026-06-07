using Cpu.Tui.Devices.Pia;

namespace Cpu.Apple1.Adapters;

public sealed class Apple1KeyboardAdapter
{
    private readonly Queue<byte> _pendingKeys = [];

    public byte ReadKey()
    {
        return _pendingKeys.Count == 0 ? (byte)0 : _pendingKeys.Dequeue();
    }

    public bool HasKey => _pendingKeys.Count > 0;

    public void EnqueueKey(byte ascii)
    {
        _pendingKeys.Enqueue(Normalize(ascii));
    }

    public void ClearKey()
    {
        _pendingKeys.Clear();
    }

    private static byte Normalize(byte ascii)
    {
        if (ascii >= (byte)'a' && ascii <= (byte)'z')
            return (byte)(ascii - 0x20);
        return ascii;
    }

    public void TrySendToPia(PiaDevice pia, ushort piaBase)
    {
        if (!HasKey)
            return;

        bool keyReady = (pia.Read((ushort)(piaBase + 1)) & 0x80) != 0;
        if (keyReady)
            return;

        byte keyCode = ReadKey();
        pia.Write(piaBase, (byte)(keyCode | 0x80));
        pia.SetCa1(true);
        pia.SetCa1(false);
    }
}
