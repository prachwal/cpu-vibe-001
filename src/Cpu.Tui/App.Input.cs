using Cpu.Module;
using Cpu.Tui.Input;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private readonly TerminalInputReader _input = new();
    private int _cursorX = -1;
    private int _cursorY = -1;

    private const string MouseEnable = "\x1b[?1000h\x1b[?1003h\x1b[?1006h";
    private const string MouseDisable = "\x1b[?1006l\x1b[?1003l\x1b[?1000l";

    public void Run()
    {
        Console.TreatControlCAsInput = true;
        Console.CursorVisible = false;
        _mouseEnabled = false;
        _running = true;
        _input.EnsureRawTerminal();

        try
        {
            while (_running)
            {
                int w = Console.WindowWidth, h = Console.WindowHeight;
                bool changed = w != _lastW || h != _lastH;
                if (changed) _renderer.Resize(w, h);

                bool wantsMouse = _config.Current.MouseEnabled && _modules.CurrentWantsMouse;
                if (wantsMouse != _mouseEnabled)
                {
                    _mouseEnabled = wantsMouse;
                    _input.SetMouseCapture(_mouseEnabled);
                    Console.Write(_mouseEnabled ? MouseEnable : MouseDisable);
                    if (_mouseEnabled && _cursorX < 0)
                    {
                        _cursorX = w / 2;
                        _cursorY = h / 2;
                    }
                }

                if (_dirty || changed)
                {
                    Render(w, h, changed || _fullRedraw);
                    _lastW = w; _lastH = h;
                    _dirty = false; _fullRedraw = false;
                }

                ReadInput();
                if (_modules.Tick())
                    _dirty = true;
                Thread.Sleep(20);
            }
        }
        finally
        {
            Console.Write(MouseDisable);
            _input.SetMouseCapture(false);
            _input.ReleaseRawTerminal();
            _renderer.Dispose();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }

    private void Render(int w, int h, bool fullRedraw)
    {
        if (fullRedraw) _renderer.Clear(ConsoleColor.Black, ConsoleColor.Black);
        if (w < 40 || h < 25) { RenderTooSmall(w, h); _renderer.Flush(); return; }

        _modules.Render(_renderer, w, h);
        if (_mouseEnabled)
            DrawMouseCursor(w, h);
        _renderer.Flush();
    }

    private void DrawMouseCursor(int w, int h)
    {
        if (_cursorX < 0 || _cursorY < 0 || _cursorX >= w || _cursorY >= h)
            return;
        _renderer.SetCell(_cursorX, _cursorY, ' ', ConsoleColor.Black, ConsoleColor.White);
    }

    private void ReadInput()
    {
        try
        {
            _input.Pump();
            while (_input.TryDequeue(out TerminalInput input))
                HandleInput(input);
        }
        catch (InvalidOperationException) { }
    }

    private void HandleInput(TerminalInput input)
    {
        if (input.Kind == TerminalInputKind.Discard)
            return;

        if (input.Kind == TerminalInputKind.Mouse)
        {
            HandleMouse(input.Mouse);
            return;
        }

        HandleKey(input.Key);
    }

    private void HandleMouse(MouseEvent e)
    {
        _cursorX = e.X;
        _cursorY = e.Y;

        if (e.IsMotion)
        {
            _dirty = true;
            return;
        }

        if (e.IsRelease || e.Button != 0)
            return;

        if (_modules.OnMouse(e))
        {
            _modules.UpdateActive();
            _dirty = true;
            _fullRedraw = true;
        }
    }

    private void HandleKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape && _modules.Active == _modules.Root)
        {
            _running = false;
            return;
        }

        if (_modules.OnKey(key))
        {
            _dirty = true;
            _fullRedraw = true;
        }
    }
}
