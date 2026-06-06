using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private void Render(TerminalLayout layout, bool fullRedraw)
    {
        if (fullRedraw) _renderer.Clear(ConsoleColor.Gray, ConsoleColor.Black);
        if (!layout.CanRender) { RenderTooSmall(layout); _renderer.Flush(); return; }

        if (_imageMode) RenderImage(layout);
        else if (_canvasMode) RenderCanvas(layout);
        else if (_demoMenu) RenderDemoMenu(layout);
        else if (_showHelp) RenderHelp(layout);
        else RenderScreen(layout);

        RenderFunctionBar(layout);
        _renderer.Flush();
    }

    private TermRect TermRectFrom(TerminalLayout l) => new(0, 0, l.Width, l.ContentHeight);

    private void RenderTooSmall(TerminalLayout layout)
    {
        var area = TermRectFrom(layout);
        TermArea.Write(_renderer, area, 0, 0, "Terminal too small", ConsoleColor.White, ConsoleColor.DarkRed);
        TermArea.Write(_renderer, area, 0, 1, $"Current: {layout.Width}x{layout.Height}", ConsoleColor.Gray, ConsoleColor.Black);
        TermArea.Write(_renderer, area, 0, 2, "Minimum: 40x25", ConsoleColor.Gray, ConsoleColor.Black);
        TermArea.Write(_renderer, area, 0, Math.Max(0, area.H - 1), "Esc Quit", ConsoleColor.Black, ConsoleColor.Gray);
    }

    private void RenderScreen(TerminalLayout layout)
    {
        var requested = GetRequestedScreenSize();
        var actual = layout.Fit(requested);
        var term = TermRectFrom(layout);
        var frame = term.CenterFrame(actual.Cols, actual.Rows);

        TermFrame.Draw(_renderer, frame, _frameStyle);
        TermArea.Screen(_renderer, frame.Inner, _screen, actual.Rows, actual.Cols);

        if (_echoMode && _echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, actual.Cols - 1);
            int cy = Math.Min(_echo.CursorY, actual.Rows - 1);
            char cur = _screen.GetChar(cx, cy);
            _renderer.SetCell(frame.Inner.X + cx, frame.Inner.Y + cy,
                cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    private void RenderImage(TerminalLayout layout)
    {
        if (_imagePaths.Length == 0)
        {
            TermArea.Write(_renderer, TermRectFrom(layout), 2, 2, "No JPG files found.", ConsoleColor.Yellow, ConsoleColor.Black);
            return;
        }
        try
        {
            var term = TermRectFrom(layout);
            int maxCols = Math.Max(1, term.W - 4), maxRows = Math.Max(1, term.H - 4);
            var (cols, rows) = FitImageToTerminal(GetCurrentImage(), maxCols, maxRows, _imageRenderMode);
            var frame = term.CenterFrame(cols, rows);

            TermArea.Clear(_renderer, term);
            TermFrame.Draw(_renderer, frame, _frameStyle, $"{_imagePaths[_imageIndex]} {_imageRenderMode}");
            TerminalGraphicsRenderer.Render(_renderer, GetCurrentImage(), _imageRenderMode, frame.Inner.X, frame.Inner.Y, cols, rows);
        }
        catch (Exception ex)
        {
            var term = TermRectFrom(layout);
            TermArea.Write(_renderer, term, 2, 2, "Image render failed", ConsoleColor.White, ConsoleColor.DarkRed);
            TermArea.Write(_renderer, term, 2, 4, Trim(ex.Message, Math.Max(1, layout.Width - 4)), ConsoleColor.Yellow, ConsoleColor.Black);
        }
    }

    private void RenderCanvas(TerminalLayout layout)
    {
        var term = TermRectFrom(layout);
        _canvasView.Render(_renderer, term);
    }

    private void RenderHelp(TerminalLayout layout)
    {
        string[] lines =
        [
            "CPU-VIBE terminal", "",
            "F1  Help  F2  Cycle screen", "F3  Echo mode  F4  Demo menu",
            "F5  Refresh  F6  Frame style", "F7  Image viewer  F9  Canvas",
            "Esc Quit", "",
            "Echo: type text, arrows move cursor.", "Canvas: left/right switch demo, F10 cycle mode"
        ];
        TermArea.Centered(_renderer, TermRectFrom(layout), lines, ConsoleColor.Gray, ConsoleColor.Black);
        // First line in cyan
        if (lines.Length > 0)
        {
            var term = TermRectFrom(layout);
            int top = Math.Max(0, (term.H - lines.Length) / 2);
            int left = Math.Max(0, (term.W - Math.Min(lines[0].Length, term.W)) / 2);
            TermArea.Write(_renderer, term, left, top, lines[0], ConsoleColor.Cyan, ConsoleColor.Black);
        }
    }

    private void RenderDemoMenu(TerminalLayout layout)
    {
        string[] names = PiaDemos.Names;
        var term = TermRectFrom(layout);
        int top = Math.Max(1, (term.H - names.Length - 2) / 2);
        TermArea.Write(_renderer, term, Math.Max(0, (term.W - 10) / 2), top, "PIA Demos", ConsoleColor.Cyan, ConsoleColor.Black);
        top += 2;
        for (int i = 0; i < names.Length; i++)
        {
            bool sel = i == _demoIndex;
            string line = (sel ? " > " : "   ") + names[i];
            TermArea.Write(_renderer, term, Math.Max(0, (term.W - line.Length) / 2), top + i, line,
                sel ? ConsoleColor.Black : ConsoleColor.Gray,
                sel ? ConsoleColor.Gray : ConsoleColor.Black);
        }
        TermArea.Write(_renderer, term, Math.Max(0, (term.W - 16) / 2), top + names.Length + 1,
            "Enter: run  Esc: back", ConsoleColor.DarkGray, ConsoleColor.Black);
    }

    private void RenderFunctionBar(TerminalLayout layout)
    {
        var size = GetRequestedScreenSize();
        string frameLabel = _frameStyle == FrameStyle.Unicode ? "UTF" : "ASCII";
        string left = _imageMode ? " F7 Next  F8 Mode  Esc Back "
            : _canvasMode ? " Left/Right Demo  F10 Mode  Esc Back "
            : _echoMode ? " F3 Normal  Esc Quit "
            : $" F1 Help  F2 {size.Rows}x{size.Cols}  F3 Echo  F4 Demo  F5 Refresh  F6 Frame:{frameLabel}  F7 Image  F9 Canvas  Esc Quit ";
        string bar = layout.Width >= left.Length + _statusText.Length + 4
            ? left + new string(' ', layout.Width - left.Length - _statusText.Length - 2) + $" {_statusText} "
            : left;
        TermArea.Write(_renderer, new TermRect(0, 0, layout.Width, layout.Height),
            0, layout.Height - 1, Trim(bar, layout.Width).PadRight(layout.Width), ConsoleColor.Black, ConsoleColor.Gray);
    }

    private static string Trim(string text, int width)
    {
        if (width <= 0) return string.Empty;
        return text.Length <= width ? text : text[..width];
    }

    private static (int Cols, int Rows) FitImageToTerminal(PixelBuffer image, int maxCols, int maxRows, TerminalGraphicsMode mode)
    {
        double pixelColsPerCell = mode switch
        {
            TerminalGraphicsMode.BrailleMono => 2.0,
            TerminalGraphicsMode.BestGlyph => 2.0,
            TerminalGraphicsMode.BestGlyphTrueColor => 2.0,
            _ => 1.0
        };
        double pixelRowsPerCell = mode switch
        {
            TerminalGraphicsMode.HalfBlockColor => 2.0,
            TerminalGraphicsMode.BrailleMono => 4.0,
            TerminalGraphicsMode.Grayscale => 2.0,
            TerminalGraphicsMode.BestGlyph => 4.0,
            TerminalGraphicsMode.BestGlyphTrueColor => 4.0,
            _ => 1.0
        };
        double scale = Math.Min(maxCols * pixelColsPerCell / image.Width, maxRows * pixelRowsPerCell / image.Height);
        scale = Math.Min(1.0, Math.Max(scale, 0.01));
        int cols = Math.Clamp((int)Math.Ceiling(image.Width * scale / pixelColsPerCell), 1, maxCols);
        int rows = Math.Clamp((int)Math.Ceiling(image.Height * scale / pixelRowsPerCell), 1, maxRows);
        return (cols, rows);
    }
}
