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
    private readonly List<byte> _escParams = new();

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
        _escParams.Clear();
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
            case 0x1B: _escState = EscState.GotEsc; _escParams.Clear(); break;
            default:
                if (b >= 0x20 && b < 0x7F) PutChar((char)b);
                break;
        }
    }

    private void ProcessEscByte(byte b)
    {
        switch (b)
        {
            case 0x5B: _escState = EscState.GotCSI; _escParams.Clear(); break;
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
        if (b == 0x3F) { _escState = EscState.GotCSIQuestion; _escParams.Clear(); return; }
        if (b >= 0x30 && b <= 0x3F) { _escParams.Add(b); _escState = EscState.GotCSIParam; return; }
        ExecuteCSI(b);
        _escState = EscState.None;
    }

    private void ProcessCSIParamByte(byte b)
    {
        if (b >= 0x30 && b <= 0x3F) { _escParams.Add(b); return; }
        ExecuteCSI(b);
        _escState = EscState.None;
    }

    private void ProcessCSIQuestionByte(byte b)
    {
        if (b >= 0x30 && b <= 0x3F) { _escParams.Add(b); return; }
        if (_escParams.Count > 0 && int.TryParse(System.Text.Encoding.ASCII.GetString(_escParams.ToArray()), out int mode))
        {
            if (b == 0x68 && mode == 25) _cursorVisible = true;
            if (b == 0x6C && mode == 25) _cursorVisible = false;
        }
        _escState = EscState.None;
    }

    private void ExecuteCSI(byte command)
    {
        string paramStr = _escParams.Count > 0
            ? System.Text.Encoding.ASCII.GetString(_escParams.ToArray())
            : "";
        string[] parts = paramStr.Split(';');
        int[] n = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++) int.TryParse(parts[i], out n[i]);

        switch (command)
        {
            case 0x41: // A - Up
                _cursorY = Math.Max(_scrollTop, _cursorY - (n.Length > 0 && n[0] > 0 ? n[0] : 1));
                break;
            case 0x42: // B - Down
                _cursorY = Math.Min(_scrollBottom, _cursorY + (n.Length > 0 && n[0] > 0 ? n[0] : 1));
                break;
            case 0x43: // C - Forward
                _cursorX = Math.Min(ScreenBuffer.Width - 1, _cursorX + (n.Length > 0 && n[0] > 0 ? n[0] : 1));
                break;
            case 0x44: // D - Back
                _cursorX = Math.Max(0, _cursorX - (n.Length > 0 && n[0] > 0 ? n[0] : 1));
                break;
            case 0x48: // H/f - Position
                int row = n.Length > 0 ? n[0] : 1;
                int col = n.Length > 1 ? n[1] : 1;
                _cursorX = Math.Clamp(col - 1, 0, ScreenBuffer.Width - 1);
                _cursorY = Math.Clamp(row - 1, 0, ScreenBuffer.Height - 1);
                break;
            case 0x4A: // J - Erase display
                switch (n.Length > 0 ? n[0] : 0)
                {
                    case 0: ClearToEndOfScreen(); break;
                    case 1: ClearToStartOfScreen(); break;
                    case 2: FillScreen(' '); break;
                    case 3: FillScreen(' '); _cursorX = 0; _cursorY = 0; break;
                }
                break;
            case 0x4B: // K - Erase line
                switch (n.Length > 0 ? n[0] : 0)
                {
                    case 0: ClearToEndOfLine(); break;
                    case 1: ClearToStartOfLine(); break;
                    case 2: FillRow(_cursorY, ' '); break;
                }
                break;
            case 0x4D: // M - Delete lines
                int del = n.Length > 0 && n[0] > 0 ? n[0] : 1;
                ScrollRegionUp(_cursorY, _scrollBottom, del);
                break;
            case 0x50: // P - Delete chars
                int dc = n.Length > 0 && n[0] > 0 ? n[0] : 1;
                DeleteCharsAt(_cursorX, _cursorY, dc);
                break;
            case 0x6D: // m - SGR
                ApplySgr(n);
                break;
            case 0x72: // r - Set scroll region
                int t = n.Length > 0 ? n[0] : 1;
                int b2 = n.Length > 1 ? n[1] : ScreenBuffer.Height;
                _scrollTop = Math.Clamp(t - 1, 0, ScreenBuffer.Height - 1);
                _scrollBottom = Math.Clamp(b2 - 1, 0, ScreenBuffer.Height - 1);
                if (_scrollTop >= _scrollBottom) { _scrollTop = 0; _scrollBottom = ScreenBuffer.Height - 1; }
                break;
            case 0x73: _savedX = _cursorX; _savedY = _cursorY; break;
            case 0x75: _cursorX = _savedX; _cursorY = _savedY; break;
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
        {
            for (int x = 0; x < ScreenBuffer.Width; x++)
            {
                char ch = _screen.GetChar(x, y + lines);
                ConsoleColor fg = _screen.GetForeground(x, y + lines);
                ConsoleColor bg = _screen.GetBackground(x, y + lines);
                _screen.SetCell(x, y, ch, fg, bg);
            }
        }

        FillRow(bottom, ' ');
    }

    private void FillRow(int row, char ch)
    {
        if (row < 0 || row >= ScreenBuffer.Height) return;
        for (int x = 0; x < ScreenBuffer.Width; x++)
            _screen.SetCell(x, row, ch, _fg, _bg);
    }

    private void FillScreen(char ch)
    {
        for (int y = 0; y < ScreenBuffer.Height; y++)
            FillRow(y, ch);
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

        char[] chars = new char[remaining];
        ConsoleColor[] fgs = new ConsoleColor[remaining];
        ConsoleColor[] bgs = new ConsoleColor[remaining];
        for (int i = 0; i < remaining; i++)
        {
            char ch = _screen.GetChar(x + count + i, y);
            chars[i] = ch == '\0' ? ' ' : ch;
            fgs[i] = _screen.GetForeground(x + count + i, y);
            bgs[i] = _screen.GetBackground(x + count + i, y);
        }

        FillRow(y, ' ');

        for (int i = 0; i < remaining; i++)
            _screen.SetCell(x + i, y, chars[i], fgs[i], bgs[i]);
    }

    private void ApplySgr(int[] codes)
    {
        if (codes.Length == 0) { _fg = ConsoleColor.Gray; _bg = ConsoleColor.Black; return; }
        foreach (int c in codes)
        {
            switch (c)
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
    }

    public void WriteString(string text) { foreach (char c in text) ProcessByte((byte)c); }

    public void WriteEsc(string sequence)
    {
        ProcessByte(0x1B);
        foreach (char c in sequence) ProcessByte((byte)c);
    }
}
