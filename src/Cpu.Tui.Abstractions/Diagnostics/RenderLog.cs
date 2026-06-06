using System.Diagnostics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Diagnostics;

/// <summary>
/// File logger for render diagnostics. Activated via env CPU_TUI_RENDER_LOG=1.
/// Logs lifecycle events, rects, and optionally individual cells.
/// Output: logs/render.log (relative) or /tmp/cpu-tui-render.log
/// </summary>
public static class RenderLog
{
    private static readonly StreamWriter? _writer;
    private static readonly bool _logCells;
    private static readonly TermRect? _filterRect;

    static RenderLog()
    {
        string? env = Environment.GetEnvironmentVariable("CPU_TUI_RENDER_LOG");
        if (env != "1") return;

        string path = "logs/render.log";
        try
        {
            Directory.CreateDirectory("logs");
            _writer = new StreamWriter(path, append: false) { AutoFlush = true };
        }
        catch
        {
            try { _writer = new StreamWriter("/tmp/cpu-tui-render.log", append: false) { AutoFlush = true }; }
            catch { _writer = null; }
        }

        _logCells = Environment.GetEnvironmentVariable("CPU_TUI_RENDER_LOG_CELLS") == "1";

        string? rectEnv = Environment.GetEnvironmentVariable("CPU_TUI_RENDER_LOG_RECT");
        if (rectEnv != null)
        {
            var parts = rectEnv.Split(',');
            if (parts.Length == 4 && int.TryParse(parts[0], out int rx) && int.TryParse(parts[1], out int ry)
                && int.TryParse(parts[2], out int rw) && int.TryParse(parts[3], out int rh))
                _filterRect = new TermRect(rx, ry, rw, rh);
        }
    }

    public static bool Enabled => _writer != null;

    [Conditional("DEBUG")]
    public static void Event(string name, string message)
    {
        if (_writer == null) return;
        _writer.WriteLine($"{Now()} EVENT {name} {message}");
    }

    [Conditional("DEBUG")]
    public static void Rect(string name, TermRect rect)
    {
        if (_writer == null) return;
        _writer.WriteLine($"{Now()} RECT {name} {rect}");
    }

    [Conditional("DEBUG")]
    public static void Cell(string source, int x, int y, char ch, TerminalColor fg, TerminalColor bg)
    {
        if (_writer == null || !_logCells) return;
        if (_filterRect.HasValue)
        {
            var r = _filterRect.Value;
            if (x < r.X || x >= r.X + r.W || y < r.Y || y >= r.Y + r.H) return;
        }
        string fgStr = fg.IsRgb ? $"({fg.R},{fg.G},{fg.B})" : fg.ConsoleColor.ToString();
        string bgStr = bg.IsRgb ? $"({bg.R},{bg.G},{bg.B})" : bg.ConsoleColor.ToString();
        _writer.WriteLine($"{Now()} CELL {source} x={x} y={y} ch='{Escape(ch)}' fg={fgStr} bg={bgStr}");
    }

    [Conditional("DEBUG")]
    public static void Outs(string format, params object?[] args)
    {
        if (_writer == null) return;
        _writer.WriteLine($"{Now()} {string.Format(format, args)}");
    }

    private static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");
    private static string Escape(char ch) => ch switch { '\0' => "\\0", ' ' => "·", _ => ch.ToString() };
}
