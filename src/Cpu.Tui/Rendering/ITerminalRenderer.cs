namespace Cpu.Tui.Rendering;

/// <summary>
/// Abstract terminal renderer — double-buffered, ANSI-backed.
/// Supports both ConsoleColor and truecolor RGB.
/// </summary>
public interface ITerminalRenderer : IDisposable
{
    int Width { get; }
    int Height { get; }
    void Resize(int width, int height);

    void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg);
    void SetCell(int x, int y, char ch, TerminalColor fg, TerminalColor bg);

    void SetText(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg);
    void Clear(ConsoleColor fg, ConsoleColor bg);
    void Flush();
}
