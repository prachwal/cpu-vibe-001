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
}
