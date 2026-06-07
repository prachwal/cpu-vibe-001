namespace Cpu.Apple1.Adapters;

public sealed class Apple1DisplayAdapter
{
    private readonly char[,] _buffer;
    private int _cursorX;
    private int _cursorY;

    public int Cols { get; }
    public int Rows { get; }

    public Apple1DisplayAdapter(int cols = 40, int rows = 24)
    {
        Cols = cols;
        Rows = rows;
        _buffer = new char[rows, cols];
        Clear();
    }

    public void Clear()
    {
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                _buffer[y, x] = ' ';
        _cursorX = 0;
        _cursorY = 0;
    }

    public char GetChar(int col, int row)
    {
        if (col < 0 || col >= Cols || row < 0 || row >= Rows) return ' ';
        return _buffer[row, col];
    }

    public void Write(byte value)
    {
        byte ch = (byte)(value & 0x7F);
        if (ch == 0x0D || ch == 0x0A)
            NewLine();
        else
            WriteChar(ch);
    }

    private void WriteChar(byte ch)
    {
        if (ch < 0x20) return;
        char displayChar = ch == 0x23 || ch == 0xA3 ? '.' : (char)ch;
        if (_cursorX >= Cols)
        {
            _cursorX = 0;
            _cursorY++;
        }
        if (_cursorY >= Rows)
        {
            ScrollUp();
            _cursorY = Rows - 1;
        }
        _buffer[_cursorY, _cursorX] = displayChar;
        _cursorX++;
    }

    private void NewLine()
    {
        _cursorX = 0;
        _cursorY++;
        if (_cursorY >= Rows)
        {
            ScrollUp();
            _cursorY = Rows - 1;
        }
    }

    private void ScrollUp()
    {
        for (int y = 1; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                _buffer[y - 1, x] = _buffer[y, x];
        for (int x = 0; x < Cols; x++)
            _buffer[Rows - 1, x] = ' ';
    }
}
