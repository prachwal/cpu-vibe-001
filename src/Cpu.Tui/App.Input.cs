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
                if (_demoIndex > 0) _demoIndex--;
                _demoMenuView.SelectedIndex = _demoIndex;
                _dirty = true; break;
            case ConsoleKey.DownArrow:
                if (_demoIndex < PiaDemos.Names.Length - 1) _demoIndex++;
                _demoMenuView.SelectedIndex = _demoIndex;
                _dirty = true; break;
            case ConsoleKey.Enter:
                StartDemo(_demoIndex); _demoMenu = false; SetDirtyFull(); break;
        }
    }

    private void HandleImageKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _imageMode = false; SetStatus("Ready"); break;
            case ConsoleKey.RightArrow:
            case ConsoleKey.F7:
                _imageView.NextImage();
                _imageIndex = _imageView.Index;
                _imagePaths = _imageView.Paths;
                SetStatus(GetFileNameOrReady());
                break;
            case ConsoleKey.LeftArrow:
                _imageView.PrevImage();
                _imageIndex = _imageView.Index;
                _imagePaths = _imageView.Paths;
                SetStatus(GetFileNameOrReady());
                break;
            case ConsoleKey.F8:
                _imageView.CycleMode();
                _imageRenderMode = _imageView.Mode;
                SetStatus(_imageRenderMode.ToString());
                break;
        }
    }

    private string GetFileNameOrReady()
    {
        return _imagePaths.Length > 0 && _imageIndex < _imagePaths.Length
            ? Path.GetFileName(_imagePaths[_imageIndex])
            : "Ready";
    }

    private void HandleCanvasKey(ConsoleKeyInfo key)
    {
        var term = new TermRect(0, 0, _lastLayout.Width, _lastLayout.ContentHeight);
        // 3D controls (↑↓ zoom, WASD rotate)
        if (_canvasView.DemoIndex == 2 && (key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow
            or ConsoleKey.W or ConsoleKey.A or ConsoleKey.S or ConsoleKey.D))
        {
            _canvasView.Handle3DKey(key.Key);
            _dirty = true;
            return;
        }
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _views.SwitchTo(_screenView, term);
                _canvasMode = false;
                SetStatus("Ready");
                break;
            case ConsoleKey.LeftArrow: _canvasView.PrevDemo(); SetDirtyFull(); break;
            case ConsoleKey.RightArrow: _canvasView.NextDemo(); SetDirtyFull(); break;
            case ConsoleKey.F10: _canvasView.CycleMode(); SetDirtyFull(); break;
        }
    }

    private void EnterCanvasMode()
    {
        _canvasMode = true;
        var term = new TermRect(0, 0, _lastLayout.Width, _lastLayout.ContentHeight);
        _views.SwitchTo(_canvasView, term);
        SetStatus("Canvas");
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
