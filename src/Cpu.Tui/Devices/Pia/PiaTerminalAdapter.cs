using Cpu.Tui;

namespace Cpu.Tui.Devices.Pia;

/// <summary>
/// Terminal adapter that fills ScreenBuffer based on PIA port writes.
/// Port A: data byte (ASCII or escape sequence byte).
/// Port B: control signals (bit 3 = strobe, rising edge latches data).
/// </summary>
public class PiaTerminalAdapter : IPiaTerminal
{
    private readonly ScreenBuffer _screen;
    private readonly PiaDevice _pia;

    private int _cursorX;
    private int _cursorY;
    private bool _cursorVisible = true;
    private int _savedX;
    private int _savedY;

    private enum EscState { None, GotEsc, GotCSI, GotCSIParam, GotCSIQuestion }
    private EscState _escState = EscState.None;
    private byte[] _escParams = new byte[16];
    private int _escParamLen;

    private ConsoleColor _fg = ConsoleColor.Gray;
    private ConsoleColor _bg = ConsoleColor.Black;

    private int _scrollTop = 0;
    private int _scrollBottom = ScreenBuffer.Height - 1;

    public PiaTerminalAdapter(PiaDevice pia, ScreenBuffer screen)
    {
        _pia = pia ?? throw new ArgumentNullException(nameof(pia));
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        _pia.AttachTerminal(this);
    }

    public int CursorX => _cursorX;
    public int CursorY => _cursorY;
    public bool CursorVisible => _cursorVisible;

    public void Reset()
    {
        _cursorX = 0;
        _cursorY = 0;
        _cursorVisible = true;
        _escState = EscState.None;
        _escParamLen = 0;
        _fg = ConsoleColor.Gray;
        _bg = ConsoleColor.Black;
        _scrollTop = 0;
        _scrollBottom = ScreenBuffer.Height - 1;
        _screen.Reset();
    }

    public void OnPortAWrite(byte newValue, byte oldValue) => ProcessByte(newValue);

    public void OnPortBWrite(byte newValue, byte oldValue) { }

    public void OnIrq(IrqSource source) { }

    private void ProcessByte(byte b)
    {
        switch (_escState)
        {
            case EscState.None:
                ProcessNormalByte(b);
                break;
            case EscState.GotEsc:
                ProcessEscByte(b);
                break;
            case EscState.GotCSI:
                ProcessCSIByte(b);
                break;
            case EscState.GotCSIParam:
                ProcessCSIParamByte(b);
                break;
            case EscState.GotCSIQuestion:
                ProcessCSIQuestionByte(b);
                break;
        }
    }

    private void ProcessNormalByte(byte b)
    {
        switch (b)
        {
            case 0x07: break;
            case 0x08: if (_cursorX > 0) _cursorX--; break;
            case 0x09:
                _cursorX = ((_cursorX / 8) + 1) * 8;
                if (_cursorX >= ScreenBuffer.Width) _cursorX = ScreenBuffer.Width - 1;
                break;
            case 0x0A: LineFeed(); break;
            case 0x0D: _cursorX = 0; break;
            case 0x1B: _escState = EscState.GotEsc; _escParamLen = 0; break;
            default:
                if (b >= 0x20 && b < 0x7F) PutChar((char)b);
                break;
        }
    }

    private void ProcessEscByte(byte b)
    {
        switch (b)
        {
            case 0x5B: _escState = EscState.GotCSI; _escParamLen = 0; break;
            case 0x37: _savedX = _cursorX; _savedY = _cursorY; _escState = EscState.None; break;
            case 0x38: _cursorX = _savedX; _cursorY = _savedY; _escState = EscState.None; break;
            case 0x44: LineFeed(); _escState = EscState.None; break;
            case 0x45: _cursorX = 0; LineFeed(); _escState = EscState.None; break;
            case 0x48: _cursorX = 0; _cursorY = 0; _escState = EscState.None; break;
            case 0x4A: ClearToEndOfScreen(); _escState = EscState.None; break;
            case 0x4B: ClearToEndOfLine(); _escState = EscState.None; break;
            default: _escState = EscState.None; break;
        }
    }

