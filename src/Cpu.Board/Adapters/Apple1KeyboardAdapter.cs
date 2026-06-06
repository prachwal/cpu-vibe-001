namespace Cpu.Board.Core.Adapters;

public sealed class Apple1KeyboardAdapter
{
    private byte _pendingKey;
    private bool _hasKey;

    public byte ReadKey()
    {
        _hasKey = false;
        return _pendingKey;
    }

    public bool HasKey => _hasKey;

    public void EnqueueKey(byte ascii)
    {
        _pendingKey = ascii;
        _hasKey = true;
    }

    public void ClearKey()
    {
        _hasKey = false;
    }
}
