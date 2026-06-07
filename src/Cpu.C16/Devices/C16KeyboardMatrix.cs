namespace Cpu.C16.Devices;

public sealed class C16KeyboardMatrix
{
    private readonly bool[,] _keys = new bool[8, 8];

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

    private int _currentRow;

    public byte ReadColumnsForRow(byte rowMask)
    {
        int selectedRow = -1;
        for (int r = 0; r < 8; r++)
        {
            if ((rowMask & (1 << r)) == 0)
            {
                selectedRow = r;
                break;
            }
        }

        if (selectedRow < 0)
            return 0xFF;

        byte result = 0xFF;
        for (int c = 0; c < 8; c++)
        {
            if (_keys[selectedRow, c])
                result &= (byte)~(1 << c);
        }
        return result;
    }

    public void SetRowMask(byte mask)
    {
        _currentRow = -1;
        for (int r = 0; r < 8; r++)
            if ((mask & (1 << r)) == 0) { _currentRow = r; break; }
    }

    public byte ReadColumns()
    {
        if (_currentRow < 0) return 0xFF;
        byte result = 0xFF;
        for (int c = 0; c < 8; c++)
            if (_keys[_currentRow, c])
                result &= (byte)~(1 << c);
        return result;
    }
}
