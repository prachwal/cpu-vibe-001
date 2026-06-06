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

    private void RenderTooSmall(TerminalLayout layout)
    {
        WriteText(0, 0, "Terminal too small", ConsoleColor.White, ConsoleColor.DarkRed);
        WriteText(0, 1, $"Current: {layout.Width}x{layout.Height}", ConsoleColor.Gray, ConsoleColor.Black);
        WriteText(0, 2, "Minimum: 40x25", ConsoleColor.Gray, ConsoleColor.Black);
        WriteText(0, Math.Max(0, layout.Height - 1), "Esc Quit", ConsoleColor.Black, ConsoleColor.Gray);
    }

    private void RenderScreen(TerminalLayout layout)
    {
        ScreenSize requested = GetRequestedScreenSize();
        ScreenSize actual = layout.Fit(requested);
        int frameW = actual.Cols + 2, frameH = actual.Rows + 2;
        int left = Math.Max(1, (layout.Width - frameW) / 2);
        int top = Math.Max(1, (layout.ContentHeight - frameH) / 2);

        DrawFrame(left, top, frameW, frameH);
        int sx = left + 1, sy = top + 1;

        for (int row = 0; row < actual.Rows; row++)
            for (int col = 0; col < actual.Cols; col++)
            {
                char ch = _screen.GetChar(col, row);
                _renderer.SetCell(sx + col, sy + row, ch == '\0' ? ' ' : ch,
                    _screen.GetForeground(col, row), _screen.GetBackground(col, row));
            }

        if (_echoMode && _echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, actual.Cols - 1);
            int cy = Math.Min(_echo.CursorY, actual.Rows - 1);
            char cur = _screen.GetChar(cx, cy);
            _renderer.SetCell(sx + cx, sy + cy, cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    private void RenderImage(TerminalLayout layout)
    {
        if (_imagePaths.Length == 0)
        {
            WriteText(2, 2, "No JPG files found.", ConsoleColor.Yellow, ConsoleColor.Black);
            return;
        }
        try
        {
            ClearContentArea(layout);
            var image = GetCurrentImage();
            int maxCols = Math.Max(1, layout.Width - 4), maxRows = Math.Max(1, layout.ContentHeight - 4);
            var (cols, rows) = FitImageToTerminal(image, maxCols, maxRows, _imageRenderMode);
            int fw = cols + 2, fh = rows + 2;
            int left = Math.Max(1, (layout.Width - fw) / 2), top = Math.Max(1, (layout.ContentHeight - fh) / 2);
            DrawFrame(left, top, fw, fh);
            TerminalGraphicsRenderer.Render(_renderer, image, _imageRenderMode, left + 1, top + 1, cols, rows);
            WriteText(left + 2, top, Trim($" {_imagePaths[_imageIndex]} {_imageRenderMode} ", Math.Max(0, fw - 4)), ConsoleColor.Cyan, ConsoleColor.Black);
        }
        catch (Exception ex)
        {
            WriteText(2, 2, "Image render failed", ConsoleColor.White, ConsoleColor.DarkRed);
            WriteText(2, 4, Trim(ex.Message, Math.Max(1, layout.Width - 4)), ConsoleColor.Yellow, ConsoleColor.Black);
        }
    }

    private void RenderCanvas(TerminalLayout layout)
    {
        int maxCols = Math.Max(1, layout.Width - 4), maxRows = Math.Max(1, layout.ContentHeight - 4);
        var (cols, rows) = FitImageToTerminal(_canvas.Buffer, maxCols, maxRows, _imageRenderMode);
        int fw = cols + 2, fh = rows + 2;
        int left = Math.Max(1, (layout.Width - fw) / 2), top = Math.Max(1, (layout.ContentHeight - fh) / 2);
        ClearContentArea(layout);
        DrawFrame(left, top, fw, fh);
        TerminalGraphicsRenderer.Render(_renderer, _canvas.Buffer, _imageRenderMode, left + 1, top + 1, cols, rows);
        WriteText(left + 2, top, Trim($" {_canvasDemos[_canvasDemoIndex]} {_imageRenderMode} ", Math.Max(0, fw - 4)), ConsoleColor.Cyan, ConsoleColor.Black);
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
        int top = Math.Max(0, (layout.ContentHeight - lines.Length) / 2);
        for (int i = 0; i < lines.Length && top + i < layout.ContentHeight; i++)
        {
            string line = Trim(lines[i], layout.Width);
            int left = Math.Max(0, (layout.Width - line.Length) / 2);
            WriteText(left, top + i, line, i == 0 ? ConsoleColor.Cyan : ConsoleColor.Gray, ConsoleColor.Black);
        }
    }

    private void RenderDemoMenu(TerminalLayout layout)
    {
        string[] names = PiaDemos.Names;
        int top = Math.Max(1, (layout.ContentHeight - names.Length - 2) / 2);
        WriteText(Math.Max(0, (layout.Width - 10) / 2), top, "PIA Demos", ConsoleColor.Cyan, ConsoleColor.Black);
        top += 2;
        for (int i = 0; i < names.Length; i++)
        {
            bool sel = i == _demoIndex;
            string line = (sel ? " > " : "   ") + names[i];
            int left = Math.Max(0, (layout.Width - line.Length) / 2);
            WriteText(left, top + i, line, sel ? ConsoleColor.Black : ConsoleColor.Gray,
                sel ? ConsoleColor.Gray : ConsoleColor.Black);
        }
        WriteText(Math.Max(0, (layout.Width - 16) / 2), top + names.Length + 1, "Enter: run  Esc: back",
            ConsoleColor.DarkGray, ConsoleColor.Black);
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
        WriteText(0, layout.Height - 1, Trim(bar, layout.Width).PadRight(layout.Width), ConsoleColor.Black, ConsoleColor.Gray);
    }

    private void DrawFrame(int left, int top, int w, int h)
    {
        var g = _frameStyle == FrameStyle.Unicode ? FrameGlyphs.Unicode : FrameGlyphs.Ascii;
        var f = ConsoleColor.DarkCyan; var b = ConsoleColor.Black;
        _renderer.SetCell(left, top, g.TopLeft, f, b);
        _renderer.SetCell(left + w - 1, top, g.TopRight, f, b);
        _renderer.SetCell(left, top + h - 1, g.BottomLeft, f, b);
        _renderer.SetCell(left + w - 1, top + h - 1, g.BottomRight, f, b);
        for (int c = left + 1; c < left + w - 1; c++)
        { _renderer.SetCell(c, top, g.Horizontal, f, b); _renderer.SetCell(c, top + h - 1, g.Horizontal, f, b); }
        for (int r = top + 1; r < top + h - 1; r++)
        { _renderer.SetCell(left, r, g.Vertical, f, b); _renderer.SetCell(left + w - 1, r, g.Vertical, f, b); }
    }

    private void ClearContentArea(TerminalLayout layout)
    {
        for (int row = 0; row < layout.ContentHeight; row++)
            for (int col = 0; col < layout.Width; col++)
                _renderer.SetCell(col, row, ' ', ConsoleColor.Gray, ConsoleColor.Black);
    }

    private void WriteText(int col, int row, string text, ConsoleColor fg, ConsoleColor bg)
    {
        _renderer.SetText(col, row, text.AsSpan(), fg, bg);
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