    private void ProcessCSIByte(byte b)
    {
        if (b == 0x3F) { _escState = EscState.GotCSIQuestion; _escParamLen = 0; return; }
        if (b >= 0x30 && b <= 0x3F) { if (_escParamLen < 16) _escParams[_escParamLen++] = b; _escState = EscState.GotCSIParam; return; }
        ExecuteCSI(b);
        _escState = EscState.None;
    }

    private void ProcessCSIParamByte(byte b)
    {
        if (b >= 0x30 && b <= 0x3F) { if (_escParamLen < 16) _escParams[_escParamLen++] = b; return; }
        ExecuteCSI(b);
        _escState = EscState.None;
    }

    private void ProcessCSIQuestionByte(byte b)
    {
        if (b >= 0x30 && b <= 0x3F) { if (_escParamLen < 16) _escParams[_escParamLen++] = b; return; }
        if (_escParamLen > 0)
        {
            int mode = ParseParamInt(0, _escParamLen);
            if (b == 0x68 && mode == 25) _cursorVisible = true;
            if (b == 0x6C && mode == 25) _cursorVisible = false;
        }
        _escState = EscState.None;
    }

    private void ExecuteCSI(byte command)
    {
        int p0 = _escParamLen > 0 ? ParseParamInt(0, _escParamLen) : 0;

        int p1 = 0;
        int sep = FindSeparator(0, _escParamLen);
        if (sep >= 0 && sep + 1 < _escParamLen)
            p1 = ParseParamInt(sep + 1, _escParamLen);

        int p2 = 0;
        int sep2 = FindSeparator(sep + 1, _escParamLen);
        if (sep2 >= 0 && sep2 + 1 < _escParamLen)
            p2 = ParseParamInt(sep2 + 1, _escParamLen);

        switch (command)
        {
            case 0x41: // A - Up
                _cursorY = Math.Max(_scrollTop, _cursorY - (p0 > 0 ? p0 : 1));
                break;
            case 0x42: // B - Down
                _cursorY = Math.Min(_scrollBottom, _cursorY + (p0 > 0 ? p0 : 1));
                break;
            case 0x43: // C - Forward
                _cursorX = Math.Min(ScreenBuffer.Width - 1, _cursorX + (p0 > 0 ? p0 : 1));
                break;
            case 0x44: // D - Back
                _cursorX = Math.Max(0, _cursorX - (p0 > 0 ? p0 : 1));
                break;
            case 0x48: // H/f - Position
                int row = p0 > 0 ? p0 : 1;
                int col = p1 > 0 ? p1 : 1;
                _cursorX = Math.Clamp(col - 1, 0, ScreenBuffer.Width - 1);
                _cursorY = Math.Clamp(row - 1, 0, ScreenBuffer.Height - 1);
                break;
            case 0x4A: // J - Erase display
                switch (p0)
                {
                    case 0: ClearToEndOfScreen(); break;
                    case 1: ClearToStartOfScreen(); break;
                    case 2: FillScreen(' '); break;
                    case 3: FillScreen(' '); _cursorX = 0; _cursorY = 0; break;
                }
                break;
            case 0x4B: // K - Erase line
                switch (p0)
                {
                    case 0: ClearToEndOfLine(); break;
                    case 1: ClearToStartOfLine(); break;
                    case 2: FillRow(_cursorY, ' '); break;
                }
                break;
            case 0x4D: // M - Delete lines
                ScrollRegionUp(_cursorY, _scrollBottom, p0 > 0 ? p0 : 1);
                break;
            case 0x50: // P - Delete chars
                DeleteCharsAt(_cursorX, _cursorY, p0 > 0 ? p0 : 1);
                break;
            case 0x6D: // m - SGR
                ExecuteSgr(p0, p1, p2);
                break;
            case 0x72: // r - Set scroll region
                int t = p0 > 0 ? p0 : 1;
                int b2 = p1 > 0 ? p1 : ScreenBuffer.Height;
                _scrollTop = Math.Clamp(t - 1, 0, ScreenBuffer.Height - 1);
                _scrollBottom = Math.Clamp(b2 - 1, 0, ScreenBuffer.Height - 1);
                if (_scrollTop >= _scrollBottom) { _scrollTop = 0; _scrollBottom = ScreenBuffer.Height - 1; }
                break;
            case 0x73: _savedX = _cursorX; _savedY = _cursorY; break;
            case 0x75: _cursorX = _savedX; _cursorY = _savedY; break;
        }
    }

