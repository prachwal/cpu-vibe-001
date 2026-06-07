namespace Cpu.C16.Devices;

public sealed class C16KeyboardMatrix
{
    private readonly bool[,] _keys = new bool[8, 8];
    private byte _rowMask = 0xFF;

    public void Press(int row, int col)
    {
        if ((uint)row < 8 && (uint)col < 8)
            _keys[row, col] = true;
    }

    public void Release(int row, int col)
    {
        if ((uint)row < 8 && (uint)col < 8)
            _keys[row, col] = false;
    }

    public void ReleaseAll() => Array.Clear(_keys, 0, _keys.Length);

    public void SetRowMask(byte rowMask) => _rowMask = rowMask;

    public byte ReadColumns()
    {
        byte result = 0xFF;
        for (int row = 0; row < 8; row++)
        {
            if ((_rowMask & (1 << row)) != 0)
                continue;

            for (int col = 0; col < 8; col++)
            {
                if (_keys[row, col])
                    result &= (byte)~(1 << col);
            }
        }
        return result;
    }
}
