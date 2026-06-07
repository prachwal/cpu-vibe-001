namespace Cpu.Vic20.Devices;

public sealed class Vic20KeyboardMatrix
{
    private readonly bool[,] _keys = new bool[8, 8];
    private int _currentRow = -1;

    public void Clear() => Array.Clear(_keys, 0, _keys.Length);

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

    public void ReleaseAll() => Clear();

    public void SetRowSelect(byte rowMask)
    {
        for (int row = 0; row < 8; row++)
        {
            if ((rowMask & (1 << row)) == 0)
            {
                _currentRow = row;
                return;
            }
        }
        _currentRow = -1;
    }

    public byte ReadColumns()
    {
        if (_currentRow < 0 || _currentRow >= 8)
            return 0xFF;

        byte result = 0xFF;
        for (int col = 0; col < 8; col++)
        {
            if (_keys[_currentRow, col])
                result &= (byte)~(1 << col);
        }
        return result;
    }
}
