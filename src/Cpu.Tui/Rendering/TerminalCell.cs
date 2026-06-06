namespace Cpu.Tui.Rendering;

/// <summary>
/// Single terminal cell state.
/// </summary>
public readonly record struct TerminalCell(char Ch, ConsoleColor Fg, ConsoleColor Bg)
{
    public static readonly TerminalCell Unknown = new('\0', ConsoleColor.Black, ConsoleColor.Black);
    public static readonly TerminalCell Empty = new(' ', ConsoleColor.Gray, ConsoleColor.Black);
}
