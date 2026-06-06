using Cpu.Tui;

namespace Cpu.Tui.Devices.Pia;

/// <summary>
/// Echo terminal mode: a simple typing screen with cursor.
/// Processes key input and writes directly to ScreenBuffer.
/// </summary>
public class EchoTerminal
{
    private readonly ScreenBuffer _screen;
    private int _cursorX;
    private int _cursorY;
    private bool _cursorVisible = true;
    private int _cursorBlinkCounter;

    public const int Rows = 25;
    public const int Cols = 80;

    public int CursorX => _cursorX;
    public int CursorY => _cursorY;
    public bool CursorVisible => _cursorVisible;

    public EchoTerminal(ScreenBuffer screen)
    {
        _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        _screen.FillRect(0, 0, Cols, Rows, ' ', ConsoleColor.Gray, ConsoleColor.Black);
    }

    public void Reset()
    {
        _cursorX = 0;
        _cursorY = 0;
        _cursorVisible = true;
        _cursorBlinkCounter = 0;
        _screen.FillRect(0, 0, Cols, Rows, ' ', ConsoleColor.Gray, ConsoleColor.Black);
    }

    public void ProcessKey(ConsoleKey key, char keyChar)
    {
        switch (key)
        {
            case ConsoleKey.Enter:
                _cursorX = 0;
                _cursorY = Math.Min(Rows - 1, _cursorY + 1);
                break;
            case ConsoleKey.Backspace:
                if (_cursorX > 0)
                {
                    _cursorX--;
                    _screen.SetCell(_cursorX, _cursorY, ' ', ConsoleColor.Gray, ConsoleColor.Black);
                }
                break;
            case ConsoleKey.LeftArrow:
                if (_cursorX > 0) _cursorX--;
                break;
            case ConsoleKey.RightArrow:
                if (_cursorX < Cols - 1) _cursorX++;
                break;
            case ConsoleKey.UpArrow:
                if (_cursorY > 0) _cursorY--;
                break;
            case ConsoleKey.DownArrow:
                if (_cursorY < Rows - 1) _cursorY++;
                break;
            case ConsoleKey.Home:
                _cursorX = 0;
                _cursorY = 0;
                break;
            case ConsoleKey.End:
                _cursorX = 0;
                _cursorY = Rows - 1;
                break;
            case ConsoleKey.PageUp:
                _cursorY = 0;
                break;
            case ConsoleKey.PageDown:
                _cursorY = Rows - 1;
                break;
            default:
                if (keyChar >= 0x20 && keyChar < 0x7F)
                {
                    _screen.SetCell(_cursorX, _cursorY, keyChar, ConsoleColor.Gray, ConsoleColor.Black);
                    _cursorX++;
                    if (_cursorX >= Cols)
                    {
                        _cursorX = 0;
                        _cursorY = Math.Min(Rows - 1, _cursorY + 1);
                    }
                }
                break;
        }
    }

    public bool TickBlink()
    {
        _cursorBlinkCounter++;
        if (_cursorBlinkCounter >= 25)
        {
            _cursorVisible = !_cursorVisible;
            _cursorBlinkCounter = 0;
            return true;
        }

        return false;
    }

    public void ShowCursor() { _cursorVisible = true; _cursorBlinkCounter = 0; }
}
