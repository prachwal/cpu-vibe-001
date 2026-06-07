namespace Cpu.Pet.Devices;

public sealed class PetKeyboardMatrix
{
    private readonly bool[,] _matrix = new bool[8, 8];
    private int _currentRow = -1;

    public void Clear()
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                _matrix[r, c] = false;
    }

    public void PressKey(int row, int col)
    {
        if ((uint)row < 8 && (uint)col < 8)
            _matrix[row, col] = true;
    }

    public void ReleaseKey(int row, int col)
    {
        if ((uint)row < 8 && (uint)col < 8)
            _matrix[row, col] = false;
    }

    public void ReleaseAll() => Clear();

    public bool AnyKeyPressed
    {
        get
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (_matrix[r, c])
                        return true;
            return false;
        }
    }

    public int CurrentRow
    {
        get => _currentRow;
        set => _currentRow = value >= 0 && value < 8 ? value : -1;
    }

    public static int DecodeRowSelect(byte value)
    {
        for (int row = 0; row < 8; row++)
        {
            if ((value & (1 << row)) == 0)
                return row;
        }
        return -1;
    }

    public byte ReadColumns()
    {
        if (_currentRow < 0 || _currentRow >= 8)
            return 0xFF;

        byte result = 0xFF;
        for (int col = 0; col < 8; col++)
        {
            if (_matrix[_currentRow, col])
                result &= (byte)~(1 << col);
        }
        return result;
    }
}
