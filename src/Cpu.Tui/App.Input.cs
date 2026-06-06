using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private void ReadInput()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);

            if (_demoMenu) { HandleDemoMenuKey(key); continue; }
            if (_imageMode) { HandleImageKey(key); continue; }
            if (_canvasMode) { HandleCanvasKey(key); continue; }
            if (_echoMode) { HandleEchoKey(key); continue; }

            switch (key.Key)
            {
                case ConsoleKey.Escape: _running = false; break;
                case ConsoleKey.F1: _showHelp = !_showHelp; SetStatus(_showHelp ? "Help" : "Ready"); break;
                case ConsoleKey.F2: CycleScreenMode(); break;
                case ConsoleKey.F3: _echoMode = true; _echo.Reset(); SetStatus("Echo"); break;
                case ConsoleKey.F4: _demoMenu = true; _demoIndex = 0; SetStatus("Demo"); break;
                case ConsoleKey.F5: SetStatus("Refresh"); break;
                case ConsoleKey.F6: ToggleFrameStyle(); break;
                case ConsoleKey.F7: StartImageMode(); break;
                case ConsoleKey.F9: EnterCanvasMode(); break;
                default: SetStatus(key.Key.ToString()); break;
            }
        }
    }

    private void HandleDemoMenuKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.F4:
                _demoMenu = false; _demoRunning = false; SetStatus("Ready"); break;
            case ConsoleKey.UpArrow:
                if (_demoIndex > 0) _demoIndex--; _dirty = true; break;
            case ConsoleKey.DownArrow:
                if (_demoIndex < PiaDemos.Names.Length - 1) _demoIndex++; _dirty = true; break;
            case ConsoleKey.Enter:
                StartDemo(_demoIndex); _demoMenu = false; SetDirtyFull(); break;
        }
    }

    private void HandleImageKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _imageMode = false; _loadedImage = null; _loadedImagePath = null; SetStatus("Ready"); break;
            case ConsoleKey.F7:
            case ConsoleKey.RightArrow:
                if (_imagePaths.Length > 0)
                {
                    _imageIndex = (_imageIndex + 1) % _imagePaths.Length;
                    _loadedImage = null; _loadedImagePath = null;
                    SetStatus(Path.GetFileName(_imagePaths[_imageIndex]));
                }
                break;
            case ConsoleKey.LeftArrow:
                if (_imagePaths.Length > 0)
                {
                    _imageIndex = (_imageIndex + _imagePaths.Length - 1) % _imagePaths.Length;
                    _loadedImage = null; _loadedImagePath = null;
                    SetStatus(Path.GetFileName(_imagePaths[_imageIndex]));
                }
                break;
            case ConsoleKey.F8: CycleImageRenderMode(); break;
        }
    }

    private void HandleCanvasKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape: _canvasMode = false; SetStatus("Ready"); break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                _canvasDemoIndex = key.Key == ConsoleKey.RightArrow
                    ? (_canvasDemoIndex + 1) % _canvasDemos.Length
                    : (_canvasDemoIndex + _canvasDemos.Length - 1) % _canvasDemos.Length;
                SeedCanvasDemo(_canvasDemoIndex);
                SetStatus(_canvasDemos[_canvasDemoIndex]);
                break;
            case ConsoleKey.F10: CycleCanvasRenderMode(); break;
        }
    }

    private void HandleEchoKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.F3)
        {
            _echoMode = false; SetStatus("Ready");
        }
        else
        {
            _echo.ProcessKey(key.Key, key.KeyChar);
            _dirty = true;
        }
    }

    private void ToggleFrameStyle()
    {
        _frameStyle = _frameStyle == FrameStyle.Ascii ? FrameStyle.Unicode : FrameStyle.Ascii;
        SetStatus(_frameStyle.ToString());
    }

    private void CycleImageRenderMode()
    {
        _imageRenderMode = _imageRenderMode switch
        {
            TerminalGraphicsMode.HalfBlockColor => TerminalGraphicsMode.BrailleMono,
            TerminalGraphicsMode.BrailleMono => TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.Grayscale => TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.BestGlyph => TerminalGraphicsMode.BestGlyphTrueColor,
            _ => TerminalGraphicsMode.HalfBlockColor
        };
        SetStatus(_imageRenderMode.ToString());
    }

    private void SetStatus(string text)
    {
        _statusText = text; _dirty = true; _fullRedraw = true;
    }

    private void SetDirtyFull()
    {
        _dirty = true; _fullRedraw = true;
    }
}
