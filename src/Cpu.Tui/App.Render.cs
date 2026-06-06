using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public partial class App
{
    private void Render(TerminalLayout layout, bool fullRedraw)
    {
        if (fullRedraw) _renderer.Clear(ConsoleColor.Black, ConsoleColor.Black);
        if (!layout.CanRender) { RenderTooSmall(layout); _renderer.Flush(); return; }

        if (_imageMode) RenderImage(layout);
        else if (_canvasMode) RenderCanvas(layout, fullRedraw);
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
        var session = new PresentationSession(_renderer, term);
        var frame = session.CenterFrame(actual.Cols, actual.Rows);

        session.Clear();
        session.DrawFrame(frame, _frameStyle);
        session.RenderScreen(_screen, actual.Rows, actual.Cols, frame);

        if (_echoMode && _echo.CursorVisible)
        {
            int cx = Math.Min(_echo.CursorX, actual.Cols - 1);
            int cy = Math.Min(_echo.CursorY, actual.Rows - 1);
            char cur = _screen.GetChar(cx, cy);
            _renderer.SetCell(frame.X + 1 + cx, frame.Y + 1 + cy,
                cur == '\0' ? ' ' : cur, ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    private void RenderImage(TerminalLayout layout)
    {
        _imageView.Paths = _imagePaths;
        _imageView.Index = _imageIndex;
        _imageView.Mode = _imageRenderMode;
        _imageView.Render(_renderer, TermRectFrom(layout));
    }

    private const int InfoPanelWidth = 26;

    private void RenderCanvas(TerminalLayout layout, bool fullRedraw)
    {
        bool showPanel = layout.Width >= 66;
        int panelW = showPanel ? InfoPanelWidth : 0;
        int canvasW = layout.Width - panelW;
        var canvasTerm = new TermRect(0, 0, canvasW, layout.ContentHeight);

        if (fullRedraw)
            _canvasView.RequireFullClear();
        _canvasView.Render(_renderer, canvasTerm);

        if (showPanel)
            RenderCanvasInfoPanel(canvasTerm);
    }

    private void RenderCanvasInfoPanel(TermRect canvasArea)
    {
        var area = new TermRect(canvasArea.X2, 0, InfoPanelWidth, canvasArea.H);
        var session = new PresentationSession(_renderer, area);
        session.Clear();
        var frame = session.CenterFrame(InfoPanelWidth - 2, canvasArea.H - 2);
        var inner = session.DrawFrame(frame, FrameStyle.Ascii, "Info");

        var buffer = _canvasView.Canvas.Buffer;
        var mode = _canvasView.Mode;

        int mc = Math.Max(1, canvasArea.W - 4);
        int mr = Math.Max(1, canvasArea.H - 4);
        var (cols, rows) = PresentationSession.FitImage(buffer, mc, mr, mode);

        int y = 1;
        WritePanelLine(_renderer, inner, 1, y++, $"Canvas : {buffer.Width}x{buffer.Height} px");
        y++;
        WritePanelLine(_renderer, inner, 1, y++, $"Cells  : {cols}x{rows}");
        y++;
        WritePanelLine(_renderer, inner, 1, y++, $"Mode   : {ModeToShortString(mode)}");
        WritePanelLine(_renderer, inner, 1, y++, $"Colors : {ModeToColorCount(mode)}");
        y++;
        WritePanelLine(_renderer, inner, 1, y++, $"Term   : {_lastLayout.Width}x{_lastLayout.ContentHeight}");
    }

    private static void WritePanelLine(ITerminalRenderer renderer, TermRect area, int x, int y, string text)
    {
        int maxLen = Math.Max(0, area.W - x);
        if (text.Length > maxLen) text = text[..maxLen];
        if (text.Length == 0) return;
        TermArea.Write(renderer, area, x, y, text, ConsoleColor.Gray, ConsoleColor.Black);
    }

    private static string ModeToShortString(TerminalGraphicsMode mode) => mode switch
    {
        TerminalGraphicsMode.HalfBlockColor => "HalfBlock",
        TerminalGraphicsMode.BrailleMono => "Braille",
        TerminalGraphicsMode.Grayscale => "Grayscale",
        TerminalGraphicsMode.BestGlyph => "BestGlyph",
        TerminalGraphicsMode.BestGlyphTrueColor => "TrueColor",
        _ => mode.ToString()
    };

    private static string ModeToColorCount(TerminalGraphicsMode mode) => mode switch
    {
        TerminalGraphicsMode.HalfBlockColor => "16",
        TerminalGraphicsMode.BrailleMono => "2",
        TerminalGraphicsMode.Grayscale => "24",
        TerminalGraphicsMode.BestGlyph => "2",
        TerminalGraphicsMode.BestGlyphTrueColor => "16.7M",
        _ => "?"
    };

    private void RenderHelp(TerminalLayout layout)
    {
        _helpView.Render(_renderer, TermRectFrom(layout));
    }

    private void RenderDemoMenu(TerminalLayout layout)
    {
        _demoMenuView.SelectedIndex = _demoIndex;
        _demoMenuView.Render(_renderer, TermRectFrom(layout));
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
}