    private int ParseParamInt(int start, int end)
    {
        int result = 0;
        bool hasDigit = false;
        for (int i = start; i < end; i++)
        {
            byte b = _escParams[i];
            if (b >= 0x30 && b <= 0x39)
            {
                result = result * 10 + (b - 0x30);
                hasDigit = true;
            }
            else break;
        }
        return hasDigit ? result : 0;
    }

    private int FindSeparator(int start, int end)
    {
        for (int i = start; i < end; i++)
            if (_escParams[i] == 0x3B) return i; // ';'
        return -1;
    }

    private void ExecuteSgr(int p0, int p1, int p2)
    {
        if (p0 == 0 && p1 == 0 && p2 == 0) { _fg = ConsoleColor.Gray; _bg = ConsoleColor.Black; return; }
        ApplySgrSingle(p0);
        if (p1 != 0) ApplySgrSingle(p1);
        if (p2 != 0) ApplySgrSingle(p2);
    }

    private void ApplySgrSingle(int code)
    {
        switch (code)
        {
            case 0: _fg = ConsoleColor.Gray; _bg = ConsoleColor.Black; break;
            case 7: (_fg, _bg) = (_bg, _fg); break;
            case 30: _fg = ConsoleColor.Black; break;
            case 31: _fg = ConsoleColor.DarkRed; break;
            case 32: _fg = ConsoleColor.DarkGreen; break;
            case 33: _fg = ConsoleColor.DarkYellow; break;
            case 34: _fg = ConsoleColor.DarkBlue; break;
            case 35: _fg = ConsoleColor.DarkMagenta; break;
            case 36: _fg = ConsoleColor.DarkCyan; break;
            case 37: _fg = ConsoleColor.Gray; break;
            case 39: _fg = ConsoleColor.Gray; break;
            case 40: _bg = ConsoleColor.Black; break;
            case 41: _bg = ConsoleColor.DarkRed; break;
            case 42: _bg = ConsoleColor.DarkGreen; break;
            case 43: _bg = ConsoleColor.DarkYellow; break;
            case 44: _bg = ConsoleColor.DarkBlue; break;
            case 45: _bg = ConsoleColor.DarkMagenta; break;
            case 46: _bg = ConsoleColor.DarkCyan; break;
            case 47: _bg = ConsoleColor.Gray; break;
            case 49: _bg = ConsoleColor.Black; break;
            case 90: _fg = ConsoleColor.DarkGray; break;
            case 91: _fg = ConsoleColor.Red; break;
            case 92: _fg = ConsoleColor.Green; break;
            case 93: _fg = ConsoleColor.Yellow; break;
            case 94: _fg = ConsoleColor.Blue; break;
            case 95: _fg = ConsoleColor.Magenta; break;
            case 96: _fg = ConsoleColor.Cyan; break;
            case 97: _fg = ConsoleColor.White; break;
            case 100: _bg = ConsoleColor.DarkGray; break;
            case 101: _bg = ConsoleColor.Red; break;
            case 102: _bg = ConsoleColor.Green; break;
            case 103: _bg = ConsoleColor.Yellow; break;
            case 104: _bg = ConsoleColor.Blue; break;
            case 105: _bg = ConsoleColor.Magenta; break;
            case 106: _bg = ConsoleColor.Cyan; break;
            case 107: _bg = ConsoleColor.White; break;
        }
    }

    private void PutChar(char ch)
    {
        if (_cursorX >= ScreenBuffer.Width) { _cursorX = 0; LineFeed(); }
        _screen.SetCell(_cursorX, _cursorY, ch, _fg, _bg);
        _cursorX++;
    }

    private void LineFeed()
    {
        if (_cursorY >= _scrollBottom)
            ScrollRegionUp(_scrollTop, _scrollBottom, 1);
        else
            _cursorY = Math.Min(ScreenBuffer.Height - 1, _cursorY + 1);
    }

    private void ScrollRegionUp(int top, int bottom, int count)
    {
        if (count <= 0 || top > bottom) return;
        int lines = Math.Min(count, bottom - top + 1);

        for (int y = top; y <= bottom - lines; y++)
            _screen.CopyRow(y + lines, y);

        byte attr = (byte)(ColorToIndex(_fg) | (ColorToIndex(_bg) << 4));
        _screen.FillRowFast(bottom, (byte)' ', attr);
    }

    private void FillRow(int row, char ch)
    {
        if (row < 0 || row >= ScreenBuffer.Height) return;
        byte attr = (byte)(ColorToIndex(_fg) | (ColorToIndex(_bg) << 4));
        _screen.FillRowFast(row, (byte)ch, attr);
    }

    private void FillScreen(char ch)
    {
        byte attr = (byte)(ColorToIndex(_fg) | (ColorToIndex(_bg) << 4));
        for (int y = 0; y < ScreenBuffer.Height; y++)
            _screen.FillRowFast(y, (byte)ch, attr);
    }

    private void ClearToEndOfScreen()
    {
        ClearToEndOfLine();
        for (int y = _cursorY + 1; y < ScreenBuffer.Height; y++)
            FillRow(y, ' ');
    }

    private void ClearToStartOfScreen()
    {
        for (int y = 0; y < _cursorY; y++)
            FillRow(y, ' ');
        FillRow(_cursorY, ' ');
    }

    private void ClearToEndOfLine()
    {
        for (int x = _cursorX; x < ScreenBuffer.Width; x++)
            _screen.SetCell(x, _cursorY, ' ', _fg, _bg);
    }

    private void ClearToStartOfLine()
    {
        for (int x = 0; x <= _cursorX; x++)
            _screen.SetCell(x, _cursorY, ' ', _fg, _bg);
    }

    private void DeleteCharsAt(int x, int y, int count)
    {
        if (count <= 0 || x >= ScreenBuffer.Width) return;
        int remaining = ScreenBuffer.Width - x - count;
        if (remaining <= 0)
        {
            FillRow(y, ' ');
            return;
        }

        // Use direct _data[] access instead of temp arrays
        byte attr = (byte)(ColorToIndex(_fg) | (ColorToIndex(_bg) << 4));
        int srcBase = ScreenBuffer.CharBase + y * ScreenBuffer.Width;
        int srcAttrBase = ScreenBuffer.AttrBase + y * ScreenBuffer.Width;

        // Read source data
        Span<byte> srcChars = stackalloc byte[remaining];
        Span<byte> srcAttrs = stackalloc byte[remaining];
        for (int i = 0; i < remaining; i++)
        {
            byte ch = _screen.GetRawChar(x + count + i, y);
            srcChars[i] = ch == 0 ? (byte)' ' : ch;
            srcAttrs[i] = _screen.GetAttrRaw(x + count + i, y);
        }

        FillRow(y, ' ');

        // Write shifted data
        for (int i = 0; i < remaining; i++)
            _screen.SetCellRaw(x + i, y, srcChars[i], srcAttrs[i]);
    }

    public void WriteString(string text) { foreach (char c in text) ProcessByte((byte)c); }

    public void WriteEsc(string sequence)
    {
        ProcessByte(0x1B);
        foreach (char c in sequence) ProcessByte((byte)c);
    }

    private static int ColorToIndex(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => 0,
        ConsoleColor.DarkBlue => 1,
        ConsoleColor.DarkGreen => 2,
        ConsoleColor.DarkCyan => 3,
        ConsoleColor.DarkRed => 4,
        ConsoleColor.DarkMagenta => 5,
        ConsoleColor.DarkYellow => 6,
        ConsoleColor.Gray => 7,
        ConsoleColor.DarkGray => 8,
        ConsoleColor.Blue => 9,
        ConsoleColor.Green => 10,
        ConsoleColor.Cyan => 11,
        ConsoleColor.Red => 12,
        ConsoleColor.Magenta => 13,
        ConsoleColor.Yellow => 14,
        ConsoleColor.White => 15,
        _ => 7
    };
}
